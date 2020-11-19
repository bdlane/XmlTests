using Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace TransactionServiceExtensions
{
    // Could deserialize into a dict first?
    public static class TransactionServiceQueryExtensions
    {
        private static Dictionary<string, PropertyInfo> propInfo = new Dictionary<string, PropertyInfo>();

        private static Dictionary<Type, Func<List<string>, object>> deserializers =
            new Dictionary<Type, Func<List<string>, object>>();

        private static Dictionary<Type, TypeConverter> typeConverters = new Dictionary<Type, TypeConverter>();

        private static Func<List<string>, object> GetDeserializer(Type type)
        {
            if (!deserializers.TryGetValue(type, out var deserializer))
            {
                deserializer = CreateDeserializerWithCachedTypeConverter(type);
                //deserializer = CreateDeserializer(type);
            }

            //return new Func<List<string>, object>(l => new TimekeeperSimpleCtor("", 1));

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

        private static Func<List<string>, object> CreateDeserializer(Type type)
        {
            var ctor = type.GetConstructors().Single();

            var propInfo = typeof(List<string>).GetProperty("Item");
            var paramListExp = Expression.Parameter(typeof(List<string>), "args");

            var argExps = ctor.GetParameters().Select((p, i) =>
            {
                var paramType = p.ParameterType;
                // Get converter (TODO: cache)
                var itemPropExp = Expression.Property(paramListExp, propInfo, Expression.Constant(i));

                var getConverterExp = Expression.Call(
                    typeof(TypeDescriptor).GetMethod(nameof(TypeDescriptor.GetConverter), new[] { typeof(Type) }),
                    Expression.Constant(paramType));

                var convertFromStringExp = Expression.Call(
                    getConverterExp,
                    typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFromInvariantString), new[] { typeof(string) }),
                    new[] { itemPropExp });

                var castExp = Expression.Convert(convertFromStringExp, paramType);

                return castExp;

                // Convert
                // Cast
            });

            var newExp = Expression.New(ctor, argExps);

            // Construct
            // Return

            var deserializer = Expression.Lambda<Func<List<string>, object>>(newExp, paramListExp).Compile();

            deserializers.Add(type, deserializer);

            return deserializer;
        }

        private static Func<List<string>, object> CreateDeserializerWithCachedTypeConverter(Type type)
        {
            var ctor = type.GetConstructors().Single();

            var propInfo = typeof(List<string>).GetProperty("Item");
            var paramListExp = Expression.Parameter(typeof(List<string>), "args");

            var argExps = ctor.GetParameters().Select((p, i) =>
            {
                var paramType = p.ParameterType;
                // Get converter from cache
                var itemPropExp = Expression.Property(paramListExp, propInfo, Expression.Constant(i));

                var getConverterExp = Expression.Call(
                    typeof(TransactionServiceQueryExtensions).GetMethod(
                        nameof(TransactionServiceQueryExtensions.GetTypeConverter), 
                        BindingFlags.NonPublic | BindingFlags.Static),
                    Expression.Constant(paramType));

                var convertFromStringExp = Expression.Call(
                    getConverterExp,
                    typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFromInvariantString), new[] { typeof(string) }),
                    new[] { itemPropExp });

                var castExp = Expression.Convert(convertFromStringExp, paramType);

                return castExp;

                // Convert
                // Cast
            });

            var newExp = Expression.New(ctor, argExps);

            // Construct
            // Return

            var deserializer = Expression.Lambda<Func<List<string>, object>>(newExp, paramListExp).Compile();

            deserializers.Add(type, deserializer);

            return deserializer;
        }

        public static IEnumerable<T> Query2<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            var rows = doc.Root.Elements();

            //return new List<T> { };

            var ctor = typeof(T).GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                //var ctorParams = ctor.GetParameters();

                var deserializer = GetDeserializer(typeof(T));

                foreach (var row in rows)
                {
                    var values = row.Elements().Select(e => e.Value);

                    yield return (T)deserializer(values.ToList());
                }
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
