using System.Collections;
using System.Collections.Generic;

namespace UnityStudio.Models {
    public sealed class CustomType : IAssetObjectContainer {

        internal CustomType() {
        }

        public string Name { get; internal set; }

        public string TypeName { get; internal set; }

        public int Level { get; internal set; }

        public IReadOnlyDictionary<string, object> Variables { get; internal set; }

        public override string ToString() {
            return $"{base.ToString()} ({TypeName})";
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            return Variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)Variables).GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => Variables.Count;

        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) {
            return Variables.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) {
            return Variables.TryGetValue(key, out value);
        }

        object IReadOnlyDictionary<string, object>.this[string key] => Variables[key];

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Variables.Keys;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Variables.Values;

    }
}
