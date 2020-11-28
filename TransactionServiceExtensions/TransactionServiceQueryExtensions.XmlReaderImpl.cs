using Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using AgileObjects.ReadableExpressions;
using System.Xml;
using System.IO;

namespace TransactionServiceExtensions.XmlReaderImpl
{
    // Could deserialize into a dict first?
    public static class TransactionServiceQueryExtensions
    {
        private static Dictionary<string, PropertyInfo> propInfo = new Dictionary<string, PropertyInfo>();

        private static Dictionary<Type, Func<XmlReader, object>> deserializers =
            new Dictionary<Type, Func<XmlReader, object>>();

        private static Dictionary<Type, TypeConverter> typeConverters = new Dictionary<Type, TypeConverter>();

        private static Func<XmlReader, object> GetDeserializer(Type type, List<string> fields)
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

        private static Func<XmlReader, object> CreateDeserializer(Type type, List<string> fields)
        {
            // TODO: object/dynamic
            // TODO: tuple
            // TODO: dictionary

            //if (type == typeof(string) || type.IsValueType //type.IsPrimitive ||
            //    || Nullable.GetUnderlyingType(type) != null)
            //{
            //    return CreatePrimitiveDeserializer(type);
            //}

            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                return CreateCtorDeserializer(type);
            }
            else
            {
                return null;
                //return CreatePropDeserializer(type, fields);
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

        private static Func<XmlReader, object> CreateCtorDeserializer(Type type)
        {
            var ctor = type.GetConstructors().Single();

            var paramReaderExp = Expression.Parameter(typeof(XmlReader), "reader");
            var readContentMethodInfo = typeof(XmlReader).GetMethod(nameof(XmlReader.ReadElementContentAsString), new Type[] { });

            var argExps = ctor.GetParameters().Select((p, i) =>
            {
                var readContentExp = Expression.Call(paramReaderExp, readContentMethodInfo);

                return GetTypeConverterExpression(p.ParameterType, readContentExp);
            });

            var newExp = Expression.New(ctor, argExps);

            return Expression.Lambda<Func<XmlReader, object>>(newExp, paramReaderExp).Compile();
        }

        public static IEnumerable<T> QueryReader2<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);

            var reader = XmlReader.Create(new StringReader(result), new XmlReaderSettings { IgnoreWhitespace = true });

            reader.ReadToFollowing("Data");
            //reader.Read(); // Row 
            //var rowNodeName = reader.LocalName;

            var items = new List<T>();

            var fields = Enumerable.Empty<string>().ToList();
            var deserializer = GetDeserializer(typeof(T), fields);

            while (reader.Read() && reader.LocalName != "Data") // Row
            {
                reader.Read(); // First column

                var item = (T)deserializer(reader);

                items.Add(item);
            }

            return items.AsEnumerable();

            //var timekeeper = new TimekeeperProps
            //{
            //    Name = reader.ReadElementContentAsString(),
            //    Age = reader.ReadElementContentAsInt(),
            //    DateOfBirth = ParseDateOfBirth(reader.ReadElementContentAsString()),
            //    MatterNumber = ParseMatterNumber(reader.ReadElementContentAsString())
            //};
        }

        // Can only have one! Dict? Dynamic? Cast?
        public static IEnumerable<IDictionary<string, string> >QueryReaderT(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            var rows = doc.Root.Elements();

            return rows.Select(r => r.Elements()
                .ToDictionary(
                e => e.Name.LocalName, e => (e.Value == string.Empty ? null : e.Value)));
        }

        public static IEnumerable<T> QueryReader<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);

            var ctor = typeof(T).GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                var ctorParams = ctor.GetParameters().ToList();

                var reader = XmlReader.Create(new StringReader(result), new XmlReaderSettings { IgnoreWhitespace = true });

                reader.ReadToFollowing("Data");
                //reader.Read(); // Row 
                //var rowNodeName = reader.LocalName;

                var items = new List<T>();

                var fields = Enumerable.Empty<string>().ToList();

                while (reader.Read() && reader.LocalName != "Data") // Row
                {
                    reader.Read(); // First column

                    var ctorParamsCount = ctorParams.Count;

                    var values = ctorParams.Select(p =>
                    {
                        var raw = reader.ReadElementContentAsString();

                        raw = raw == "" ? null : raw;

                        // Assume same order
                        var converter = GetTypeConverter(p.ParameterType);
                        return converter.ConvertFromInvariantString(raw);
                    });

                    var item = (T)ctor.Invoke(values.ToArray());

                    items.Add(item);
                }

                return items.AsEnumerable();
            }
            else
            {
                throw new NotSupportedException();
                //var fields = rows.First().Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
                //foreach (var row in rows)
                //{
                //    var obj = Activator.CreateInstance<T>();

                //    foreach (var field in fields)
                //    {
                //        propInfo.TryGetValue(field.Key, out var prop);

                //        prop = typeof(T).GetProperty(field.Key);

                //        var propType = prop.PropertyType;

                //        var converter = TypeDescriptor.GetConverter(propType);

                //        var rowValue = row.Elements().Single(e => e.Name.LocalName == field.Key);
                //        var value = converter.ConvertFromInvariantString(rowValue.Value == "" ? null : rowValue.Value);

                //        prop.SetValue(obj, value);
                //    }

                //    yield return obj; // Should call .ToList() so deserialization isn't repeated.
                //}
            }
        }
    }
}
