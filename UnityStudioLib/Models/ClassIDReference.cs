using System;
using System.Collections.Generic;

namespace UnityStudio.Models {
    internal static class ClassIDReference {

        internal static string GetClassName(int id) {
            if (!_isInitialized) {
                InitializeNames();
            }

            return Names[id];
        }

        internal static bool HasClassOf(int id) {
            if (!_isInitialized) {
                InitializeNames();
            }

            return Names.ContainsKey(id);
        }

        internal static bool TryGetClassName(int id, out string className) {
            if (!_isInitialized) {
                InitializeNames();
            }

            return Names.TryGetValue(id, out className);
        }

        private static void InitializeNames() {
            if (_isInitialized) {
                return;
            }

            var enumType = typeof(KnownClassID);
            var names = Enum.GetNames(enumType);
            var values = (int[])Enum.GetValues(enumType);

            for (var i = 0; i < names.Length; ++i) {
                Names[values[i]] = names[i];
            }

            _isInitialized = true;
        }

        private static readonly IDictionary<int, string> Names = new Dictionary<int, string>();
        private static bool _isInitialized;

    }
}
