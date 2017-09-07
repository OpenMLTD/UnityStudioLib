using System;
using System.Collections;
using System.Collections.Generic;
using UnityStudio.Extensions;
using UnityStudio.Models;

namespace UnityStudio.Unity {
    public sealed class MonoBehavior : IAssetObjectContainer {

        internal MonoBehavior(AssetPreloadData preloadData, bool metadataOnly) {
            var assetFile = preloadData.Source;
            var reader = assetFile.FileReader;

            reader.Position = preloadData.Offset;

            var gameObject = assetFile.ReadPPtr();
            var enabled = reader.ReadByte() != 0;

            reader.AlignBy(4);

            var script = assetFile.ReadPPtr();
            var nameLength = reader.ReadInt32();
            var name = reader.ReadAlignedString(nameLength);

            Name = string.IsNullOrEmpty(name) ? $"{preloadData.TypeName} #{preloadData.UniqueID}" : name;

            MetadataOnly = metadataOnly;
            if (metadataOnly) {
                return;
            }

            _variables = preloadData.GetStructure();
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, object> Variables {
            get {
                if (MetadataOnly) {
                    throw new InvalidOperationException();
                }
                return _variables;
            }
        }

        internal bool MetadataOnly { get; }

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

        private readonly IReadOnlyDictionary<string, object> _variables;

    }
}
