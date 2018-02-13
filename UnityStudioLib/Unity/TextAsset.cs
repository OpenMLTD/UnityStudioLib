using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SevenZip.Compression.LZMA;
using UnityStudio.Extensions;
using UnityStudio.Models;

namespace UnityStudio.Unity {
    public sealed class TextAsset {

        public TextAsset([NotNull] AssetPreloadData preloadData, bool metadataOnly) {
            var sourceFile = preloadData.Source;
            var reader = sourceFile.FileReader;

            reader.Position = preloadData.Offset;

            if (sourceFile.Platform == AssetPlatform.UnityPackage) {
                var objectHideFlags = reader.ReadUInt32();
                var prefabParentObject = sourceFile.ReadPPtr();
                var prefabInternal = sourceFile.ReadPPtr();
            }

            Name = reader.ReadAlignedString(reader.ReadInt32());

            var scriptSize = reader.ReadInt32();

            if (metadataOnly) {
                var isLzmaCompressed = reader.ReadByte();

                if (isLzmaCompressed == 93) {
                    reader.Position += 4;
                    preloadData.Size = reader.ReadInt32(); //actualy int64
                    Size = preloadData.Size;
                    reader.Position -= 8;
                } else {
                    preloadData.Size = scriptSize;
                }

                reader.Position += scriptSize - 1;
            } else {
                var script = new byte[scriptSize];

                reader.Read(script, 0, scriptSize);

                if (script[0] == 93) {
                    script = SevenZipHelper.Decompress(script);
                }

                RawData = script;

                Size = scriptSize;
            }

            reader.AlignBy(4);

            PathName = reader.ReadAlignedString(reader.ReadInt32());
        }

        public string Name { get; }

        public int Size { get; }

        [CanBeNull]
        public byte[] RawData { get; }

        public string PathName { get; }

        [CanBeNull]
        public string GetString() {
            if (RawData == null) {
                return null;
            } else {
                var str = Encoding.UTF8.GetString(RawData);

                str = ReplaceNewLine.Replace(str, "\r\n");

                return str;
            }
        }

        private static readonly Regex ReplaceNewLine = new Regex("(?<!\r)\n");

    }
}
