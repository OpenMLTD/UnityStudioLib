using JetBrains.Annotations;

namespace UnityStudio.Utilities {
    internal struct SimpleValueTuple<T1, T2> {

        public SimpleValueTuple([CanBeNull] T1 item1, [CanBeNull] T2 item2) {
            Item1 = item1;
            Item2 = item2;
        }

        public T1 Item1 { get; }

        public T2 Item2 { get; }

    }
}
