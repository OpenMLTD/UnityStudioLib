using System;
using UnityStudio.Serialization.Naming;

namespace UnityStudio.Serialization {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class MonoBehaviorAttribute : Attribute {

        public PopulationStrategy PopulationStrategy { get; set; } = PopulationStrategy.OptOut;

        public bool ThrowOnUnmatched { get; set; } = false;

        /// <summary>
        /// Used to specify the naming style of serialized fields in the input file.
        /// The assigned type must implement <see cref="INamingConvention"/> interface.
        /// </summary>
        public Type NamingConventionType { get; set; }

    }
}
