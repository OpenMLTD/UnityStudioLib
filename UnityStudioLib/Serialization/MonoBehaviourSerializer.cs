using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using UnityStudio.Extensions;
using UnityStudio.Models;
using UnityStudio.Serialization.Naming;
using UnityStudio.Unity;

namespace UnityStudio.Serialization {
    public sealed class MonoBehaviourSerializer {

        public T Deserialize<T>(MonoBehaviour monoBehavior) where T : new() {
            var ret = Deserialize(monoBehavior, typeof(T));
            return (T)ret;
        }

        public object Deserialize([NotNull] MonoBehaviour monoBehavior, [NotNull] Type type) {
            if (monoBehavior == null) {
                throw new ArgumentNullException(nameof(monoBehavior));
            }

            return Deserialize((IAssetObjectContainer)monoBehavior, type);
        }

        private object Deserialize([NotNull] IReadOnlyDictionary<string, object> container, [NotNull] Type type) {
            if (container == null) {
                throw new ArgumentNullException(nameof(container));
            }

            var monoBehaviorAttr = type.GetCustomAttribute<MonoBehaviourAttribute>() ?? DefaultClassOptions;

            var allProperties = type.GetProperties(InternalBindings);
            var allFields = type.GetFields(InternalBindings).Where(field => {
                var compilerGenerated = field.GetCustomAttribute<CompilerGeneratedAttribute>();
                // Filter out compiler generated fields.
                return compilerGenerated == null;
            });

