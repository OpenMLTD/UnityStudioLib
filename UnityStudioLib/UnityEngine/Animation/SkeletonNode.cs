namespace UnityStudio.UnityEngine.Animation {
    public sealed class SkeletonNode {

        internal SkeletonNode(int parentIndex, int axisIndex) {
            ParentIndex = parentIndex;
            AxisIndex = axisIndex;
        }

        public int ParentIndex { get; }

        public int AxisIndex { get; }

    }
}
