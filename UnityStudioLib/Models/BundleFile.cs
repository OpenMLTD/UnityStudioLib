using System;
using System.Collections.Generic;
using LZ4;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityStudio.Extensions;

namespace UnityStudio.Models {
    public sealed class BundleFile : DisposableBase {

        public BundleFile(FileStream fileStream, bool isLz4Compressed)
            : this(fileStream, fileStream.Name, isLz4Compressed) {
        }

        public BundleFile(Stream fileStream, string fileName, bool isLz4Compressed) {
            FullFileName = Path.GetFullPath(fileName);

            var stream = GetRawDataStream(fileStream, isLz4Compressed);

            using (var reader = new EndianBinaryReader(stream, Endian.BigEndian)) {
                _entries = ReadBundle(reader);
            }

            if (stream != fileStream) {
                stream.Dispose();
            }

            AssetFiles = LoadAssetFiles(FullFileName, EngineVersion);
        }

        public string FullFileName { get; }

        public string PlayerVersion { get; private set; }

        public string EngineVersion { get; private set; }

        public IReadOnlyList<AssetFile> AssetFiles { get; }

        protected override void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }

            foreach (var entry in _entries) {
                entry.Dispose();
            }
            foreach (var assetFile in AssetFiles) {
                assetFile.Dispose();
            }
        }

        private static Stream GetRawDataStream(Stream fileStream, bool isLz4Compressed) {
            if (!isLz4Compressed) {
                return fileStream;
            }

            byte[] compressedData;
            int uncompressedSize;
            using (var reader = new EndianBinaryReader(fileStream, Endian.LittleEndian)) {
                var version = reader.ReadInt32();
                uncompressedSize = reader.ReadInt32();
                var compressedSize = reader.ReadInt32();
                var tag = reader.ReadInt32();

                compressedData = new byte[compressedSize];
                reader.Read(compressedData, 0, compressedSize);
            }

            byte[] rawData;
            using (var compressedStream = new MemoryStream(compressedData, false)) {
                using (var decoder = new LZ4Stream(compressedStream, CompressionMode.Decompress)) {
                    rawData = new byte[uncompressedSize];
                    decoder.Read(rawData, 0, uncompressedSize);
                }
            }

            return new MemoryStream(rawData, false);
        }

        private IReadOnlyList<AssetFileEntry> ReadBundle(EndianBinaryReader reader) {
            var signature = reader.ReadStringToNull();
            var format = reader.ReadInt32();
            PlayerVersion = reader.ReadStringToNull();
            EngineVersion = reader.ReadStringToNull();

            switch (signature) {
                case "UnityWeb":
                case "UnityRaw":
                case "\xFA\xFA\xFA\xFA\xFA\xFA\xFA\xFA":
                    if (format < 6) {
                        return ReadFormat(reader, signature);
                    } else if (format == 6) {
                        return ReadFormat6(reader, true);
                    } else {
                        throw new FormatException();
                    }
                case "UnityFS":
                    if (format == 6) {
                        return ReadFormat6(reader, false);
                    } else {
                        throw new FormatException();
                    }
                default:
                    throw new FormatException();
            }
        }

        private IReadOnlyList<AssetFileEntry> ReadFormat(EndianBinaryReader reader, string signature) {
            var bundleSize = reader.ReadInt32();

            var dummy2 = reader.ReadInt16();
            int offset = reader.ReadInt16();
            var dummy3 = reader.ReadInt32();
            var lzmaChunks = reader.ReadInt32();

            var lzmaSize = 0;
            long streamSize = 0;

            for (var i = 0; i < lzmaChunks; ++i) {
                lzmaSize = reader.ReadInt32();
                streamSize = reader.ReadInt32();
            }

            reader.Position = offset;

            switch (signature) {
                case "UnityWeb":
                case "\xFA\xFA\xFA\xFA\xFA\xFA\xFA\xFA":
                    var lzmaBuffer = new byte[lzmaSize];
                    reader.Read(lzmaBuffer, 0, lzmaSize);
                    var decompressedBytes = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(lzmaBuffer);
                    using (var memoryStream = new MemoryStream(decompressedBytes, false)) {
                        using (var r = new EndianBinaryReader(memoryStream, Endian.BigEndian)) {
                            return FillAssetsFilesFromOldFormat(r, 0);
                        }
                    }
                case "UnityRaw":
                    return FillAssetsFilesFromOldFormat(reader, offset);
                default:
                    throw new FormatException();
            }
        }

        private static IReadOnlyList<AssetFileEntry> ReadFormat6(EndianBinaryReader reader, bool padding) {
            var bundleSize = reader.ReadInt64();
            var compressedSize = reader.ReadInt32();
            var uncompressedSize = reader.ReadInt32();
            var flag = reader.ReadInt32();

            if (padding) {
                reader.ReadByte();
            }

            byte[] blocksInfoBytes;
            if ((flag & 0x80) == 0) {
                blocksInfoBytes = reader.ReadBytes(compressedSize);
            } else {
                var originalPosition = reader.Position;
                reader.Position = reader.BaseStream.Length - compressedSize;
                blocksInfoBytes = reader.ReadBytes(compressedSize);
                reader.Position = originalPosition;
            }

            Stream extraStream;
            byte[] rawBytes;
            switch (flag & 0x3f) {
                case 0:
                    extraStream = new MemoryStream(blocksInfoBytes, false);
                    break;
                case 1:
                    rawBytes = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(blocksInfoBytes);
                    extraStream = new MemoryStream(rawBytes, false);
                    break;
                case 2:
                case 3:
                    rawBytes = LZ4Codec.Decode(blocksInfoBytes, 0, blocksInfoBytes.Length, uncompressedSize);
                    extraStream = new MemoryStream(rawBytes, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }

            AssetFileEntry[] assetFilesInMemory;
            using (var blocksInfoReader = new EndianBinaryReader(extraStream, Endian.BigEndian)) {
                blocksInfoReader.Position = 0x10;
                var blockCount = blocksInfoReader.ReadInt32();
                using (var assetsDataStream = new MemoryStream()) {
                    for (var i = 0; i < blockCount; ++i) {
                        uncompressedSize = blocksInfoReader.ReadInt32();
                        compressedSize = blocksInfoReader.ReadInt32();
                        flag = blocksInfoReader.ReadInt16();

                        var compressedBytes = reader.ReadBytes(compressedSize);
                        byte[] uncompressedBytes;
                        switch (flag & 0x3f) {
                            case 0:
                                assetsDataStream.Write(compressedBytes, 0, compressedSize);
                                break;
                            case 1:
                                uncompressedBytes = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(compressedBytes);
                                assetsDataStream.Write(uncompressedBytes, 0, uncompressedSize);
                                break;
                            case 2:
                            case 3:
                                uncompressedBytes = LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, uncompressedSize);
                                assetsDataStream.Write(uncompressedBytes, 0, uncompressedSize);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
                        }
                    }

                    using (var assetsDataReader = new EndianBinaryReader(assetsDataStream, Endian.BigEndian)) {
                        var entryCount = blocksInfoReader.ReadInt32();
                        assetFilesInMemory = new AssetFileEntry[entryCount];

                        for (var i = 0; i < entryCount; ++i) {
                            var entryOffset = blocksInfoReader.ReadInt64();
                            var entrySize = blocksInfoReader.ReadInt64();
                            var dummy = blocksInfoReader.ReadInt32();
                            var fileName = blocksInfoReader.ReadStringToNull();

                            assetsDataReader.Position = entryOffset;
                            var buf = new byte[entrySize];
                            assetsDataReader.Read(buf, 0, (int)entrySize);
                            var memory = new MemoryStream(buf, false);

                            var memoryFile = new AssetFileEntry(memory, fileName, false);
                            assetFilesInMemory[i] = memoryFile;
                        }
                    }
                }
            }

            extraStream.Dispose();

            return assetFilesInMemory;
        }

        private static IReadOnlyList<AssetFileEntry> FillAssetsFilesFromOldFormat(EndianBinaryReader reader, int offset) {
            var fileCount = reader.ReadInt32();
            var assetFilesInMemory = new AssetFileEntry[fileCount];

            for (var i = 0; i < fileCount; ++i) {
                var fileName = reader.ReadStringToNull();
                var fileOffset = reader.ReadInt32();
                fileOffset += offset;
                var fileSize = reader.ReadInt32();

                var originalPosition = reader.Position;
                reader.Position = fileOffset;

                var buf = new byte[fileSize];
                reader.Read(buf, 0, fileSize);
                reader.Position = originalPosition;

                var memory = new MemoryStream(buf, false);
                var memoryFile = new AssetFileEntry(memory, fileName, false);
                assetFilesInMemory[i] = memoryFile;
            }

            return assetFilesInMemory;
        }

        private IReadOnlyList<AssetFile> LoadAssetFiles(string bundleFileName, string engineVersion) {
            var entries = _entries;

            var bundleDirectory = Path.GetDirectoryName(bundleFileName);
            if (bundleDirectory == null) {
                throw new ArgumentException();
            }

            var assetFiles = new List<AssetFile>();

            foreach (var entry in entries) {
                var assetFile = new AssetFile(new EndianBinaryReader(entry.Stream, Endian.BigEndian), entry.FileName, false);

                if (assetFile.FileFormatVersion == 6 && Path.GetFileName(bundleFileName) != "mainData") {
                    assetFile.Version = engineVersion;
                    assetFile.VersionComponents = AssetFile.VersionSplitter.Split(engineVersion).Select(str => Convert.ToInt32(str)).ToArray();
                    assetFile.BuildType = AssetFile.BuildTypePattern.Match(engineVersion).Groups["build"].Value;
                }

                assetFiles.Add(assetFile);
            }

            foreach (var assetFile in assetFiles) {
                foreach (var sharedFile in assetFile.SharedAssetList) {
                    sharedFile.FileName = Path.Combine(bundleDirectory, sharedFile.FileName);
                    var loadedSharedFile = assetFiles.Find(f => f.FullFileName == sharedFile.FileName);
                    if (loadedSharedFile != null) {
                        sharedFile.Index = assetFiles.IndexOf(loadedSharedFile);
                    }
                }
            }

            return assetFiles;
        }

        private readonly IReadOnlyList<AssetFileEntry> _entries;

    }
}

