using System;

namespace UnityStudio.Serialization {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class MonoBehaviorIgnoreAttribute : Attribute {
    }
}
