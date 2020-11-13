using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XmlTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Generating data...");
            GenerateData(100000);
            Console.Write("Done");
            Console.WriteLine();
            //return;

            var xmlString = File.ReadAllText("DataLarge.xml");


            //var serializer = new XmlSerializer(typeof(List<Timekeeper>), new XmlRootAttribute("data"));
            //var reader = new StringReader(xmlString);
            //var timekeepers =  (List<Timekeeper>)serializer.Deserialize(reader);

            //Console.WriteLine(timekeepers.Count);
            //foreach (var tk in timekeepers)
            //{
            //    Console.WriteLine(tk.Name);
            //}

            //var converter = new DateTimeConverter();

            //var date = converter.ConvertFromInvariantString("2020-10-01");

            Console.WriteLine("*****Timekeepers (properties)*****");

            var sw = Stopwatch.StartNew();
            var tks = Query<Timekeeper>(xmlString).ToList();
            Console.WriteLine($"Deserialized {tks.Count()} in {sw.Elapsed}");
            Console.WriteLine(tks.First().ToString());

            //foreach (var tk in tks)
            //{
            //    //Console.WriteLine(tk);
            //}

            Console.WriteLine("*****Fee earners (cons*****");

            sw.Restart();
            var fes = Query<FeeEarner>(xmlString);
            Console.WriteLine($"Deserialized {tks.Count()} in {sw.Elapsed}");
            Console.WriteLine(fes.First().ToString());

            //foreach (var fe in fes)
            //{
            //    //Console.WriteLine(fe);
            //}

            Console.WriteLine("Done with serializer");
        }

        static IEnumerable<T> Query<T>(string xml)
        {
            var doc = XDocument.Parse(xml);

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
                        var prop = typeof(T).GetProperty(field.Key);

                        var propType = prop.PropertyType;

                        var converter = TypeDescriptor.GetConverter(propType);

                        var value = converter.ConvertFromInvariantString(field.Value);

                        prop.SetValue(obj, value);
                    }
                }

                yield return obj;
            }

            
        }

        // dynamic vs T
        // Query, QuerySingle, QuerySingleOrDefault etc.
        // ExecuteScalar

        static void GenerateData(int size)
        {
            var random = new Random();
            var data = Enumerable.Range(0, size).Select(i => new TimekeeperSerializable
            {
                Name = Guid.NewGuid().ToString(),
                Age = random.Next(),
                DateOfBirth = random.Next(2) == 1 ? RandomDay().ToString("yyyy-MM-dd") : "",
                MatterNumber = random.Next(2) == 1 ? Guid.NewGuid().ToString() : ""
            }).ToList();

            using (var file = File.OpenWrite("/Users/brendan/Projects/XmlTests/XmlTests/DataLarge.xml"))
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                var serializer = new XmlSerializer(typeof(Data));
                serializer.Serialize(file, new Data { Timekeepers = data }, ns);
            }

            return;

            DateTime RandomDay()
            {
                DateTime start = new DateTime(1995, 1, 1);
                int range = (DateTime.Today - start).Days;
                return start.AddDays(random.Next(range));
            }
        }
    }

    // Handle primitives vs objects? What about enums?

    // Has to be public
    // Case must match
    // Needs a default parameterless constructor
    public class Timekeeper
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public MatterNumber MatterNumber { get; set; }

        public override string ToString() => $"{Name}, {Age} ({DateOfBirth?.ToString() ?? "NULL"}) {MatterNumber}";
    }

    public class Data
    {
        [XmlElement(ElementName = "Timekeeper")]
        public List<TimekeeperSerializable> Timekeepers { get; set; }
    }

    [XmlType(TypeName = "Timekeeper")]
    public class TimekeeperSerializable
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public string DateOfBirth { get; set; }

        public string MatterNumber { get; set; }

        public override string ToString() => $"{Name}, {Age} ({DateOfBirth?.ToString() ?? "NULL"}) {MatterNumber}";
    }

    public class FeeEarner
    {
        public FeeEarner(string name, int age, DateTime? dateOfBirth, MatterNumber matterNumber)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            Name = name;
            Age = age;
            DateOfBirth = dateOfBirth;
            MatterNumber = matterNumber;
        }

        public string Name { get; set; }

        public int Age { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public MatterNumber MatterNumber { get; set; }

        public override string ToString() => $"{Name}, {Age} ({DateOfBirth?.ToString() ?? "NULL"}) {MatterNumber}";
    }

    [TypeConverter(typeof(MatterNumberTypeConverter))]
    public class MatterNumber
    {
        private readonly string s;

        public MatterNumber(string s)
        {
            this.s = s;
        }

        public override string ToString() => s;
    }

    public class MatterNumberTypeConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(MatterNumber);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new MatterNumber(value as string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return new MatterNumber(value as string);
        }
    }
}
