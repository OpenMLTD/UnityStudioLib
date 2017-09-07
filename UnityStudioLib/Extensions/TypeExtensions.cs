using System;
using System.Linq;

namespace UnityStudio.Extensions {
    internal static class TypeExtensions {

        public static bool ImplementsInterface(this Type type, Type interfaceType) {
            var ifs = type.GetInterfaces();
            return ifs.Contains(interfaceType);
        }

        public static bool ImplementsGenericInterface(this Type type, Type interfaceType) {
            var ifs = type.GetInterfaces();
            return ifs.Any(@if => @if.IsGenericType && @if.GetGenericTypeDefinition() == interfaceType);
        }

    }
}
