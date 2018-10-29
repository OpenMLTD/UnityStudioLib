using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityStudio.Utilities {
    internal static class EnumerableUtils {

        public static IEnumerable<(T1, T2)> Zip<T1, T2>([NotNull, ItemCanBeNull] IEnumerable<T1> e1, [NotNull, ItemCanBeNull] IEnumerable<T2> e2) {
            var i1 = e1.GetEnumerator();
            var i2 = e2.GetEnumerator();

            var m1 = i1.MoveNext();
            var m2 = i2.MoveNext();

            while (m1 && m2) {
                yield return (i1.Current, i2.Current);

                m1 = i1.MoveNext();
                m2 = i2.MoveNext();
            }

            i1.Dispose();
            i2.Dispose();
        }

    }
}
