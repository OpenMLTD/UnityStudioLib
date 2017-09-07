namespace UnityStudio.Serialization.Naming {
    public sealed class CamelCaseNamingConvetion : INamingConvention {

        private CamelCaseNamingConvetion() {
        }

        public string GetCorrected(string input) {
            if (string.IsNullOrEmpty(input)) {
                return string.Empty;
            }

            if (!char.IsUpper(input, 0)) {
                return input;
            }

            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

    }
}
