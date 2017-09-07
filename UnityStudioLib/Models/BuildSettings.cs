using System.Collections.Generic;
using UnityStudio.Extensions;

namespace UnityStudio.Models {
    internal sealed class BuildSettings {

        internal BuildSettings(EndianBinaryReader reader, uint offset, int fileFormatVersion, IReadOnlyList<int> versionComponents, string buildType) {
            reader.Position = offset;

            var levels = reader.ReadInt32();
            for (var i = 0; i < levels; ++i) {
                var length = reader.ReadInt32();
                var level = reader.ReadAlignedString(length);
            }

            if (versionComponents[0] == 5) {
                var preloadPluginCount = reader.ReadInt32();
                for (var i = 0; i < preloadPluginCount; ++i) {
                    var length = reader.ReadInt32();
                    var preloadPlugin = reader.ReadAlignedString(length);
                }
            }

            reader.Position += 4;
            if (fileFormatVersion >= 8) {
                reader.Position += 4;
            }
            if (fileFormatVersion >= 9) {
                reader.Position += 4;
            }
            if (versionComponents[0] == 5 || (versionComponents[0] == 4 && (versionComponents[1] >= 3 || buildType != "a"))) {
                reader.Position += 4;
            }

            var versionStringLength = reader.ReadInt32();
            Version = reader.ReadAlignedString(versionStringLength);
        }

        internal string Version { get; }

    }
}
