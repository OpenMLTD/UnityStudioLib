using System;
using JetBrains.Annotations;
using UnityStudio.Models;
using UnityStudio.Unity;

namespace UnityStudio.Extensions {
    public static class AssetPreloadDataExtensions {

        [NotNull]
        public static MonoBehavior LoadAsMonoBehavior([NotNull] this AssetPreloadData preloadData, bool metadataOnly) {
            if (preloadData.KnownType != KnownClassID.MonoBehaviour) {
                throw new InvalidCastException("The asset preload data is not a MonoBehavior.");
            }

            return new MonoBehavior(preloadData, metadataOnly);
        }

        [NotNull]
        public static TextAsset LoadAsTextAsset([NotNull] this AssetPreloadData preloadData, bool metadataOnly) {
            if (preloadData.KnownType != KnownClassID.TextAsset) {
                throw new InvalidCastException("The asset preload data is not a TextAsset.");
            }

            return new TextAsset(preloadData, metadataOnly);
        }

    }
}
