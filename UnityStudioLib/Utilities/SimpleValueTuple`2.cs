using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityStudio.Utilities {
    internal struct SimpleValueTuple<T1, T2> {

        public SimpleValueTuple([CanBeNull] T1 item1, [CanBeNull] T2 item2) {
            Item1 = item1;
            Item2 = item2;
        }

        [CanBeNull]
        public T1 Item1 { get; }

        [CanBeNull]
        public T2 Item2 { get; }

        public override bool Equals(object obj) {
            if (!(obj is SimpleValueTuple<T1, T2>)) {
                return false;
            }

            var tuple = (SimpleValueTuple<T1, T2>)obj;
            return EqualityComparer<T1>.Default.Equals(Item1, tuple.Item1) &&
                   EqualityComparer<T2>.Default.Equals(Item2, tuple.Item2);
        }

        public override int GetHashCode() {
            var hashCode = -1030903623;
            hashCode = hashCode * -1521134295 + HashCode.Hash(Item1);
            hashCode = hashCode * -1521134295 + HashCode.Hash(Item2);
            return hashCode;
        }
    }
}
