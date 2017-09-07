using System;

namespace UnityStudio {
    internal static class MathHelper {

        public static int GetDigitCount(int value) {
            if (value == 0) {
                return 1;
            }

            var sign = Math.Sign(value);
            var count = 0;
            while (value != 0) {
                value /= 10;
                ++count;
            }

            if (sign < 0) {
                count += 1;
            }

            return count;
        }

    }
}
