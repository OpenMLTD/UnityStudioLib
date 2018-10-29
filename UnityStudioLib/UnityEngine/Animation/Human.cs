using JetBrains.Annotations;

namespace UnityStudio.UnityEngine.Animation {
    public sealed class Human {

        internal Human() {
            RootTransform = new Transform();
            Skeleton = Skeleton.CreateEmpty();
            SkeletonPose = SkeletonPose.CreateEmpty();
            RightHand = new Hand();
            LeftHand = new Hand();
            Handles = new object[0];
            Colliders = new object[0];
            HumanBoneIndices = new int[25];
            ColliderIndices = new int[25];

            for (var i = 0; i < 25; ++i) {
                HumanBoneIndices[i] = -1;
                ColliderIndices[i] = -1;
            }
        }

        [NotNull]
        public Transform RootTransform { get; internal set; }

        [NotNull]
        public Skeleton Skeleton { get; internal set; }

        [NotNull]
        public SkeletonPose SkeletonPose { get; internal set; }

        [NotNull]
        public Hand RightHand { get; internal set; }

        [NotNull]
        public Hand LeftHand { get; internal set; }

        [NotNull, ItemCanBeNull]
        public object[] Handles { get; internal set; }

        [NotNull, ItemCanBeNull]
        public object[] Colliders { get; internal set; }

        [NotNull]
        public int[] HumanBoneIndices { get; internal set; }

        [NotNull]
        public float[] HumanBoneMasses {
            get => _humanBoneMasses;
            internal set => _humanBoneMasses = value;
        }

        [NotNull]
        public int[] ColliderIndices { get; internal set; }

        public float Scale { get; internal set; } = 1.0f;

        public float ArmTwist { get; internal set; } = 0.5f;

        public float ForeArmTwist { get; internal set; } = 0.5f;

        public float UpperLegTwist { get; internal set; } = 0.5f;

        public float LegTwist { get; internal set; } = 0.5f;

        public float ArmStretch { get; internal set; } = 0.05f;

        public float LegStretch { get; internal set; } = 0.05f;

        public float FeetSpacing { get; internal set; } = 0.0f;

        public bool HasLeftHand { get; internal set; }

        public bool HasRightHand { get; internal set; }

        public bool HasTDoF { get; internal set; }

        private float[] _humanBoneMasses = {
            0.1454546f,
            0.1212121f,
            0.1212121f,
            0.04848485f,
            0.04848485f,
            0.009696971f,
            0.009696971f,
            0.03030303f,
            0.1454546f,
            0.1454546f,
            0.01212121f,
            0.04848485f,
            0.006060607f,
            0.006060607f,
            0.02424243f,
            0.02424243f,
            0.01818182f,
            0.01818182f,
            0.006060607f,
            0.006060607f,
            0.002424243f,
            0.002424243f,
            0f,
            0f,
            0f
        };

    }
}
