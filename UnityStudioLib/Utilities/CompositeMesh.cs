using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using UnityStudio.UnityEngine;
using UnityStudio.UnityEngine.MeshParts;

namespace UnityStudio.Utilities {
    public sealed class CompositeMesh : Mesh {

        private CompositeMesh() {
        }

        public override string Name => _name;

        [NotNull, ItemNotNull]
        public IReadOnlyList<string> Names { get; private set; }

        // Mapping: submesh index => name index
        public IReadOnlyList<int> ParentMeshIndices { get; private set; }

        public int CompositedMeshCount { get; internal set; }

        private void UpdateName() {
            var sb = new StringBuilder();

            sb.Append("Composite mesh of:");

            foreach (var name in Names) {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(name);
            }

            _name = sb.ToString();
        }

        private string _name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public static CompositeMesh FromMeshes([NotNull, ItemNotNull] params Mesh[] meshes) {
            return FromMeshes(meshes as IReadOnlyList<Mesh>);
        }

        [NotNull]
        public static CompositeMesh FromMeshes([NotNull, ItemNotNull] IReadOnlyList<Mesh> meshes) {
            var result = new CompositeMesh();

            result.CompositedMeshCount = meshes.Count;

            {
                var meshNames = new List<string>();

                foreach (var m in meshes) {
                    if (m is CompositeMesh cm) {
                        meshNames.AddRange(cm.Names);
                    } else {
                        meshNames.Add(m.Name);
                    }
                }

                result.Names = meshNames.ToArray();

                result.UpdateName();
            }

            var subMeshList = new List<SubMesh>();
            var indexList = new List<uint>();
            var materialIDList = new List<int>();
            var skinList = new List<IReadOnlyList<BoneInfluence>>();
            var bindPoseList = new List<Matrix4x4>();
            var vertexList = new List<Vector3>();
            var normalList = new List<Vector3>();
            var colorList = new List<Vector4>();
            // We only handle UV1 here.
            var uv1List = new List<Vector2>();
            var tangentList = new List<Vector3>();
            var boneNameHashList = new List<uint>();
            var parentMeshIndices = new List<int>();

            uint vertexStart = 0;
            uint indexStart = 0;
            var boneHashStart = 0;
            var parentMeshIndex = 0;

            foreach (var mesh in meshes) {
                var cm = mesh as CompositeMesh;

                for (var i = 0; i < mesh.SubMeshes.Count; i++) {
                    var subMesh = mesh.SubMeshes[i];
                    Debug.Assert(subMesh.Topology == MeshTopology.Triangles);

                    var newSubMesh = new SubMesh();

                    newSubMesh.FirstIndex = subMesh.FirstIndex + indexStart;
                    newSubMesh.IndexCount = subMesh.IndexCount;
                    newSubMesh.Topology = subMesh.Topology;
                    newSubMesh.TriangleCount = subMesh.TriangleCount;
                    newSubMesh.FirstVertex = subMesh.FirstVertex + vertexStart;
                    newSubMesh.VertexCount = subMesh.VertexCount;
                    newSubMesh.BoundingBox = subMesh.BoundingBox;

                    subMeshList.Add(newSubMesh);

                    if (cm != null) {
                        parentMeshIndices.Add(parentMeshIndex + cm.ParentMeshIndices[i]);
                    } else {
                        parentMeshIndices.Add(parentMeshIndex);
                    }
                }

                if (cm != null) {
                    parentMeshIndex += cm.Names.Count;
                } else {
                    ++parentMeshIndex;
                }

                foreach (var index in mesh.Indices) {
                    indexList.Add(index + vertexStart);
                }

                foreach (var skin in mesh.Skin) {
                    var s = new BoneInfluence[4];

                    for (var i = 0; i < skin.Count; i++) {
                        var influence = skin[i];

                        if (influence == null) {
                            break;
                        }

                        var newInfluence = new BoneInfluence();

                        newInfluence.BoneIndex = influence.BoneIndex + boneHashStart;
                        newInfluence.Weight = influence.Weight;

                        s[i] = newInfluence;
                    }

                    skinList.Add(s);
                }

                // Some vertex do not bind to a bone (e.g. 100 vertices & 90 bone influences),
                // so we have to fill the gap ourselves, or this will influence the meshes
                // combined after current mesh.
                if (mesh.Skin.Count < mesh.VertexCount) {
                    var difference = mesh.VertexCount - mesh.Skin.Count;

                    for (var i = 0; i < difference; ++i) {
                        skinList.Add(new BoneInfluence[4]);
                    }
                }

                materialIDList.AddRange(mesh.MaterialIDs);

                boneNameHashList.AddRange(mesh.BoneNameHashes);

                bindPoseList.AddRange(mesh.BindPose);
                vertexList.AddRange(mesh.Vertices);
                normalList.AddRange(mesh.Normals);

                if (mesh.Colors != null) {
                    colorList.AddRange(mesh.Colors);
                } else {
                    for (var i = 0; i < mesh.VertexCount; ++i) {
                        colorList.Add(Vector4.Zero);
                    }
                }

                // TODO: counts don't match?
                if (mesh.Tangents != null) {
                    tangentList.AddRange(mesh.Tangents);
                } else {
                    for (var i = 0; i < mesh.VertexCount; ++i) {
                        tangentList.Add(Vector3.Zero);
                    }
                }

                uv1List.AddRange(mesh.UV1);

                vertexStart += (uint)mesh.VertexCount;
                indexStart += (uint)mesh.Indices.Count;
                boneHashStart += mesh.BoneNameHashes.Length;
            }

            result.VertexCount = (int)vertexStart;

            var shape = MergeBlendShapes(meshes);

            result.Shape = shape;

            result.SubMeshes = subMeshList.ToArray();
            result.Indices = indexList.ToArray();
            result.MaterialIDs = materialIDList.ToArray();
            result.Skin = skinList.ToArray();
            result.BindPose = bindPoseList.ToArray();
            result.Vertices = vertexList.ToArray();
            result.Normals = normalList.ToArray();
            result.Colors = colorList.ToArray();
            result.UV1 = uv1List.ToArray();
            result.Tangents = tangentList.ToArray();
            result.BoneNameHashes = boneNameHashList.ToArray();
            result.ParentMeshIndices = parentMeshIndices.ToArray();

            return result;
        }

