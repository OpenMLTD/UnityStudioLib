using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityStudio.Extensions;
using UnityStudio.Models;
using UnityStudio.Unity.MeshParts;

namespace UnityStudio.Unity {
    public sealed class Mesh {

        internal Mesh([NotNull] AssetPreloadData preloadData, bool metadataOnly) {
            var version = preloadData.Source.VersionComponents;
            var reader = preloadData.Source.FileReader;

            reader.Position = preloadData.Offset;

            _reader = reader;

            var use16BitIndices = true; //3.5.0 and newer always uses 16bit indices

            if (preloadData.Source.Platform == AssetPlatform.UnityPackage) {
                var objectHideFlags = reader.ReadUInt32();
                var prefabParentObject = preloadData.Source.ReadPPtr();
                var prefabInternal = preloadData.Source.ReadPPtr();
            }

            Name = reader.ReadAlignedString();

            if (metadataOnly) {
                return;
            }

            if (version[0] < 3 || (version[0] == 3 && version[1] < 5)) {
                use16BitIndices = reader.ReadBoolean();
                reader.Position += 3;
            }

            #region Index Buffer for 2.5.1 and earlier
            if (version[0] == 2 && version[1] <= 5) {
                var indexBufferByteSize = reader.ReadInt32();

                if (use16BitIndices) {
                    _indexBuffer = new uint[indexBufferByteSize / 2];

                    for (var i = 0; i < indexBufferByteSize / 2; i++) {
                        _indexBuffer[i] = reader.ReadUInt16();
                    }

                    reader.AlignBy(4);
                } else {
                    _indexBuffer = new uint[indexBufferByteSize / 4];

                    for (var i = 0; i < indexBufferByteSize / 4; i++) {
                        _indexBuffer[i] = reader.ReadUInt32();
                    }
                }
            }
            #endregion

            #region subMeshes
            var subMeshesCount = reader.ReadInt32();

            var subMeshes = new SubMesh[subMeshesCount];

            for (var i = 0; i < subMeshesCount; i++) {
                var subMesh = new SubMesh();

                subMesh.FirstByte = reader.ReadUInt32();
                subMesh.IndexCount = reader.ReadUInt32(); //what is this in case of triangle strips?
                subMesh.Topology = reader.ReadInt32(); //isTriStrip

                if (version[0] < 4) {
                    subMesh.TriangleCount = reader.ReadUInt32();
                }

                if (version[0] >= 3) {
                    if (version[0] > 2017 || (version[0] == 2017 && version[1] >= 3)) { //2017.3 and up
                        var baseVertex = reader.ReadUInt32();
                    }

                    subMesh.FirstVertex = reader.ReadUInt32();
                    subMesh.VertexCount = reader.ReadUInt32();

                    var aabb = new AABB();

                    aabb.MinX = reader.ReadSingle();
                    aabb.MinY = reader.ReadSingle();
                    aabb.MinZ = reader.ReadSingle();
                    aabb.MaxX = reader.ReadSingle();
                    aabb.MaxY = reader.ReadSingle();
                    aabb.MaxZ = reader.ReadSingle();

                    subMesh.BoundingBox = aabb;
                }

                subMeshes[i] = subMesh;
            }

            SubMeshes = subMeshes;
            #endregion

            if (version[0] == 4 && ((version[1] == 1 && preloadData.Source.BuildType[0] != 'a') || (version[1] > 1 && version[1] <= 2))) { // BlendShapeData for 4.1.0 to 4.2.x, excluding 4.1.0 alpha
                var shapeCount = reader.ReadInt32();

                if (shapeCount > 0) {
                    //bool stop = true;
                }

                for (var i = 0; i < shapeCount; i++) { //untested
                    var shapeName = reader.ReadAlignedString();
                    reader.Position += 36; //uint firstVertex, vertexCount; Vector3f aabbMinDelta, aabbMaxDelta; bool hasNormals, hasTangents
                }

                var shapeVertexCount = reader.ReadInt32();

                reader.Position += shapeVertexCount * 40; //vertex positions, normals, tangents & uint index
            } else if (version[0] >= 5 || (version[0] == 4 && version[1] >= 3)) { // BlendShapeData and BindPose for 4.3.0 and later
                Shape = reader.ReadBlendShapeData();

                var bindPoseCount = reader.ReadInt32();
                var bindPose = new float[bindPoseCount][,];

                for (var i = 0; i < bindPoseCount; i++) {
                    bindPose[i] = new[,] {
                        { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() },
                        { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() } };
                }

                BindPose = bindPose;

                var boneNameHashCount = reader.ReadInt32();

                BoneNameHashes = new uint[boneNameHashCount];

                for (var i = 0; i < boneNameHashCount; i++) {
                    BoneNameHashes[i] = reader.ReadUInt32();
                }

                var rootBoneNameHash = reader.ReadUInt32();
            }
            uint meshCompression = 0;

            #region Index Buffer for 2.6.0 and later
            if (version[0] >= 3 || (version[0] == 2 && version[1] >= 6)) {
                meshCompression = reader.ReadByte();
                if (version[0] >= 4) {
                    if (version[0] < 5) {
                        uint m_StreamCompression = reader.ReadByte();
                    }
                    var m_IsReadable = reader.ReadBoolean();
                    var m_KeepVertices = reader.ReadBoolean();
                    var m_KeepIndices = reader.ReadBoolean();
                    if (preloadData.HasStructMember("m_UsedForStaticMeshColliderOnly")) {
                        var m_UsedForStaticMeshColliderOnly = reader.ReadBoolean();
                    }
                }
                reader.AlignBy(4);
                //This is a bug fixed in 2017.3.1p1 and later versions
                if ((version[0] > 2017 || (version[0] == 2017 && version[1] >= 4)) || //2017.4
                    ((version[0] == 2017 && version[1] == 3 && version[2] == 1) && preloadData.Source.BuildType[0] == 'p') || //fixed after 2017.3.1px
                    ((version[0] == 2017 && version[1] == 3) && meshCompression == 0))//2017.3.xfx with no compression
                {
                    var m_IndexFormat = reader.ReadInt32();
                }
                var m_IndexBuffer_size = reader.ReadInt32();

                if (use16BitIndices) {
                    _indexBuffer = new uint[m_IndexBuffer_size / 2];
                    for (var i = 0; i < m_IndexBuffer_size / 2; i++) { _indexBuffer[i] = reader.ReadUInt16(); }
                    reader.AlignBy(4);
                } else {
                    _indexBuffer = new uint[m_IndexBuffer_size / 4];
                    for (var i = 0; i < m_IndexBuffer_size / 4; i++) { _indexBuffer[i] = reader.ReadUInt32(); }
                    reader.AlignBy(4);//untested
                }
            }
            #endregion

            #region Vertex Buffer for 3.4.2 and earlier
            if (version[0] < 3 || (version[0] == 3 && version[1] < 5)) {
                var vertexCount = reader.ReadInt32();
                var vertices = new float[vertexCount * 3];

                for (var v = 0; v < vertexCount * 3; v++) {
                    vertices[v] = reader.ReadSingle();
                }

                Vertices = vertices;
                VertexCount = vertexCount;

                var skinCount = reader.ReadInt32();
                var skin = new BoneInfluence[skinCount][];

                for (var i = 0; i < skinCount; i++) {
                    skin[i] = new BoneInfluence[4];

                    for (var j = 0; j < 4; j++) {
                        skin[i][j] = new BoneInfluence {
                            Weight = reader.ReadSingle()
                        };
                    }

                    for (var j = 0; j < 4; j++) {
                        skin[i][j].BoneIndex = reader.ReadInt32();
                    }
                }

                Skin = skin;

                var bindPoseCount = reader.ReadInt32();
                var bindPose = new float[bindPoseCount][,];

                for (var i = 0; i < bindPoseCount; i++) {
                    bindPose[i] = new[,] {
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
                    };
                }

                BindPose = bindPose;

                var uv1Count = reader.ReadInt32();
                UV1 = new float[uv1Count * 2];

                for (var v = 0; v < uv1Count * 2; v++) {
                    UV1[v] = reader.ReadSingle();
                }

                var uv2Count = reader.ReadInt32();
                UV2 = new float[uv2Count * 2];

                for (var v = 0; v < uv2Count * 2; v++) {
                    UV2[v] = reader.ReadSingle();
                }

                if (version[0] == 2 && version[1] <= 5) {
                    var m_TangentSpace_size = reader.ReadInt32();
                    Normals = new float[m_TangentSpace_size * 3];

                    for (var v = 0; v < m_TangentSpace_size; v++) {
                        Normals[v * 3] = reader.ReadSingle();
                        Normals[v * 3 + 1] = reader.ReadSingle();
                        Normals[v * 3 + 2] = reader.ReadSingle();
                        reader.Position += 16; //Vector3f tangent & float handedness 
                    }
                } else { //2.6.0 and later
                    var m_Tangents_size = reader.ReadInt32();
                    reader.Position += m_Tangents_size * 16; //Vector4f

                    var m_Normals_size = reader.ReadInt32();
                    Normals = new float[m_Normals_size * 3];

                    for (var v = 0; v < m_Normals_size * 3; v++) {
                        Normals[v] = reader.ReadSingle();
                    }
                }
            }
            #endregion
            #region Vertex Buffer for 3.5.0 and later
                else {
                #region read vertex stream

                if (version[0] < 2018 || (version[0] == 2018 && version[1] < 2)) {  //2018.2 down
                    var skinCount = reader.ReadInt32();

                    var skin = new BoneInfluence[skinCount][];

                    for (var i = 0; i < skin.Length; i++) {
                        skin[i] = new BoneInfluence[4];

                        for (var j = 0; j < 4; j++) {
                            skin[i][j] = new BoneInfluence {
                                Weight = reader.ReadSingle()
                            };
                        }

                        for (var j = 0; j < 4; j++) {
                            skin[i][j].BoneIndex = reader.ReadInt32();
                        }
                    }

                    Skin = skin;
                }

                if (version[0] == 3 || (version[0] == 4 && version[1] <= 2)) {
                    var bindPoseCount = reader.ReadInt32();
                    var bindPose = new float[bindPoseCount][,];

                    for (var i = 0; i < bindPoseCount; i++) {
                        bindPose[i] = new[,] {
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
                        };
                    }

                    BindPose = bindPose;
                }

                if (version[0] < 2018) { //2018 down
                    var currentChannels = reader.ReadInt32();
                }

                VertexCount = reader.ReadInt32();
                //int singleStreamStride = 0;//used tor version 5
                var streamCount = 0;

                if (version[0] < 4) { // streams for 3.5.0 - 3.5.7
                    if (meshCompression != 0 && version[2] == 0) { //special case not just on platform 9 (PS3)
                        reader.Position += 12;
                    } else {
                        var streams = new StreamInfo[4];

                        for (var i = 0; i < 4; i++) {
                            var streamInfo = new StreamInfo();

                            streamInfo.ChannelMask = new BitArray(new[] {
                                reader.ReadInt32()
                            });
                            streamInfo.Offset = reader.ReadInt32();
                            streamInfo.Stride = reader.ReadInt32();
                            streamInfo.Align = reader.ReadUInt32();

                            streams[i] = streamInfo;
                        }

                        _streams = streams;
                    }
                } else { //channels and streams for 4.0.0 and later
                    var channelCount = reader.ReadInt32();
                    var channels = new ChannelInfo[channelCount];

                    for (var i = 0; i < channels.Length; i++) {
                        var channel = new ChannelInfo();

                        channel.Stream = reader.ReadByte();
                        channel.Offset = reader.ReadByte();
                        channel.Format = reader.ReadByte();
                        channel.Dimension = reader.ReadByte();

                        //calculate stride for version 5
                        //singleStreamStride += m_Channels[c].dimension * (4 / (int)Math.Pow(2, m_Channels[c].format));

                        if (channel.Stream >= streamCount) {
                            streamCount = channel.Stream + 1;
                        }

                        channels[i] = channel;
                    }

                    _channels = channels;

                    if (version[0] < 5) {
                        var streamInfoCount = reader.ReadInt32();
                        var streams = new StreamInfo[streamInfoCount];

                        for (var i = 0; i < streams.Length; i++) {
                            var streamInfo = new StreamInfo();

                            streamInfo.ChannelMask = new BitArray(new int[1] { reader.ReadInt32() });
                            streamInfo.Offset = reader.ReadInt32();
                            streamInfo.Stride = reader.ReadByte();
                            streamInfo.DividerOp = reader.ReadByte();
                            streamInfo.Frequency = reader.ReadUInt16();

                            streams[i] = streamInfo;
                        }

                        _streams = streams;
                    }
                }

                //actual Vertex Buffer
                var tempDataSize = reader.ReadInt32();
                var tempDataZone = new byte[tempDataSize];
                reader.Read(tempDataZone, 0, tempDataSize);

                if (version[0] >= 5) { //create streams
                    var streams = new StreamInfo[streamCount];

                    for (var i = 0; i < streamCount; i++) {
                        var streamInfo = new StreamInfo();

                        streamInfo.Offset = 0;
                        streamInfo.Stride = 0;

                        uint channelMask = 0;

                        for (var j = 0; j < _channels.Length; j++) {
                            var channel = _channels[j];

                            if (channel.Stream == i) {
                                if (channel.Dimension > 0) {
                                    channelMask |= 1u << j;
                                }

                                streamInfo.Stride += channel.Dimension * (4 / (int)Math.Pow(2, channel.Format));
                            }
                        }

                        if (i > 0) {
                            streamInfo.Offset = streams[i - 1].Offset + streams[i - 1].Stride * VertexCount;

                            if (streamCount >= 2) {
                                streamInfo.Offset = tempDataZone.Length - streamInfo.Stride * VertexCount;
                            }
                        }

                        streamInfo.ChannelMask = new BitArray(new[] {
                            (int)channelMask
                        });

                        streams[i] = streamInfo;
                    }

                    _streams = streams;
                }
                #endregion

                #region compute FvF
                var componentByteSize = 0;
                byte[] componentBytes;
                float[] componentsArray;

                if (_channels != null) { // 4.0.0 and later
                    //it is better to loop channels instead of streams
                    //because channels are likely to be sorted by vertex property
                    foreach (var channel in _channels) {
                        if (channel.Dimension <= 0) {
                            continue;
                        }

                        var stream = _streams[channel.Stream];

                        for (var b = 0; b < 8; b++) {
                            if (!stream.ChannelMask.Get(b)) {
                                continue;
                            }

                            // in version 4.x the colors channel has 1 dimension, as in 1 color with 4 components
                            if (b == 2 && channel.Format == 2) {
                                channel.Dimension = 4;
                            }

                            componentByteSize = 4 / (int)Math.Pow(2, channel.Format);

                            componentBytes = new byte[componentByteSize];
                            componentsArray = new float[VertexCount * channel.Dimension];

                            for (var v = 0; v < VertexCount; v++) {
                                var vertexOffset = stream.Offset + channel.Offset + stream.Stride * v;

                                for (var d = 0; d < channel.Dimension; d++) {
                                    var componentOffset = vertexOffset + componentByteSize * d;

                                    Buffer.BlockCopy(tempDataZone, componentOffset, componentBytes, 0, componentByteSize);

                                    componentsArray[v * channel.Dimension + d] = BytesToFloat(componentBytes);
                                }
                            }

                            switch (b) {
                                case 0:
                                    Vertices = componentsArray;
                                    break;
                                case 1:
                                    Normals = componentsArray;
                                    break;
                                case 2:
                                    Colors = componentsArray;
                                    break;
                                case 3:
                                    UV1 = componentsArray;
                                    break;
                                case 4:
                                    UV2 = componentsArray;
                                    break;
                                case 5:
                                    if (version[0] >= 5) {
                                        UV3 = componentsArray;
                                    } else {
                                        Tangents = componentsArray;
                                    }
                                    break;
                                case 6: UV4 = componentsArray; break;
                                case 7: Tangents = componentsArray; break;
                            }

                            stream.ChannelMask.Set(b, false);

                            break;
                        }
                    }
                } else if (_streams != null) { // 3.5.0 - 3.5.7
                    foreach (var stream in _streams) {
                        //a stream may have multiple vertex components but without channels there are no offsets, so I assume all vertex properties are in order
                        //version 3.5.x only uses floats, and that's probably why channels were introduced in version 4

                        var channel = new ChannelInfo(); //create my own channel so I can use the same methods

                        channel.Offset = 0;

                        for (var b = 0; b < 6; b++) {
                            if (!stream.ChannelMask.Get(b)) {
                                continue;
                            }

                            switch (b) {
                                case 0:
                                case 1:
                                    componentByteSize = 4;
                                    channel.Dimension = 3;
                                    break;
                                case 2:
                                    componentByteSize = 1;
                                    channel.Dimension = 4;
                                    break;
                                case 3:
                                case 4:
                                    componentByteSize = 4;
                                    channel.Dimension = 2;
                                    break;
                                case 5:
                                    componentByteSize = 4;
                                    channel.Dimension = 4;
                                    break;
                            }

                            componentBytes = new byte[componentByteSize];
                            componentsArray = new float[VertexCount * channel.Dimension];

                            for (var v = 0; v < VertexCount; v++) {
                                var vertexOffset = stream.Offset + channel.Offset + stream.Stride * v;

                                for (var d = 0; d < channel.Dimension; d++) {
                                    var dataSizeOffset = vertexOffset + componentByteSize * d;

                                    Buffer.BlockCopy(tempDataZone, dataSizeOffset, componentBytes, 0, componentByteSize);

                                    componentsArray[v * channel.Dimension + d] = BytesToFloat(componentBytes);
                                }
                            }

                            switch (b) {
                                case 0:
                                    Vertices = componentsArray;
                                    break;
                                case 1:
                                    Normals = componentsArray;
                                    break;
                                case 2:
                                    Colors = componentsArray;
                                    break;
                                case 3:
                                    UV1 = componentsArray;
                                    break;
                                case 4:
                                    UV2 = componentsArray;
                                    break;
                                case 5:
                                    Tangents = componentsArray;
                                    break;
                            }

                            channel.Offset += (byte)(channel.Dimension * componentByteSize); //safe to cast as byte because strides larger than 255 are unlikely
                            stream.ChannelMask.Set(b, false);
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region Compressed Mesh data for 2.6.0 and later - 160 bytes
            if (version[0] >= 3 || (version[0] == 2 && version[1] >= 6)) {
                //remember there can be combinations of packed and regular vertex properties

                #region m_Vertices
                var verticesPacked = ReadPackedVector(reader);

                if (verticesPacked.ItemCount > 0) {
                    VertexCount = verticesPacked.ItemCount / 3;
                    var verticesUnpacked = UnpackBitVector(verticesPacked);
                    var bitmax = 0;//used to convert int value to float

                    for (var b = 0; b < verticesPacked.BitSize; b++) {
                        bitmax |= (1 << b);
                    }

                    Vertices = new float[verticesPacked.ItemCount];

                    for (var v = 0; v < verticesPacked.ItemCount; v++) {
                        Vertices[v] = ((float)verticesUnpacked[v] / bitmax) * verticesPacked.Range + verticesPacked.Start;
                    }
                }
                #endregion

                #region m_UV
                var uvPacked = ReadPackedVector(reader); //contains all channels 

                if (uvPacked.ItemCount > 0 && ExportUVs) {
                    var uvUnpacked = UnpackBitVector(uvPacked);
                    var bitmax = 0;

                    for (var b = 0; b < uvPacked.BitSize; b++) {
                        bitmax |= (1 << b);
                    }

                    UV1 = new float[VertexCount * 2];

                    for (var v = 0; v < VertexCount * 2; v++) {
                        UV1[v] = ((float)uvUnpacked[v] / bitmax) * uvPacked.Range + uvPacked.Start;
                    }

                    if (uvPacked.ItemCount >= VertexCount * 4) {
                        UV2 = new float[VertexCount * 2];

                        for (uint v = 0; v < VertexCount * 2; v++) {
                            UV2[v] = ((float)uvUnpacked[v + VertexCount * 2] / bitmax) * uvPacked.Range + uvPacked.Start;
                        }

                        if (uvPacked.ItemCount >= VertexCount * 6) {
                            UV3 = new float[VertexCount * 2];

                            for (uint v = 0; v < VertexCount * 2; v++) {
                                UV3[v] = ((float)uvUnpacked[v + VertexCount * 4] / bitmax) * uvPacked.Range + uvPacked.Start;
                            }

                            if (uvPacked.ItemCount == VertexCount * 8) {
                                UV4 = new float[VertexCount * 2];

                                for (uint v = 0; v < VertexCount * 2; v++) {
                                    UV4[v] = ((float)uvUnpacked[v + VertexCount * 6] / bitmax) * uvPacked.Range + uvPacked.Start;
                                }
                            }
                        }
                    }
                }
                #endregion

                #region m_BindPose
                if (version[0] < 5) {
                    var bindPosesPacked = ReadPackedVector(reader);

                    if (bindPosesPacked.ItemCount > 0 && ExportDeformers) {
                        var bindPosesUnpacked = UnpackBitVector(bindPosesPacked);
                        var bitmax = 0;//used to convert int value to float

                        for (var b = 0; b < bindPosesPacked.BitSize; b++) {
                            bitmax |= (1 << b);
                        }

                        BindPose = new float[bindPosesPacked.ItemCount / 16][,];

                        for (var i = 0; i < BindPose.Length; i++) {
                            BindPose[i] = new float[4, 4];
                            for (var j = 0; j < 4; j++) {
                                for (var k = 0; k < 4; k++) {
                                    BindPose[i][j, k] = (float)((double)bindPosesUnpacked[i * 16 + j * 4 + k] / bitmax) * bindPosesPacked.Range + bindPosesPacked.Start;
                                }
                            }
                        }
                    }
                }
                #endregion

                var normalsPacked = ReadPackedVector(reader);

                var tangentsPacked = ReadPackedVector(reader);

                var boneWeightsPacked = ReadPackedVector(reader);

                #region m_Normals
                var normalSignsPacked = ReadPackedVector(reader);

                if (normalsPacked.ItemCount > 0 && ExportNormals) {
                    var normalsUnpacked = UnpackBitVector(normalsPacked);
                    var normalSigns = UnpackBitVector(normalSignsPacked);
                    var bitmax = 0;

                    for (var b = 0; b < normalsPacked.BitSize; b++) {
                        bitmax |= (1 << b);

                    }

                    Normals = new float[normalsPacked.ItemCount / 2 * 3];

                    for (var v = 0; v < normalsPacked.ItemCount / 2; v++) {
                        Normals[v * 3] = ((float)normalsUnpacked[v * 2] / bitmax) * normalsPacked.Range + normalsPacked.Start;
                        Normals[v * 3 + 1] = ((float)normalsUnpacked[v * 2 + 1] / bitmax) * normalsPacked.Range + normalsPacked.Start;
                        Normals[v * 3 + 2] = (float)Math.Sqrt(1 - Normals[v * 3] * Normals[v * 3] - Normals[v * 3 + 1] * Normals[v * 3 + 1]);

                        if (normalSigns[v] == 0) {
                            Normals[v * 3 + 2] *= -1;
                        }
                    }
                }
                #endregion

                #region m_Tangents
                var tangentSignsPacked = ReadPackedVector(reader);

                if (tangentsPacked.ItemCount > 0 && ExportTangents) {
                    var tangentsUnpacked = UnpackBitVector(tangentsPacked);
                    var tangentSignsUnpacked = UnpackBitVector(tangentSignsPacked);
                    var bitmax = 0;

                    for (var b = 0; b < tangentsPacked.BitSize; b++) {
                        bitmax |= (1 << b);
                    }

                    Tangents = new float[tangentsPacked.ItemCount / 2 * 3];

                    for (var v = 0; v < tangentsPacked.ItemCount / 2; v++) {
                        Tangents[v * 3] = ((float)tangentsUnpacked[v * 2] / bitmax) * tangentsPacked.Range + tangentsPacked.Start;
                        Tangents[v * 3 + 1] = ((float)tangentsUnpacked[v * 2 + 1] / bitmax) * tangentsPacked.Range + tangentsPacked.Start;
                        Tangents[v * 3 + 2] = (float)Math.Sqrt(1 - Tangents[v * 3] * Tangents[v * 3] - Tangents[v * 3 + 1] * Tangents[v * 3 + 1]);

                        if (tangentSignsUnpacked[v] == 0) {
                            Tangents[v * 3 + 2] *= -1;
                        }
                    }
                }
                #endregion

                #region m_FloatColors
                if (version[0] >= 5) {
                    var floatColors = ReadPackedVector(reader);

                    if (floatColors.ItemCount > 0 && ExportColors) {
                        var floatColorsUnpacked = UnpackBitVector(floatColors);
                        var bitmax = 0;

                        for (var b = 0; b < floatColors.ItemCount; b++) {
                            bitmax |= (1 << b);
                        }

                        Colors = new float[floatColors.ItemCount];

                        for (var v = 0; v < floatColors.ItemCount; v++) {
                            Colors[v] = (float)floatColorsUnpacked[v] / bitmax * floatColors.Range + floatColors.Start;
                        }
                    }
                }
                #endregion

                #region m_Skin
                var boneIndicesPacked = ReadPackedVector(reader);

                if (boneIndicesPacked.ItemCount > 0 && ExportDeformers) {
                    var boneWeightsUnpacked = UnpackBitVector(boneWeightsPacked);
                    var bitmax = 0;

                    for (var b = 0; b < boneWeightsPacked.BitSize; b++) {
                        bitmax |= (1 << b);
                    }

                    var boneIndicesUnpacked = UnpackBitVector(boneIndicesPacked);

                    var skin = new BoneInfluence[VertexCount][];

                    for (var i = 0; i < skin.Length; i++) {
                        skin[i] = new BoneInfluence[4];
                    }

                    Skin = skin;

                    int inflCount = boneWeightsPacked.ItemCount;
                    var vertIndex = 0;
                    var weightIndex = 0;
                    var bonesIndex = 0;

                    for (weightIndex = 0; weightIndex < inflCount; vertIndex++) {
                        var inflSum = 0;
                        int j;

                        for (j = 0; j < 4; j++) {
                            int curWeight;

                            if (j == 3) {
                                curWeight = 31 - inflSum;
                            } else {
                                curWeight = (int)boneWeightsUnpacked[weightIndex];
                                weightIndex++;
                                inflSum += curWeight;
                            }

                            var realCurWeight = (float)curWeight / bitmax;

                            var boneIndex = (int)boneIndicesUnpacked[bonesIndex];

                            bonesIndex++;

                            if (boneIndex < 0) {
                                throw new Exception($"Invalid bone index {boneIndex}");
                            }

                            var boneInfl = new BoneInfluence() {
                                Weight = realCurWeight,
                                BoneIndex = boneIndex,
                            };

                            skin[vertIndex][j] = boneInfl;

                            if (inflSum == 31) {
                                break;
                            }
                            if (inflSum > 31) {
                                throw new Exception("Influence sum " + inflSum + " greater than 31");
                            }
                        }
                        for (; j < 4; j++) {
                            var boneInfl = new BoneInfluence() {
                                Weight = 0.0f,
                                BoneIndex = 0,
                            };

                            skin[vertIndex][j] = boneInfl;
                        }
                    }

                    var isFine = vertIndex == VertexCount;

                    if (!isFine) {
                        throw new ApplicationException("Declared vertex count does not match with real vertex count.");
                    }
                }
                #endregion

                var trianglesPacked = ReadPackedVector(reader);

                if (trianglesPacked.ItemCount > 0) {
                    _indexBuffer = UnpackBitVector(trianglesPacked);
                }
            }
            #endregion

            if (version[0] <= 2 || (version[0] == 3 && version[1] <= 4)) { // Colors & Collision triangles for 3.4.2 and earlier
                reader.Position += 24; //Axis-Aligned Bounding Box
                var colorCount = reader.ReadInt32();

                Colors = new float[colorCount * 4];

                for (var v = 0; v < colorCount * 4; v++) {
                    Colors[v] = (float)(reader.ReadByte()) / 0xff;
                }

                var collisionTrianglesCount = reader.ReadInt32();

                reader.Position += collisionTrianglesCount * 4; //UInt32 indices

                var collisionVertexCount = reader.ReadInt32();
            } else {
                // Compressed colors & Local AABB for 3.5.0 to 4.x.x
                // vertex colors are either in streams or packed bits

                if (version[0] < 5) {
                    var colorsPacked = ReadPackedVector(reader);

                    if (colorsPacked.ItemCount > 0) {
                        if (colorsPacked.BitSize == 32) {
                            //4 x 8bit color channels
                            Colors = new float[colorsPacked.Data.Length];

                            for (var v = 0; v < colorsPacked.Data.Length; v++) {
                                Colors[v] = (float)colorsPacked.Data[v] / 0xFF;
                            }
                        } else //not tested
                          {
                            var colorsUnpacked = UnpackBitVector(colorsPacked);
                            var bitmax = 0;//used to convert int value to float

                            for (var b = 0; b < colorsPacked.BitSize; b++) {
                                bitmax |= (1 << b);
                            }

                            Colors = new float[colorsPacked.ItemCount];

                            for (var v = 0; v < colorsPacked.ItemCount; v++) {
                                Colors[v] = (float)colorsUnpacked[v] / bitmax;
                            }
                        }
                    }
                } else {
                    var uvInfo = reader.ReadUInt32();
                }

                reader.Position += 24; //Axis-Aligned Bounding Box
            }

            var meshUsageFlags = reader.ReadInt32();

            if (version[0] >= 5) {
                //int m_BakedConvexCollisionMesh = a_Stream.ReadInt32();
                //a_Stream.Position += m_BakedConvexCollisionMesh;
                //int m_BakedTriangleCollisionMesh = a_Stream.ReadInt32();
                //a_Stream.Position += m_BakedConvexCollisionMesh;
            }

            #region Build face indices
            for (var s = 0; s < subMeshesCount; s++) {
                var firstIndex = SubMeshes[s].FirstByte / 2;

                if (!use16BitIndices) {
                    firstIndex /= 2;
                }

                if (SubMeshes[s].Topology == 0) {
                    for (var i = 0; i < SubMeshes[s].IndexCount / 3; i++) {
                        m_Indices.Add(_indexBuffer[firstIndex + i * 3]);
                        m_Indices.Add(_indexBuffer[firstIndex + i * 3 + 1]);
                        m_Indices.Add(_indexBuffer[firstIndex + i * 3 + 2]);
                        m_materialIDs.Add(s);
                    }
                } else {
                    uint j = 0;
                    for (var i = 0; i < SubMeshes[s].IndexCount - 2; i++) {
                        var fa = _indexBuffer[firstIndex + i];
                        var fb = _indexBuffer[firstIndex + i + 1];
                        var fc = _indexBuffer[firstIndex + i + 2];

                        if ((fa != fb) && (fa != fc) && (fc != fb)) {
                            m_Indices.Add(fa);
                            if ((i % 2) == 0) {
                                m_Indices.Add(fb);
                                m_Indices.Add(fc);
                            } else {
                                m_Indices.Add(fc);
                                m_Indices.Add(fb);
                            }
                            m_materialIDs.Add(s);
                            j++;
                        }
                    }
                    //just fix it
                    SubMeshes[s].IndexCount = j * 3;
                }
            }
            #endregion

            PackedBitVector ReadPackedVector(BinaryReader r) {
                var packedBitVector = new PackedBitVector();

                packedBitVector.ItemCount = r.ReadInt32();

                var dataLength = r.ReadInt32();
                packedBitVector.Data = new byte[dataLength];
                r.Read(packedBitVector.Data, 0, dataLength);
                r.AlignBy(4);

                packedBitVector.BitSize = reader.ReadByte();
                r.AlignBy(4);

                return packedBitVector;
            }
        }

        [NotNull]
        public string Name { get; }

        public IReadOnlyList<SubMesh> SubMeshes { get; }

        public List<uint> m_Indices = new List<uint>(); //use a list because I don't always know the facecount for triangle strips

        public List<int> m_materialIDs = new List<int>();

        public IReadOnlyList<IReadOnlyList<BoneInfluence>> Skin { get; }

        [NotNull, ItemNotNull]
        public float[][,] BindPose { get; }

        public int VertexCount { get; }

        [NotNull]
        public float[] Vertices { get; }

        [NotNull]
        public float[] Normals { get; }

        [CanBeNull]
        public float[] Colors { get; }

        [NotNull]
        public float[] UV1 { get; }

        [NotNull]
        public float[] UV2 { get; }

        [CanBeNull]
        public float[] UV3 { get; }

        [CanBeNull]
        public float[] UV4 { get; }

        [CanBeNull]
        public float[] Tangents { get; }

        [NotNull]
        public uint[] BoneNameHashes { get; }

        [CanBeNull]
        public BlendShapeData Shape { get; }

        [NotNull]
        private static uint[] UnpackBitVector([NotNull] PackedBitVector packedData) {
            var unpackedVectors = new uint[packedData.ItemCount];

            if (packedData.BitSize == 0) {
                packedData.BitSize = (byte)((packedData.Data.Length * 8) / packedData.ItemCount);
            }

            int groupSize = packedData.BitSize;
            var group = new byte[groupSize];
            var groupCount = packedData.ItemCount / 8;

            for (var g = 0; g < groupCount; g++) {
                Buffer.BlockCopy(packedData.Data, g * groupSize, group, 0, groupSize);

                var groupBits = new BitArray(group);

                for (var v = 0; v < 8; v++) {
                    var valueBits = new BitArray(new bool[packedData.BitSize]);

                    for (var b = 0; b < packedData.BitSize; b++) {
                        valueBits.Set(b, groupBits.Get(b + v * packedData.BitSize));
                    }

                    var valueArr = new int[1];

                    valueBits.CopyTo(valueArr, 0);
                    unpackedVectors[v + g * 8] = (uint)valueArr[0];
                }
            }

            var endBytes = packedData.Data.Length - groupCount * groupSize;
            var endVal = packedData.ItemCount - groupCount * 8;

            if (endBytes > 0) {
                Buffer.BlockCopy(packedData.Data, groupCount * groupSize, group, 0, endBytes);
                var groupBits = new BitArray(group);

                for (var v = 0; v < endVal; v++) {
                    var valueBits = new BitArray(new bool[packedData.BitSize]);

                    for (var b = 0; b < packedData.BitSize; b++) {
                        valueBits.Set(b, groupBits.Get(b + v * packedData.BitSize));
                    }

                    var valueArr = new int[1];

                    valueBits.CopyTo(valueArr, 0);
                    unpackedVectors[v + groupCount * 8] = (uint)valueArr[0];
                }
            }

            return unpackedVectors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float BytesToFloat([NotNull] byte[] data) {
            return BytesToFloat(data, _reader.Endian);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float BytesToFloat([NotNull] byte[] data, Endian endian) {
            if (endian == SystemEndian.Type) {
                Array.Reverse(data);
            }

            float result;

            switch (data.Length) {
                case 1:
                    result = data[0] / 255.0f;
                    break;
                case 2:
                    result = Half.ToHalf(data, 0);
                    break;
                case 4:
                    result = BitConverter.ToSingle(data, 0);
                    break;
                default:
                    throw new ArgumentException($"Invalid data byte array length: {data.Length}.", nameof(data));
            }

            return result;
        }

        private static readonly bool ExportUVs = true;

        private static readonly bool ExportDeformers = true;

        private static readonly bool ExportNormals = true;

        private static readonly bool ExportTangents = true;

        private static readonly bool ExportColors = true;

        private readonly EndianBinaryReader _reader;

        [NotNull]
        private readonly uint[] _indexBuffer;
        [NotNull, ItemNotNull]
        private readonly ChannelInfo[] _channels;
        [NotNull, ItemNotNull]
        private readonly StreamInfo[] _streams;

    }
}

