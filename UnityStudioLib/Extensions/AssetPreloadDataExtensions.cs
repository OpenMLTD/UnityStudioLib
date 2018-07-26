using System;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityStudio.Models;
using UnityStudio.UnityEngine;
using UnityStudio.UnityEngine.Animation;

namespace UnityStudio.Extensions {
    public static class AssetPreloadDataExtensions {

        [NotNull]
        public static MonoBehaviour LoadAsMonoBehaviour([NotNull] this AssetPreloadData preloadData, bool metadataOnly) {
            if (preloadData.KnownType != KnownClassID.MonoBehaviour) {
                throw new InvalidCastException("The asset preload data is not a MonoBehavior.");
            }

            return new MonoBehaviour(preloadData, metadataOnly);
        }

        [NotNull]
        public static TextAsset LoadAsTextAsset([NotNull] this AssetPreloadData preloadData, bool metadataOnly) {
            if (preloadData.KnownType != KnownClassID.TextAsset) {
                throw new InvalidCastException("The asset preload data is not a TextAsset.");
            }

            return new TextAsset(preloadData, metadataOnly);
        }

        [NotNull]
        public static Avatar LoadAsAvatar([NotNull] this AssetPreloadData preloadData) {
            if (preloadData.KnownType != KnownClassID.Avatar) {
                throw new InvalidCastException("The asset preload data is not an Avatar.");
            }

            return Avatar.ReadFromAssetPreloadData(preloadData);
        }

        [NotNull]
        public static Mesh LoadAsMesh([NotNull] this AssetPreloadData preloadData) {
            if (preloadData.KnownType != KnownClassID.Mesh) {
                throw new InvalidCastException("The asset preload data is not a Mesh.");
            }

            return new Mesh(preloadData, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasStructMember([NotNull] this AssetPreloadData preloadData, [NotNull] string name) {
            return preloadData.Source.Objects.TryGetValue(preloadData.Type1, out var classStructure)
                   && classStructure.Children.Any(x => x.Name == name);
        }

    }
}
