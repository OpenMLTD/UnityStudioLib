namespace UnityStudio.UnityEngine.MeshParts {
    public sealed class ChannelInfo {

        internal ChannelInfo() {
        }

        public byte Stream { get; internal set; }
        public byte Offset { get; internal set; }
        public byte Format { get; internal set; }
        public byte Dimension { get; internal set; }

    }
}
