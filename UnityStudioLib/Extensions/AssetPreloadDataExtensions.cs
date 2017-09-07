using System;
using UnityStudio.Models;
using UnityStudio.Unity;

namespace UnityStudio.Extensions {
    public static class AssetPreloadDataExtensions {

        public static MonoBehavior LoadAsMonoBehavior(this AssetPreloadData preloadData, bool metadataOnly) {
            if (preloadData.KnownType != KnownClassID.MonoBehaviour) {
                throw new InvalidCastException("The asset preload data is not a MonoBehavior.");
            }
            return new MonoBehavior(preloadData, metadataOnly);
        }

    }
}
