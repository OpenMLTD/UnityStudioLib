using JetBrains.Annotations;

namespace UnityStudio.UnityEngine.Animation {
    public sealed class Skeleton {

        internal Skeleton() {
        }

        [NotNull, ItemNotNull]
        public SkeletonNode[] Nodes { get; internal set; }

        [NotNull]
        public uint[] NodeIDs { get; internal set; }

        [NotNull, ItemCanBeNull]
        public object[] Axes { get; internal set; }

        [NotNull]
        internal static Skeleton CreateEmpty() {
            var skeleton = new Skeleton();

            skeleton.Nodes = new SkeletonNode[0];
            skeleton.NodeIDs = new uint[0];
            skeleton.Axes = new object[0];

            return skeleton;
        }

    }
}
