using JetBrains.Annotations;

namespace UnityStudio.Unity.Animation {
    public sealed class SkeletonPose {

        internal SkeletonPose() {
        }

        [NotNull, ItemNotNull]
        public Transform[] Transforms { get; internal set; }

        [NotNull]
        internal static SkeletonPose CreateEmpty() {
            return new SkeletonPose {
                Transforms = new Transform[0]
            };
        }

    }
}
