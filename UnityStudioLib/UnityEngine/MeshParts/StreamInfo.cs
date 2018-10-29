using System.Collections;
using JetBrains.Annotations;

namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class StreamInfo {

        internal StreamInfo() {
        }

        [NotNull]
        public BitArray ChannelMask { get; internal set; }
        public int Offset { get; internal set; }
        public int Stride { get; internal set; }
        public uint Align { get; internal set; }
        public byte DividerOp { get; internal set; }
        public ushort Frequency { get; internal set; }

    }
}
