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
    public sealed class MonoBehaviorSerializer {

        public T Deserialize<T>(MonoBehavior monoBehavior) where T : new() {
            var ret = Deserialize(monoBehavior, typeof(T));
            return (T)ret;
        }

        public object Deserialize([NotNull] MonoBehavior monoBehavior, [NotNull] Type type) {
            if (monoBehavior == null) {
                throw new ArgumentNullException(nameof(monoBehavior));
            }

            return Deserialize((IAssetObjectContainer)monoBehavior, type);
        }

        private object Deserialize([NotNull] IReadOnlyDictionary<string, object> container, [NotNull] Type type) {
            if (container == null) {
                throw new ArgumentNullException(nameof(container));
            }

            var monoBehaviorAttr = type.GetCustomAttribute<MonoBehaviorAttribute>() ?? DefaultClassOptions;

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
                    properties = allProperties.Where(prop => prop.GetCustomAttribute<MonoBehaviorPropertyAttribute>() != null).ToArray();
                    fields = allFields.Where(field => field.GetCustomAttribute<MonoBehaviorPropertyAttribute>() != null).ToArray();
                    break;
                case PopulationStrategy.OptOut:
                    properties = allProperties.Where(prop => prop.GetCustomAttribute<MonoBehaviorIgnoreAttribute>() == null).ToArray();
                    fields = allFields.Where(field => field.GetCustomAttribute<MonoBehaviorIgnoreAttribute>() == null).ToArray();
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
                                            array.SetValue(innerObject, i);
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

                        var dictType = propOrField.GetType();
                        var obj1 = Deserialize(ct, dictType);
                        propOrField.SetValue(obj, obj1);
                    }
                } else {
                    // It is a primitive value.

                    if (kv.Value is byte b && propOrField.GetValueType() == typeof(bool)) {
                        propOrField.SetValue(obj, b != 0);
                    } else {
                        propOrField.SetValue(obj, kv.Value);
                    }
                }
            }

            return obj;
        }

        private static readonly MonoBehaviorAttribute DefaultClassOptions = new MonoBehaviorAttribute();

        private static PropertyOrField FindByName(IReadOnlyList<PropertyInfo> properties, IReadOnlyList<FieldInfo> fields, string name, INamingConvention namingConvention) {
            foreach (var prop in properties) {
                var mbp = prop.GetCustomAttribute<MonoBehaviorPropertyAttribute>();
                var propName = mbp?.Name ?? (namingConvention != null ? namingConvention.GetCorrected(prop.Name) : prop.Name);
                if (propName == name) {
                    return new PropertyOrField(prop);
                }
            }

            foreach (var field in fields) {
                var mbp = field.GetCustomAttribute<MonoBehaviorPropertyAttribute>();
                var fieldName = mbp?.Name ?? (namingConvention != null ? namingConvention.GetCorrected(field.Name) : field.Name);
                if (fieldName == name) {
                    return new PropertyOrField(field);
                }
            }

            return new PropertyOrField();
        }

        private const BindingFlags InternalBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] FilteredNames = {
            "m_GameObject",
            "m_Enabled",
            "m_Script",
            "m_Name"
        };

        private struct PropertyOrField {

            internal PropertyOrField(FieldInfo field) {
                Field = field;
                Property = null;
                IsValid = true;
            }

            internal PropertyOrField(PropertyInfo property) {
                Field = null;
                Property = property;
                IsValid = true;
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

            internal FieldInfo Field { get; }

            internal PropertyInfo Property { get; }

        }

    }
}

