using System;

namespace UnityStudio.Serialization {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class MonoBehaviorPropertyAttribute : Attribute {

        public string Name { get; set; }

    }
}
