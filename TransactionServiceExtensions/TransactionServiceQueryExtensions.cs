using Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using AgileObjects.ReadableExpressions;

namespace TransactionServiceExtensions
{
    // Could deserialize into a dict first?
    public static class TransactionServiceQueryExtensions
    {
        private static Dictionary<string, PropertyInfo> propInfo = new Dictionary<string, PropertyInfo>();

        private static Dictionary<Type, Func<List<string>, object>> deserializers =
            new Dictionary<Type, Func<List<string>, object>>();

        private static Dictionary<Type, TypeConverter> typeConverters = new Dictionary<Type, TypeConverter>();

        private static Func<List<string>, object> GetDeserializer(Type type, List<string> fields)
        {
            if (!deserializers.TryGetValue(type, out var deserializer))
            {
                deserializer = CreateDeserializer(type, fields);
                deserializers.Add(type, deserializer);
            }

            return deserializer;
        }

        private static TypeConverter GetTypeConverter(Type type)
        {
            if (!typeConverters.TryGetValue(type, out var converter))
            {
                converter = TypeDescriptor.GetConverter(type);
                typeConverters.Add(type, converter);
            }

            return converter;
        }

        private static Func<List<string>, object> CreateDeserializer(Type type, List<string> fields)
        {
            // TODO: object/dynamic
            // TODO: tuple
            // TODO: dictionary

            if (type == typeof(string) || type.IsValueType //type.IsPrimitive ||
                || Nullable.GetUnderlyingType(type) != null)
            {
                return CreatePrimitiveDeserializer(type);
            }

            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                return CreateCtorDeserializer(type);
            }
            else
            {
                return CreatePropDeserializer(type, fields);
            }
        }

        private static Func<List<string>, object> CreatePrimitiveDeserializer(Type type)
        {
            var propInfo = typeof(List<string>).GetProperty("Item");
            var paramListExp = Expression.Parameter(typeof(List<string>), "args");

            var itemPropExp = Expression.Property(
                paramListExp, propInfo, Expression.Constant(0));

            // TODO: Avoid double cast expression
            var castExp = Expression.Convert(
                GetTypeConverterExpression(type, itemPropExp),
                typeof(object));

            return Expression.Lambda<Func<List<string>, object>>(castExp, paramListExp).Compile();
        }

        private static Func<List<string>, object> CreatePropDeserializer(Type type, List<string> fields)
        {
            // Assume correct param order?
            var paramListExp = Expression.Parameter(typeof(List<string>), "fieldValues");
            var itemPropInfo = typeof(List<string>).GetProperty("Item");

            var initializerExps = fields.Select((f, i) =>
            {
                var itemPropExp = Expression.Property(
                    paramListExp,
                    itemPropInfo,
                    Expression.Constant(i));

                // Ignore casing for now
                var propInfo = type.GetProperty(f);

                var castExp = GetTypeConverterExpression(
                    propInfo.PropertyType, itemPropExp);

                return Expression.Bind(
                    propInfo,
                    castExp);
            });

            var initExpression = Expression.MemberInit(
                Expression.New(type),
                initializerExps);

            return Expression.Lambda<Func<List<string>, object>>(initExpression, paramListExp).Compile();
        }

        private static MethodInfo getTypeConverterMethod =
                typeof(TransactionServiceQueryExtensions).GetMethod(
                    nameof(TransactionServiceQueryExtensions.GetTypeConverter),
                    BindingFlags.NonPublic | BindingFlags.Static);

        private static MethodInfo ConvertFromInvariantStringMEthod =
                typeof(TypeConverter).GetMethod(
                    nameof(TypeConverter.ConvertFromInvariantString),
                    new[] { typeof(string) });


        private static Expression GetTypeConverterExpression(
            Type type, Expression accessorExp)
        {
            var getConverterExp = Expression.Call(
                    getTypeConverterMethod,
                    Expression.Constant(type));

            var convertFromStringExp = Expression.Call(
                getConverterExp,
                ConvertFromInvariantStringMEthod,
                new[] { accessorExp });

            return Expression.Convert(
                convertFromStringExp, type);
        }

        private static Func<List<string>, object> CreateCtorDeserializer(Type type)
        {
            var ctor = type.GetConstructors().Single();

            var propInfo = typeof(List<string>).GetProperty("Item");
            var paramListExp = Expression.Parameter(typeof(List<string>), "args");

            var argExps = ctor.GetParameters().Select((p, i) =>
            {
                var itemPropExp = Expression.Property(
                    paramListExp, propInfo, Expression.Constant(i));

                return GetTypeConverterExpression(p.ParameterType, itemPropExp);
            });

            var newExp = Expression.New(ctor, argExps);

            return Expression.Lambda<Func<List<string>, object>>(newExp, paramListExp).Compile();
        }

        public static IEnumerable<T> Query2<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            var rows = doc.Root.Elements();

            var ctor = typeof(T).GetConstructors().FirstOrDefault();
            var fields = rows.Take(1).Elements().Select(e => e.Name.LocalName).ToList();
            var deserializer = GetDeserializer(typeof(T), fields);

            foreach (var row in rows)
            {
                var values = row.Elements()
                    .Select(e => e.Value == string.Empty ? null : e.Value)
                    .ToList();

                yield return (T)deserializer(values);
            }
        }

        public static IEnumerable<T> Query<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            var rows = doc.Root.Elements();

            var ctor = typeof(T).GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                var ctorParams = ctor.GetParameters();

                foreach (var row in rows)
                {
                    var values = ctorParams.Select(p =>
                    {
                        var raw = row.Elements().Single(e => e.Name.LocalName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)).Value;

                        raw = raw == "" ? null : raw;

                        var converter = TypeDescriptor.GetConverter(p.ParameterType);
                        var value = converter.ConvertFromInvariantString(raw);

                        return value;
                    });

                    var obj = (T)ctor.Invoke(values.ToArray());
                    yield return obj;
                }
            }
            else
            {
                var fields = rows.First().Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
                foreach (var row in rows)
                {
                    var obj = Activator.CreateInstance<T>();

                    foreach (var field in fields)
                    {
                        propInfo.TryGetValue(field.Key, out var prop);

                        prop = typeof(T).GetProperty(field.Key);

                        var propType = prop.PropertyType;

                        var converter = TypeDescriptor.GetConverter(propType);

                        var rowValue = row.Elements().Single(e => e.Name.LocalName == field.Key);
                        var value = converter.ConvertFromInvariantString(rowValue.Value == "" ? null : rowValue.Value);

                        prop.SetValue(obj, value);
                    }

                    yield return obj; // Should call .ToList() so deserialization isn't repeated.
                }
            }
        }
    }
}
