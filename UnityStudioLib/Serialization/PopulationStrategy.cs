namespace UnityStudio.Serialization {
    public enum PopulationStrategy {

        /// <summary>
        /// Populate only properties or fields with <see cref="MonoBehaviorPropertyAttribute"/> attribute.
        /// </summary>
        OptIn,

        /// <summary>
        /// Populate every property or field, unless it has marked with a <see cref="MonoBehaviorIgnoreAttribute"/> attribute.
        /// </summary>
        OptOut

    }
}
