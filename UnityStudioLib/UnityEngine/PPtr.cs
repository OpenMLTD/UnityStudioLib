namespace UnityStudio.UnityEngine {
    public sealed class PPtr {

        internal PPtr(int fileID, long pathID) {
            FileID = fileID;
            PathID = pathID;
        }

        public int FileID { get; }

        public long PathID { get; }

    }
}