        private static BlendShapeData MergeBlendShapes([NotNull, ItemNotNull] IReadOnlyList<Mesh> meshes) {
            var vertices = new List<BlendShapeVertex>();
            var shapes = new List<MeshBlendShape>();
            var channels = new List<MeshBlendShapeChannel>();
            var weights = new List<float>();

            uint meshVertexIndexStart = 0;
            var totalFrameCount = 0;
            uint totalVertexCount = 0;

            foreach (var mesh in meshes) {
                var meshShape = mesh.Shape;

                if (meshShape != null) {
                    var channelFrameCount = 0;

                    foreach (var channel in meshShape.Channels) {
                        var chan = new MeshBlendShapeChannel();

                        chan.Name = channel.Name;
                        chan.FrameIndex = channel.FrameIndex + totalFrameCount;
                        chan.FrameCount = channel.FrameCount;
                        chan.NameHash = channel.NameHash;

                        channelFrameCount += channel.FrameCount;

                        channels.Add(chan);
                    }

                    totalFrameCount += channelFrameCount;

                    weights.AddRange(meshShape.FullWeights);

                    uint shapeVertexCount = 0;

                    foreach (var s in meshShape.Shapes) {
                        var shape = new MeshBlendShape();

                        shape.FirstVertex = s.FirstVertex + totalVertexCount;
                        shape.HasNormals = s.HasNormals;
                        shape.HasTangents = s.HasTangents;
                        shape.VertexCount = s.VertexCount;

                        shapeVertexCount += s.VertexCount;

                        shapes.Add(shape);
                    }

                    totalVertexCount += shapeVertexCount;

                    foreach (var v in meshShape.Vertices) {
                        var vertex = new BlendShapeVertex();

                        vertex.Index = v.Index + meshVertexIndexStart;
                        vertex.Vertex = v.Vertex;
                        vertex.Normal = v.Normal;
                        vertex.Tangent = v.Tangent;

                        vertices.Add(vertex);
                    }
                }

                meshVertexIndexStart += (uint)mesh.VertexCount;
            }

            return new BlendShapeData(vertices.ToArray(), shapes.ToArray(), channels.ToArray(), weights.ToArray());
        }

    }
}
