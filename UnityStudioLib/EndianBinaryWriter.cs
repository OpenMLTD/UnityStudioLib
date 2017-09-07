using System;
using System.IO;
using System.Text;

namespace UnityStudio {
    public sealed class EndianBinaryWriter : BinaryWriter {

        public EndianBinaryWriter(Stream output, Endian endian)
            : base(output) {
            Endian = endian;
        }

        public EndianBinaryWriter(Stream output, Encoding encoding, Endian endian)
            : base(output, encoding) {
            Endian = endian;
        }

        public EndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen, Endian endian)
            : base(output, encoding, leaveOpen) {
            Endian = endian;
        }

        public Endian Endian { get; set; }

        public void Seek(long position, SeekOrigin origin) {
            switch (origin) {
                case SeekOrigin.Begin:
                    Position = position;
                    break;
                case SeekOrigin.Current:
                    Position += position;
                    break;
                case SeekOrigin.End:
                    Position = BaseStream.Length - position;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
        }

        public long Position {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public override void Write(short value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(int value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(long value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(ushort value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(uint value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(ulong value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(float value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

        public override void Write(double value) {
            if (Endian != SystemEndian.Type) {
                value = EndianHelper.SwapEndian(value);
            }
            base.Write(value);
        }

    }
}
