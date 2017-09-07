using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityStudio {
    public static class DescriptedEnumReflector {

        public static string Read(Enum value, [NotNull] Type enumType) {
            if (!enumType.IsEnum) {
                throw new ArgumentException($"{nameof(enumType)} must be an enum.", nameof(enumType));
            }

            var flagsAttribute = enumType.GetCustomAttributes(typeof(FlagsAttribute), false);
            if (flagsAttribute.Length == 0) {
                string valueName;
                try {
                    valueName = Enum.GetName(enumType, value);
                } catch (ArgumentException) {
                    valueName = null;
                }
                if (valueName == null) {
                    return value.ToString();
                }

                var fi = enumType.GetField(valueName);
                var dna = fi.GetCustomAttribute<DescriptionAttribute>();
                return dna != null ? dna.Description : value.ToString();
            } else {
                var enumValues = Enum.GetValues(enumType);
                var names = (from Enum v in enumValues
                             where value.HasFlag(v)
                             let fi = enumType.GetField(Enum.GetName(enumType, v))
                             let dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute))
                             select dna != null ? dna.Description : v.ToString())
                            .ToArray();
                return names.Length > 0 ? string.Join(", ", names) : string.Empty;
            }
        }

    }
}
