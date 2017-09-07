using UnityStudio.Models;

namespace UnityStudio.Extensions {
    public static class AssetPlatformExtensions {

        public static string GetDescription(this AssetPlatform platform) {
            return DescriptedEnumReflector.Read(platform, typeof(AssetPlatform));
        }

    }
}
