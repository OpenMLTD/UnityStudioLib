using System.IO;

namespace UnityStudio.Models {
    internal sealed class AssetFileEntry : DisposableBase {

        public AssetFileEntry(Stream stream, string fileName)
            : this(stream, fileName, true) {
        }

        public AssetFileEntry(Stream stream, string fileName, bool leaveOpen) {
            Stream = stream;
            FileName = fileName;
            _leaveOpen = leaveOpen;
        }

        public Stream Stream { get; }

        public string FileName { get; }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (!_leaveOpen) {
                    Stream.Dispose();
                }
            }
        }

        private readonly bool _leaveOpen;

    }
}
