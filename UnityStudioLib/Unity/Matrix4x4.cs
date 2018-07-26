using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace UnityStudio.Unity {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Matrix4x4 {

        public float M11, M12, M13, M14;

        public float M21, M22, M23, M24;

        public float M31, M32, M33, M34;

        public float M41, M42, M43, M44;

        public float this[int row, int column] {
            get {
                switch (row) {
                    case 0: {
                            switch (column) {
                                case 0:
                                    return M11;
                                case 1:
                                    return M12;
                                case 2:
                                    return M13;
                                case 3:
                                    return M14;
                            }
                            break;
                        }
                    case 1: {
                            switch (column) {
                                case 0:
                                    return M21;
                                case 1:
                                    return M22;
                                case 2:
                                    return M23;
                                case 3:
                                    return M24;
                            }
                            break;
                        }
                    case 2: {
                            switch (column) {
                                case 0:
                                    return M31;
                                case 1:
                                    return M32;
                                case 2:
                                    return M33;
                                case 3:
                                    return M34;
                            }
                            break;
                        }
                    case 3: {
                            switch (column) {
                                case 0:
                                    return M41;
                                case 1:
                                    return M42;
                                case 2:
                                    return M43;
                                case 3:
                                    return M44;
                            }
                            break;
                        }
                }

                throw new ArgumentOutOfRangeException();
            }
            set {
                switch (row) {
                    case 0: {
                            switch (column) {
                                case 0:
                                    M11 = value;
                                    return;
                                case 1:
                                    M12 = value;
                                    return;
                                case 2:
                                    M13 = value;
                                    return;
                                case 3:
                                    M14 = value;
                                    return;
                            }
                            break;
                        }
                    case 1: {
                            switch (column) {
                                case 0:
                                    M21 = value;
                                    return;
                                case 1:
                                    M22 = value;
                                    return;
                                case 2:
                                    M23 = value;
                                    return;
                                case 3:
                                    M24 = value;
                                    return;
                            }
                            break;
                        }
                    case 2: {
                            switch (column) {
                                case 0:
                                    M31 = value;
                                    return;
                                case 1:
                                    M32 = value;
                                    return;
                                case 2:
                                    M33 = value;
                                    return;
                                case 3:
                                    M34 = value;
                                    return;
                            }
                            break;
                        }
                    case 3: {
                            switch (column) {
                                case 0:
                                    M41 = value;
                                    return;
                                case 1:
                                    M42 = value;
                                    return;
                                case 2:
                                    M43 = value;
                                    return;
                                case 3:
                                    M44 = value;
                                    return;
                            }
                            break;
                        }
                }
            }
        }

        public static Matrix4x4 FromArray([NotNull] float[,] data) {
            var rows = data.GetLength(0);
            var cols = data.GetLength(1);

            if (rows < 4 || cols < 4) {
                throw new ArgumentException("The data array must at least be a 4-by-4 array.");
            }

            return new Matrix4x4 {
                M11 = data[0, 0],
                M12 = data[0, 1],
                M13 = data[0, 2],
                M14 = data[0, 3],
                M21 = data[1, 0],
                M22 = data[1, 1],
                M23 = data[1, 2],
                M24 = data[1, 3],
                M31 = data[2, 0],
                M32 = data[2, 1],
                M33 = data[2, 2],
                M34 = data[2, 3],
                M41 = data[3, 0],
                M42 = data[3, 1],
                M43 = data[3, 2],
                M44 = data[3, 3]
            };
        }

        public static readonly Matrix4x4 Identity = new Matrix4x4 {
            M11 = 1,
            M22 = 1,
            M33 = 1,
            M44 = 1
        };

    }
}
