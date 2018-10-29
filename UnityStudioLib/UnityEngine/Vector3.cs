using System.Runtime.InteropServices;

namespace UnityStudio.UnityEngine {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector3 {

        public Vector3(float value) {
            X = value;
            Y = value;
            Z = value;
        }

        public Vector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }

        public float X;

        public float Y;

        public float Z;

        public static readonly Vector3 Zero = new Vector3(0);

        public static readonly Vector3 One = new Vector3(1);

        public override string ToString() {
            return $"({X}, {Y}, {Z})";
        }

    }
}
