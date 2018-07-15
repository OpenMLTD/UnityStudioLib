using JetBrains.Annotations;

namespace UnityStudio.Unity.MeshParts {
    public sealed class PackedBitVector {

        internal PackedBitVector() {
        }

        public int ItemCount { get; internal set; }
        public float Range { get; internal set; } = 1.0f;
        public float Start { get; internal set; }
        [NotNull]
        public byte[] Data { get; internal set; }
        public byte BitSize { get; internal set; }

    }
}
