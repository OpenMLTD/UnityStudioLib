namespace UnityStudio.Unity {
    public sealed class Transform {

        public Transform() {
            Translation = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public Transform(Vector3 translation, Quaternion rotation, Vector3 scale) {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }

        public Vector3 Translation { get; set; }

        public Quaternion Rotation { get; set; }

        public Vector3 Scale { get; set; }

    }
}
