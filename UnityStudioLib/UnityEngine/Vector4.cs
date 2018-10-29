using System.Runtime.InteropServices;

namespace UnityStudio.UnityEngine {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector4 {

        public Vector4(float x, float y, float z, float w) {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(float value) {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        public float X;

        public float Y;

        public float Z;

        public float W;

        public static readonly Vector4 Zero = new Vector4(0);

        public static readonly Vector4 One = new Vector4(1);

        public override string ToString() {
            return $"({X}, {Y}, {Z}, {W})";
        }

    }
}
