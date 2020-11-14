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
