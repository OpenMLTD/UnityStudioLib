using System;
using System.Diagnostics;

namespace UnityStudio.Tests {
    public sealed class DoubleToSingleConverter : ISimpleTypeConverter {

        public bool CanConvertFrom(Type sourceType) {
            return sourceType == typeof(double);
        }

        public bool CanConvertTo(Type destinationType) {
            return destinationType == typeof(float);
        }

        public object ConvertTo(object value, Type destinationType) {
            Debug.Assert(value != null, nameof(value) + " != null");
            return (float)(double)value;
        }

    }
}
