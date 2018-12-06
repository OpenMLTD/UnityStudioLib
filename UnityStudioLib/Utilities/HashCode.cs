using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityStudio.Utilities {
    internal static class HashCode {

        public static int Hash<T>([CanBeNull] T value) {
            if (ReferenceEquals(value, null)) {
                return DefaultHashCode;
            } else {
                return EqualityComparer<T>.Default.GetHashCode(value);
            }
        }

        private const int DefaultHashCode = 0;

    }
}
