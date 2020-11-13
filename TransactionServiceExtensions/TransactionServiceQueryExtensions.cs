using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace TransactionServiceExtensions
{

    public static class TransactionServiceQueryExtensions
    {
        private static Dictionary<string, PropertyInfo> propInfo = new Dictionary<string, PropertyInfo>();

        public static IEnumerable<T> Query<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            var rows = doc.Root.Elements();

            foreach (var row in rows)
            {
                // Assumes same number of fields per row
                // i.e. null values still have an element
                var fields = row.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);

                T obj;

                var ctor = typeof(T).GetConstructors().FirstOrDefault();

                if (ctor.GetParameters().Any())
                {
                    var ctorParams = ctor.GetParameters();

                    var paramValues = fields.Select(f =>
                    {
                        var paramInfo = ctorParams
                            .Where(p => p.Name.Equals(f.Key, StringComparison.OrdinalIgnoreCase))
                            .Single();

                        var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);
                        var value = converter.ConvertFromInvariantString(f.Value);

                        return value;
                    });

                    obj = (T)ctor.Invoke(paramValues.ToArray());
                }
                else
                {
                    obj = Activator.CreateInstance<T>();

                    foreach (var field in fields)
                    {
                        propInfo.TryGetValue(field.Key, out var prop);

                        prop = prop ?? typeof(T).GetProperty(field.Key);

                        var propType = prop.PropertyType;

                        var converter = TypeDescriptor.GetConverter(propType);

                        var value = converter.ConvertFromInvariantString(field.Value);

                        prop.SetValue(obj, value);
                    }
                }

                yield return obj; // Should call .ToList() so deserialization isn't repeated.
            }
        }
    }
}
