namespace UnityStudio.Unity {
    public struct Vector2 {

        public Vector2(float x, float y) {
            X = x;
            Y = y;
        }

        public Vector2(float value) {
            X = value;
            Y = value;
        }

        public float X;

        public float Y;

        public static readonly Vector2 Zero = new Vector2(0);

        public static readonly Vector2 One = new Vector2(1);

        public override string ToString() {
            return $"({X}, {Y})";
        }

    }
}
