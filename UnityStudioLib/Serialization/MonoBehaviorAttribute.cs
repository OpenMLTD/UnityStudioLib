using System;

namespace UnityStudio.Serialization {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class MonoBehaviorAttribute : Attribute {

        public PopulationStrategy PopulationStrategy { get; set; } = PopulationStrategy.OptOut;

        public bool ThrowOnUnmatched { get; set; } = false;

        public Type NamingConventionType { get; set; }

    }
}
