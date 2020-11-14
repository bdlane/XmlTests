using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace TransactionServiceExtensions
{
    // Could deserialize into a dict first?
    public static class TransactionServiceQueryExtensions
    {
        private static Dictionary<string, PropertyInfo> propInfo = new Dictionary<string, PropertyInfo>();

        public static IEnumerable<T> Query<T>(this ITransactionService service, string queryXml)
        {
            var result = service.GetArchetypeData(queryXml);
            var doc = XDocument.Parse(result);
            // yield break;
            var rows = doc.Root.Elements();

            var fields = rows.First().Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);

            T obj;

            var ctor = typeof(T).GetConstructors().FirstOrDefault();

            if (ctor.GetParameters().Any())
            {
                foreach (var row in rows)
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
                    yield return obj;
                }
            }
            else
            {
                foreach (var row in rows)
                {
                    obj = Activator.CreateInstance<T>();

                    foreach (var field in fields)
                    {
                        propInfo.TryGetValue(field.Key, out var prop);

                        prop =  typeof(T).GetProperty(field.Key);

                        var propType = prop.PropertyType;

                        var converter = TypeDescriptor.GetConverter(propType);

                        var rowValue = row.Elements().Single(e => e.Name.LocalName == field.Key);

                        var value = converter.ConvertFromInvariantString(rowValue.Value);

                        prop.SetValue(obj, value);
                    }

                    yield return obj; // Should call .ToList() so deserialization isn't repeated.
                }
            }
        }
    }
}
