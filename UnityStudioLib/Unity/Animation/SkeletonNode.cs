namespace UnityStudio.Unity.Animation {
    public sealed class SkeletonNode {

        internal SkeletonNode(int parentId, int axesId) {
            ParentId = parentId;
            AxesId = axesId;
        }

        public int ParentId { get; }

        public int AxesId { get; }

    }
}
