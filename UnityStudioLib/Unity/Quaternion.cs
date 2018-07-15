namespace UnityStudio.Unity {
    public struct Quaternion {

        public float X;

        public float Y;

        public float Z;

        public float W;

        public static Quaternion Identity { get; } = new Quaternion {
            W = 1.0f
        };

        public override string ToString() {
            return $"Axis: ({X}, {Y}, {Z}), Angle: {W}";
        }

    }
}
