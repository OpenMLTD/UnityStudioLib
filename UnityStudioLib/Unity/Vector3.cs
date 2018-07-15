namespace UnityStudio.Unity {
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

        public static Vector3 Zero { get; } = new Vector3(0);

        public static Vector3 One { get; } = new Vector3(1);

    }
}
