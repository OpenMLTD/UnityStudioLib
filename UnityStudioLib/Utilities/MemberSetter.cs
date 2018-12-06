using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using UnityStudio.Serialization;

namespace UnityStudio.Utilities {
    /// <summary>
    /// Accelerated member setter, faster than <see cref="FieldInfo.SetValue(object, object)"/> and <see cref="PropertyInfo.SetValue(object, object)"/>.
    /// However there is compilation cost, so caching should be used.
    /// </summary>
    internal sealed class MemberSetter {

        private MemberSetter() {
            IsValid = false;
        }

        public MemberSetter([NotNull] PropertyInfo property, [CanBeNull] MonoBehaviourPropertyAttribute attribute) {
            _property = property;
            _field = null;
            Attribute = attribute;
            IsValid = true;

            _valueSetter = CompilePropertySetter(property);
        }

        public MemberSetter([NotNull] FieldInfo field, [CanBeNull] MonoBehaviourPropertyAttribute attribute) {
            _property = null;
            _field = field;
            Attribute = attribute;
            IsValid = true;

            _valueSetter = CompileFieldSetter(field);
        }

        public bool IsValid { get; }

        public void SetValue([CanBeNull] object @this, [CanBeNull] object value) {
            if (!IsValid) {
                throw new InvalidOperationException();
            }

            if (_valueSetter == null) {
                throw new InvalidOperationException("Value setter is not ready.");
            }

            _valueSetter.Invoke(@this, value);
        }

        [NotNull]
        public Type GetValueType() {
            if (!IsValid) {
                throw new InvalidOperationException();
            }

            if (_property != null) {
                return _property.PropertyType;
            }

            if (_field != null) {
                return _field.FieldType;
            }

            throw new InvalidOperationException();
        }

        [NotNull]
        private static Action<object, object> CompileFieldSetter([NotNull] FieldInfo field) {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");

            var convertedInstance = Expression.Convert(instanceParam, field.DeclaringType);
            var convertedValue = Expression.Convert(valueParam, field.FieldType);

            var fieldAccess = Expression.Field(convertedInstance, field);
            var assign = Expression.Assign(fieldAccess, convertedValue);

            var lambda = Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam);
            var del = lambda.Compile();

            return del;
        }

        [NotNull]
        private static Action<object, object> CompilePropertySetter([NotNull] PropertyInfo property) {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");

            var convertedInstance = Expression.Convert(instanceParam, property.DeclaringType);
            var convertedValue = Expression.Convert(valueParam, property.PropertyType);

            // Including private setters
            var setterCall = Expression.Call(convertedInstance, property.SetMethod, convertedValue);

            var lambda = Expression.Lambda<Action<object, object>>(setterCall, instanceParam, valueParam);
            var del = lambda.Compile();

            return del;
        }

        [CanBeNull]
        internal MonoBehaviourPropertyAttribute Attribute { get; }

        [NotNull]
        internal static readonly MemberSetter Null = new MemberSetter();

        private readonly Action<object, object> _valueSetter;

        private readonly PropertyInfo _property;
        private readonly FieldInfo _field;

    }
}
