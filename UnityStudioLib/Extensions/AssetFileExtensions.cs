using UnityStudio.Models;
using UnityStudio.Unity;

namespace UnityStudio.Extensions {
    public static class AssetFileExtensions {

        internal static PPtr ReadPPtr(this AssetFile assetFile) {
            var reader = assetFile.FileReader;
            var fileID = reader.ReadInt32();

            if (0 <= fileID && fileID < assetFile.SharedAssetList.Count) {
                fileID = assetFile.SharedAssetList[fileID].Index;
            } else {
                fileID = -1;
            }

            long pathID;
            if (assetFile.FileFormatVersion < 14) {
                pathID = reader.ReadInt32();
            } else {
                pathID = reader.ReadInt64();
            }

            return new PPtr(fileID, pathID);
        }

    }
}
