using System;
using JetBrains.Annotations;

namespace UnityStudio.Unity.Animation {
    public sealed class Hand {

        internal Hand() {
            _boneIndices = new int[15];

            for (var i = 0; i < _boneIndices.Length; ++i) {
                _boneIndices[i] = -1;
            }
        }

        public int this[int index] {
            get {
                if (index < 0 || index >= 15) {
                    throw new IndexOutOfRangeException();
                }

                return _boneIndices[index];
            }
            set {
                if (index < 0 || index >= 15) {
                    throw new IndexOutOfRangeException();
                }

                _boneIndices[index] = value;
            }
        }

        public int Thumb1 {
            get => this[Thumb1Index];
            set => this[Thumb1Index] = value;
        }

        public int Thumb2 {
            get => this[Thumb2Index];
            set => this[Thumb2Index] = value;
        }

        public int Thumb3 {
            get => this[Thumb3Index];
            set => this[Thumb3Index] = value;
        }

        public int IndexFinger1 {
            get => this[IndexFinger1Index];
            set => this[IndexFinger1Index] = value;
        }

        public int IndexFinger2 {
            get => this[IndexFinger2Index];
            set => this[IndexFinger2Index] = value;
        }

        public int IndexFinger3 {
            get => this[IndexFinger3Index];
            set => this[IndexFinger3Index] = value;
        }

        public int MiddleFinger1 {
            get => this[MiddleFinger1Index];
            set => this[MiddleFinger1Index] = value;
        }

        public int MiddleFinger2 {
            get => this[MiddleFinger2Index];
            set => this[MiddleFinger2Index] = value;
        }

        public int MiddleFinger3 {
            get => this[MiddleFinger3Index];
            set => this[MiddleFinger3Index] = value;
        }

        public int RingFinger1 {
            get => this[RingFinger1Index];
            set => this[RingFinger1Index] = value;
        }

        public int RingFinger2 {
            get => this[RingFinger2Index];
            set => this[RingFinger2Index] = value;
        }

        public int RingFinger3 {
            get => this[RingFinger3Index];
            set => this[RingFinger3Index] = value;
        }

        public int LittleFinger1 {
            get => this[LittleFinger1Index];
            set => this[LittleFinger1Index] = value;
        }

        public int LittleFinger2 {
            get => this[LittleFinger2Index];
            set => this[LittleFinger2Index] = value;
        }

        public int LittleFinger3 {
            get => this[LittleFinger3Index];
            set => this[LittleFinger3Index] = value;
        }

        public const int Thumb1Index = 0;
        public const int Thumb2Index = 1;
        public const int Thumb3Index = 2;
        public const int IndexFinger1Index = 3;
        public const int IndexFinger2Index = 4;
        public const int IndexFinger3Index = 5;
        public const int MiddleFinger1Index = 6;
        public const int MiddleFinger2Index = 7;
        public const int MiddleFinger3Index = 8;
        public const int RingFinger1Index = 9;
        public const int RingFinger2Index = 10;
        public const int RingFinger3Index = 11;
        public const int LittleFinger1Index = 12;
        public const int LittleFinger2Index = 13;
        public const int LittleFinger3Index = 14;

        [NotNull]
        private readonly int[] _boneIndices;

    }
}
