using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            result.Names = meshes.Select(m => m.Name).ToArray();

            result.UpdateName();

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

            uint vertexStart = 0;
            uint indexStart = 0;
            var boneHashStart = 0;

            foreach (var mesh in meshes) {
                foreach (var subMesh in mesh.SubMeshes) {
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

            return result;
        }

    }
}
