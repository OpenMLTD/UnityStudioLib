using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityStudio.Extensions;
using UnityStudio.Models;
using UnityStudio.UnityEngine.MeshParts;

namespace UnityStudio.UnityEngine {
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
                var bindPose = new Matrix4x4[bindPoseCount];

                for (var i = 0; i < bindPoseCount; i++) {
                    bindPose[i] = Matrix4x4.FromArray(new[,] {
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
                    });
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

            if (version[0] < 3 || (version[0] == 3 && version[1] < 5)) { // Vertex Buffer for 3.4.2 and earlier
                var vertexCount = reader.ReadInt32();
                var vertices = new Vector3[vertexCount];

                for (var v = 0; v < vertexCount; v++) {
                    vertices[v] = reader.ReadVector3();
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
                var bindPose = new Matrix4x4[bindPoseCount];

                for (var i = 0; i < bindPoseCount; i++) {
                    bindPose[i] = Matrix4x4.FromArray(new[,] {
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                        {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
                    });
                }

                BindPose = bindPose;

                var uv1Count = reader.ReadInt32();
                UV1 = new Vector2[uv1Count];

                for (var v = 0; v < uv1Count; v++) {
                    UV1[v] = reader.ReadVector2();
                }

                var uv2Count = reader.ReadInt32();
                UV2 = new Vector2[uv2Count];

                for (var v = 0; v < uv2Count * 2; v++) {
                    UV2[v] = reader.ReadVector2();
                }

                if (version[0] == 2 && version[1] <= 5) {
                    var tangentSpaceSize = reader.ReadInt32();

                    Normals = new Vector3[tangentSpaceSize];

                    for (var v = 0; v < tangentSpaceSize; v++) {
                        Normals[v] = reader.ReadVector3();
                        reader.Position += 16; //Vector3f tangent & float handedness 
                    }
                } else { //2.6.0 and later
                    var tangentCount = reader.ReadInt32();
                    reader.Position += tangentCount * 16; //Vector4f

                    var normalCount = reader.ReadInt32();
                    Normals = new Vector3[normalCount];

                    for (var v = 0; v < normalCount; v++) {
                        Normals[v] = reader.ReadVector3();
                    }
                }
            } else { // Vertex Buffer for 3.5.0 and later
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
                    var bindPose = new Matrix4x4[bindPoseCount];

                    for (var i = 0; i < bindPoseCount; i++) {
                        bindPose[i] = Matrix4x4.FromArray(new[,] {
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()},
                            {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
                        });
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

                                streamInfo.Stride += channel.Dimension * (4 / (1 << channel.Format));
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

                            componentByteSize = 4 / (1 << channel.Format);

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
                                case 0: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Vertices = arr;
                                        break;
                                    }
                                case 1: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Normals = arr;
                                        break;
                                    }
                                case 2: {
                                        FloatVectorCopy4(componentsArray, out var arr);
                                        Colors = arr;
                                        break;
                                    }
                                case 3: {
                                        FloatVectorCopy2(componentsArray, out var arr);
                                        UV1 = arr;
                                        break;
                                    }
                                case 4: {
                                        FloatVectorCopy2(componentsArray, out var arr);
                                        UV2 = arr;
                                        break;
                                    }
                                case 5: {
                                        if (version[0] >= 5) {
                                            FloatVectorCopy2(componentsArray, out var arr);
                                            UV3 = arr;
                                        } else {
                                            FloatVectorCopy3(componentsArray, out var arr);
                                            Tangents = arr;
                                        }

                                        break;
                                    }
                                case 6: {
                                        FloatVectorCopy2(componentsArray, out var arr);
                                        UV4 = arr;
                                        break;
                                    }
                                case 7: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Tangents = arr;
                                        break;
                                    }
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
                                case 0: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Vertices = arr;
                                        break;
                                    }
                                case 1: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Normals = arr;
                                        break;
                                    }
                                case 2: {
                                        FloatVectorCopy4(componentsArray, out var arr);
                                        Colors = arr;
                                        break;
                                    }
                                case 3: {
                                        FloatVectorCopy2(componentsArray, out var arr);
                                        UV1 = arr;
                                        break;
                                    }
                                case 4: {
                                        FloatVectorCopy2(componentsArray, out var arr);
                                        UV2 = arr;
                                        break;
                                    }
                                case 5: {
                                        FloatVectorCopy3(componentsArray, out var arr);
                                        Tangents = arr;
                                        break;
                                    }
                            }

                            channel.Offset += (byte)(channel.Dimension * componentByteSize); //safe to cast as byte because strides larger than 255 are unlikely
                            stream.ChannelMask.Set(b, false);
                        }
                    }
                }
                #endregion
            }

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

                    Vertices = new Vector3[verticesPacked.ItemCount / 3];

                    for (var v = 0; v < Vertices.Length; ++v) {
                        var x = ((float)verticesUnpacked[v * 3] / bitmax) * verticesPacked.Range + verticesPacked.Start;
                        var y = ((float)verticesUnpacked[v * 3 + 1] / bitmax) * verticesPacked.Range + verticesPacked.Start;
                        var z = ((float)verticesUnpacked[v * 3 + 2] / bitmax) * verticesPacked.Range + verticesPacked.Start;

                        Vertices[v] = new Vector3(x, y, z);
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

                    UV1 = new Vector2[VertexCount];

                    for (var v = 0; v < UV1.Length; ++v) {
                        var x = ((float)uvUnpacked[v * 2] / bitmax) * uvPacked.Range + uvPacked.Start;
                        var y = ((float)uvUnpacked[v * 2 + 1] / bitmax) * uvPacked.Range + uvPacked.Start;

                        UV1[v] = new Vector2(x, y);
                    }

                    if (uvPacked.ItemCount >= VertexCount * 4) {
                        UV2 = new Vector2[VertexCount];

                        var baseUV2 = VertexCount * 2;

                        for (uint v = 0; v < UV2.Length; ++v) {
                            var x = ((float)uvUnpacked[v * 2 + baseUV2] / bitmax) * uvPacked.Range + uvPacked.Start;
                            var y = ((float)uvUnpacked[v * 2 + baseUV2 + 1] / bitmax) * uvPacked.Range + uvPacked.Start;

                            UV2[v] = new Vector2(x, y);
                        }

                        if (uvPacked.ItemCount >= VertexCount * 6) {
                            UV3 = new Vector2[VertexCount];

                            var baseUV4 = VertexCount * 4;

                            for (uint v = 0; v < UV3.Length; ++v) {
                                var x = ((float)uvUnpacked[v * 2 + baseUV4] / bitmax) * uvPacked.Range + uvPacked.Start;
                                var y = ((float)uvUnpacked[v * 2 + baseUV4 + 1] / bitmax) * uvPacked.Range + uvPacked.Start;

                                UV3[v] = new Vector2(x, y);
                            }

                            if (uvPacked.ItemCount == VertexCount * 8) {
                                UV4 = new Vector2[VertexCount];

                                var baseUV6 = VertexCount * 6;

                                for (uint v = 0; v < UV4.Length; ++v) {
                                    var x = ((float)uvUnpacked[v * 2 + baseUV6] / bitmax) * uvPacked.Range + uvPacked.Start;
                                    var y = ((float)uvUnpacked[v * 2 + baseUV6 + 1] / bitmax) * uvPacked.Range + uvPacked.Start;

                                    UV4[v] = new Vector2(x, y);
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

                        BindPose = new Matrix4x4[bindPosesPacked.ItemCount / 16];

                        for (var i = 0; i < BindPose.Length; i++) {
                            var pose = new Matrix4x4();

                            for (var j = 0; j < 4; j++) {
                                for (var k = 0; k < 4; k++) {
                                    pose[j, k] = (float)((double)bindPosesUnpacked[i * 16 + j * 4 + k] / bitmax) * bindPosesPacked.Range + bindPosesPacked.Start;
                                }
                            }

                            BindPose[i] = pose;
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

                    Normals = new Vector3[normalsPacked.ItemCount / 2];

                    for (var v = 0; v < Normals.Length; ++v) {
                        var x = ((float)normalsUnpacked[v * 2] / bitmax) * normalsPacked.Range + normalsPacked.Start;
                        var y = ((float)normalsUnpacked[v * 2 + 1] / bitmax) * normalsPacked.Range + normalsPacked.Start;
                        var z = (float)Math.Sqrt(1 - x * x - y * y);

                        if (normalSigns[v] == 0) {
                            z = -z;
                        }

                        Normals[v] = new Vector3(x, y, z);
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

                    Tangents = new Vector3[tangentsPacked.ItemCount / 2];

                    for (var v = 0; v < Tangents.Length; ++v) {
                        var x = ((float)tangentsUnpacked[v * 2] / bitmax) * tangentsPacked.Range + tangentsPacked.Start;
                        var y = ((float)tangentsUnpacked[v * 2 + 1] / bitmax) * tangentsPacked.Range + tangentsPacked.Start;
                        var z = (float)Math.Sqrt(1 - x * x - y * y);

                        if (tangentSignsUnpacked[v] == 0) {
                            z = -z;
                        }

                        Tangents[v] = new Vector3(x, y, z);
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

                        Colors = new Vector4[floatColors.ItemCount / 4];

                        for (var v = 0; v < Colors.Length; ++v) {
                            var x = (float)floatColorsUnpacked[v * 4] / bitmax * floatColors.Range + floatColors.Start;
                            var y = (float)floatColorsUnpacked[v * 4 + 1] / bitmax * floatColors.Range + floatColors.Start;
                            var z = (float)floatColorsUnpacked[v * 4 + 2] / bitmax * floatColors.Range + floatColors.Start;
                            var w = (float)floatColorsUnpacked[v * 4 + 3] / bitmax * floatColors.Range + floatColors.Start;

                            Colors[v] = new Vector4(x, y, z, w);
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
                            var boneInfl = new BoneInfluence {
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

                Colors = new Vector4[colorCount];

                for (var v = 0; v < colorCount; ++v) {
                    var x = (float)(reader.ReadByte()) / 0xff;
                    var y = (float)(reader.ReadByte()) / 0xff;
                    var z = (float)(reader.ReadByte()) / 0xff;
                    var w = (float)(reader.ReadByte()) / 0xff;

                    Colors[v] = new Vector4(x, y, z, w);
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
                            Colors = new Vector4[colorsPacked.Data.Length / 4];

                            for (var v = 0; v < Colors.Length; ++v) {
                                var x = (float)colorsPacked.Data[v * 4] / 0xff;
                                var y = (float)colorsPacked.Data[v * 4 + 1] / 0xff;
                                var z = (float)colorsPacked.Data[v * 4 + 2] / 0xff;
                                var w = (float)colorsPacked.Data[v * 4 + 3] / 0xff;

                                Colors[v] = new Vector4(x, y, z, w);
                            }
                        } else //not tested
                          {
                            var colorsUnpacked = UnpackBitVector(colorsPacked);
                            var bitmax = 0;//used to convert int value to float

                            for (var b = 0; b < colorsPacked.BitSize; b++) {
                                bitmax |= (1 << b);
                            }

                            Colors = new Vector4[colorsPacked.ItemCount / 4];

                            for (var v = 0; v < Colors.Length; ++v) {
                                var x = (float)colorsUnpacked[v * 4] / bitmax;
                                var y = (float)colorsUnpacked[v * 4 + 1] / bitmax;
                                var z = (float)colorsUnpacked[v * 4 + 2] / bitmax;
                                var w = (float)colorsUnpacked[v * 4 + 3] / bitmax;

                                Colors[v] = new Vector4(x, y, z, w);
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

                subMeshes[s].FirstIndex = firstIndex;

                if (SubMeshes[s].Topology == 0) {
                    for (var i = 0; i < SubMeshes[s].IndexCount / 3; i++) {
                        _indices.Add(_indexBuffer[firstIndex + i * 3]);
                        _indices.Add(_indexBuffer[firstIndex + i * 3 + 1]);
                        _indices.Add(_indexBuffer[firstIndex + i * 3 + 2]);
                        _materialIDs.Add(s);
                    }
                } else {
                    uint j = 0;
                    for (var i = 0; i < SubMeshes[s].IndexCount - 2; i++) {
                        var fa = _indexBuffer[firstIndex + i];
                        var fb = _indexBuffer[firstIndex + i + 1];
                        var fc = _indexBuffer[firstIndex + i + 2];

                        if ((fa != fb) && (fa != fc) && (fc != fb)) {
                            _indices.Add(fa);

                            if ((i % 2) == 0) {
                                _indices.Add(fb);
                                _indices.Add(fc);
                            } else {
                                _indices.Add(fc);
                                _indices.Add(fb);
                            }

                            _materialIDs.Add(s);

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

            unsafe void FloatVectorCopy2(float[] srcArr, out Vector2[] destArr) {
                const int numFloatInT = 2;
                destArr = new Vector2[srcArr.Length / numFloatInT];

                fixed (void* p = destArr) {
                    Marshal.Copy(srcArr, 0, new IntPtr(p), srcArr.Length);
                }

                //for (var i = 0; i < destArr.Length; ++i) {
                //    var x = srcArr[i * numFloatInT];
                //    var y = srcArr[i * numFloatInT + 1];

                //    destArr[i] = new Vector2(x, y);
                //}
            }

            unsafe void FloatVectorCopy3(float[] srcArr, out Vector3[] destArr) {
                const int numFloatInT = 3;
                destArr = new Vector3[srcArr.Length / numFloatInT];

                fixed (void* p = destArr) {
                    Marshal.Copy(srcArr, 0, new IntPtr(p), srcArr.Length);
                }

                //for (var i = 0; i < destArr.Length; ++i) {
                //    var x = srcArr[i * numFloatInT];
                //    var y = srcArr[i * numFloatInT + 1];
                //    var z = srcArr[i * numFloatInT + 2];

                //    destArr[i] = new Vector3(x, y, z);
                //}
            }

            unsafe void FloatVectorCopy4(float[] srcArr, out Vector4[] destArr) {
                const int numFloatInT = 4;
                destArr = new Vector4[srcArr.Length / numFloatInT];

                fixed (void* p = destArr) {
                    Marshal.Copy(srcArr, 0, new IntPtr(p), srcArr.Length);
                }

                //for (var i = 0; i < destArr.Length; ++i) {
                //    var x = srcArr[i * numFloatInT];
                //    var y = srcArr[i * numFloatInT + 1];
                //    var z = srcArr[i * numFloatInT + 2];
                //    var w = srcArr[i * numFloatInT + 3];

                //    destArr[i] = new Vector4(x, y, z, w);
                //}
            }
        }

        [NotNull]
        public string Name { get; }

        public IReadOnlyList<SubMesh> SubMeshes { get; }

        public IReadOnlyList<uint> Indices => _indices; //use a list because I don't always know the facecount for triangle strips

        public IReadOnlyList<int> MaterialIDs => _materialIDs;

        [NotNull, ItemNotNull]
        public IReadOnlyList<IReadOnlyList<BoneInfluence>> Skin { get; }

        [NotNull]
        public Matrix4x4[] BindPose { get; }

        public int VertexCount { get; }

        [NotNull]
        public Vector3[] Vertices { get; }

        [NotNull]
        public Vector3[] Normals { get; }

        [CanBeNull]
        public Vector4[] Colors { get; }

        [NotNull]
        public Vector2[] UV1 { get; }

        [NotNull]
        public Vector2[] UV2 { get; }

        [CanBeNull]
        public Vector2[] UV3 { get; }

        [CanBeNull]
        public Vector2[] UV4 { get; }

        [CanBeNull]
        public Vector3[] Tangents { get; }

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
            if (endian != SystemEndian.Type) {
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

        private readonly List<uint> _indices = new List<uint>();

        private readonly List<int> _materialIDs = new List<int>();

        [NotNull]
        private readonly uint[] _indexBuffer;
        [NotNull, ItemNotNull]
        private readonly ChannelInfo[] _channels;
        [NotNull, ItemNotNull]
        private readonly StreamInfo[] _streams;

    }
}