            IReadOnlyList<PropertyInfo> properties;
            IReadOnlyList<FieldInfo> fields;
            switch (monoBehaviorAttr.PopulationStrategy) {
                case PopulationStrategy.OptIn:
                    properties = allProperties.Where(prop => prop.GetCustomAttribute<MonoBehaviourPropertyAttribute>() != null).ToArray();
                    fields = allFields.Where(field => field.GetCustomAttribute<MonoBehaviourPropertyAttribute>() != null).ToArray();
                    break;
                case PopulationStrategy.OptOut:
                    properties = allProperties.Where(prop => prop.GetCustomAttribute<MonoBehaviourIgnoreAttribute>() == null).ToArray();
                    fields = allFields.Where(field => field.GetCustomAttribute<MonoBehaviourIgnoreAttribute>() == null).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var naming = monoBehaviorAttr.NamingConventionType != null ?
                (INamingConvention)Activator.CreateInstance(monoBehaviorAttr.NamingConventionType, true) : null;

            var obj = Activator.CreateInstance(type, true);

            foreach (var kv in container) {
                if (FilteredNames.Contains(kv.Key)) {
                    continue;
                }

                var propOrField = FindByName(properties, fields, kv.Key, naming);
                if (!propOrField.IsValid) {
                    if (monoBehaviorAttr.ThrowOnUnmatched) {
                        throw new SerializationException();
                    } else {
                        continue;
                    }
                }

                if (kv.Value is CustomType ct) {
                    if (ct.Variables.Count == 1 && ct.Variables.ContainsKey("Array") && ct.Variables["Array"] is IReadOnlyList<object> varArray) {
                        // It is an array.

                        var arrayType = propOrField.GetValueType();
                        var targetIsArray = arrayType.IsArray;
                        var targetIsCollection = arrayType.ImplementsGenericInterface(typeof(ICollection<>));
                        if (!targetIsArray && !targetIsCollection) {
                            throw new SerializationException();
                        }

                        Type elementType;
                        if (targetIsArray) {
                            elementType = arrayType.GetElementType();
                        } else {
                            elementType = arrayType.GetGenericArguments()[0];
                        }

                        if (elementType == null) {
                            throw new SerializationException();
                        }

                        if (targetIsArray) {
                            // T[]

                            var array = Array.CreateInstance(elementType, varArray.Count);
                            if (varArray.Count == 0) {
                                propOrField.SetValue(obj, array);
                            } else {
                                var sourceIsCustomObjectArray = varArray[0].GetType().ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>));
                                if (sourceIsCustomObjectArray) {
                                    for (var i = 0; i < varArray.Count; ++i) {
                                        var d2 = (IReadOnlyDictionary<string, object>)varArray[i];
                                        var innerObject = d2.First().Value;
                                        if (innerObject.GetType().ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>))) {
                                            var obj1 = Deserialize((IReadOnlyDictionary<string, object>)innerObject, elementType);
                                            array.SetValue(obj1, i);
                                        } else {
                                            if (elementType == typeof(bool) && innerObject is byte byt) {
                                                array.SetValue(byt != 0, i);
                                            } else {
                                                array.SetValue(innerObject, i);
                                            }
                                        }
                                    }
                                } else {
                                    for (var i = 0; i < varArray.Count; ++i) {
                                        array.SetValue(varArray[i], i);
                                    }
                                }
                            }

                            propOrField.SetValue(obj, array);
                        } else {
                            // ICollection<T>

                            var ctor = arrayType.GetConstructor(InternalBindings, null, Type.EmptyTypes, null);
                            if (ctor == null) {
                                throw new SerializationException();
                            }

                            var collection = ctor.Invoke(null);
                            var addMethod = arrayType.GetMethod("Add", InternalBindings);

                            if (addMethod == null) {
                                throw new InvalidCastException("The type is not a collection.");
                            }

                            if (varArray.Count == 0) {
                                propOrField.SetValue(obj, collection);
                            } else {
                                var sourceIsCustomObjectArray = varArray[0].GetType().ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>));
                                if (sourceIsCustomObjectArray) {
                                    foreach (var arrElem in varArray) {
                                        var d2 = (IReadOnlyDictionary<string, object>)arrElem;
                                        var innerObject = d2.First().Value;
                                        if (innerObject.GetType().ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>))) {
                                            var obj1 = Deserialize((IReadOnlyDictionary<string, object>)innerObject, elementType);
                                            addMethod.Invoke(collection, new[] { obj1 });
                                        } else {
                                            addMethod.Invoke(collection, new[] { innerObject });
                                        }
                                    }
                                } else {
                                    foreach (var arrElem in varArray) {
                                        addMethod.Invoke(collection, new[] { arrElem });
                                    }
                                }
                            }

                            propOrField.SetValue(obj, collection);
                        }
                    } else {
                        // It is an object.

                        var dictType = propOrField.GetValueType();
                        var obj1 = Deserialize(ct, dictType);
                        propOrField.SetValue(obj, obj1);
                    }
                } else {
                    // It is a primitive value.

                    var acceptedType = propOrField.GetValueType();
                    var serializedValueType = kv.Value.GetType();

                    if (serializedValueType == acceptedType) {
                        propOrField.SetValue(obj, kv.Value);
                    } else {
                        // A little convertion is needed here...
                        var converted = false;

                        // Is the situation: target type is an enum, recorded type is a integer type?
                        if (acceptedType.IsEnum) {
                            if (serializedValueType == typeof(byte) || serializedValueType == typeof(sbyte) ||
                                serializedValueType == typeof(ushort) || serializedValueType == typeof(short) ||
                                serializedValueType == typeof(uint) || serializedValueType == typeof(int) ||
                                serializedValueType == typeof(ulong) || serializedValueType == typeof(long)) {
                                var enumValue = Enum.ToObject(acceptedType, kv.Value);

                                propOrField.SetValue(obj, enumValue);

                                converted = true;
                            }
                        }

                        if (!converted) {
                            do {
                                if (kv.Value is byte b && acceptedType == typeof(bool)) {
                                    // A special case: Unity uses UInt8 to store booleans.
                                    propOrField.SetValue(obj, b != 0);
                                    converted = true;

                                    break;
                                }

                                var converterType = propOrField.Attribute?.ConverterType;

                                if (converterType == null) {
                                    // No specified converter found, then fail.
                                    break;
                                }

                                if (!converterType.ImplementsInterface(typeof(ISimpleTypeConverter))) {
                                    throw new ArgumentException("Converter does not implement " + nameof(ISimpleTypeConverter) + ".");
                                }

                                ISimpleTypeConverter converter;

                                // Retrieve or create specified converter.
                                if (_createdTypeConverters.ContainsKey(converterType)) {
                                    converter = _createdTypeConverters[converterType];
                                } else {
                                    converter = (ISimpleTypeConverter)Activator.CreateInstance(converterType);
                                    _createdTypeConverters[converterType] = converter;
                                }

                                if (!converter.CanConvertFrom(serializedValueType) || !converter.CanConvertTo(acceptedType)) {
                                    // If the converter cannot handle desired conversion, fail.
                                    break;
                                }

                                var convertedValue = converter.ConvertTo(kv.Value, acceptedType);

                                propOrField.SetValue(obj, convertedValue);

                                converted = true;
                            } while (false);
                        }

                        if (!converted) {
                            throw new InvalidCastException($"Serialized type {serializedValueType} cannot be converted to {acceptedType}.");
                        }
                    }
                }
            }

            return obj;
        }

        private static PropertyOrField FindByName(IReadOnlyList<PropertyInfo> properties, IReadOnlyList<FieldInfo> fields, string name, INamingConvention namingConvention) {
            foreach (var prop in properties) {
                var mbp = prop.GetCustomAttribute<MonoBehaviourPropertyAttribute>();
                var propName = !string.IsNullOrEmpty(mbp?.Name) ? mbp.Name : (namingConvention != null ? namingConvention.GetCorrected(prop.Name) : prop.Name);
                if (propName == name) {
                    return new PropertyOrField(prop, mbp);
                }
            }

            foreach (var field in fields) {
                var mbp = field.GetCustomAttribute<MonoBehaviourPropertyAttribute>();
                var fieldName = !string.IsNullOrEmpty(mbp?.Name) ? mbp.Name : (namingConvention != null ? namingConvention.GetCorrected(field.Name) : field.Name);
                if (fieldName == name) {
                    return new PropertyOrField(field, mbp);
                }
            }

            return new PropertyOrField();
        }

        private const BindingFlags InternalBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly MonoBehaviourAttribute DefaultClassOptions = new MonoBehaviourAttribute();

        private readonly Dictionary<Type, ISimpleTypeConverter> _createdTypeConverters = new Dictionary<Type, ISimpleTypeConverter>(10);

        private static readonly string[] FilteredNames = {
            "m_GameObject",
            "m_Enabled",
            "m_Script",
            "m_Name"
        };

        private struct PropertyOrField {

            internal PropertyOrField(FieldInfo field, MonoBehaviourPropertyAttribute attribute) {
                Field = field;
                Property = null;
                IsValid = true;
                Attribute = attribute;
            }

            internal PropertyOrField(PropertyInfo property, MonoBehaviourPropertyAttribute attribute) {
                Field = null;
                Property = property;
                IsValid = true;
                Attribute = attribute;
            }

            internal void SetValue(object @this, object value) {
                if (!IsValid) {
                    throw new InvalidOperationException();
                }
                Property?.SetValue(@this, value);
                Field?.SetValue(@this, value);
            }

            internal Type GetValueType() {
                if (!IsValid) {
                    throw new InvalidOperationException();
                }
                if (Property != null) {
                    return Property.PropertyType;
                }
                if (Field != null) {
                    return Field.FieldType;
                }
                throw new InvalidOperationException();
            }

            internal bool IsValid { get; }

            [CanBeNull]
            internal FieldInfo Field { get; }

            [CanBeNull]
            internal PropertyInfo Property { get; }

            [CanBeNull]
            internal MonoBehaviourPropertyAttribute Attribute { get; }

        }

    }
}

