using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using Benchmarks.TestData;
using TransactionServiceExtensions;

namespace Benchmarks
{
    [RankColumn(NumeralSystem.Arabic)]
    public class TransactionSerivceExtensionsBenchmarks
    {
        private string xml;
        [Params(1, 100, 1000)]
        public int NumberOfRecords
        {
            set
            {
                xml = new DataGenerator().Generate(value);
                service = new MockTransactionService(xml);

            }
        }

        private ITransactionService service;

        // Need to cache the generated assembly when using the
        // XmlSerialzier(type, XmlRootAttribute) constructor
        // https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer?redirectedfrom=MSDN&view=net-5.0#dynamically-generated-assemblies
        private readonly XmlSerializer serializer =
            new XmlSerializer(typeof(List<Benchmarks.TestData.Deserialize.Timekeeper>), new XmlRootAttribute("Data"));

        public TransactionSerivceExtensionsBenchmarks()
        {
            //xml = new DataGenerator().Generate();
            //service = new MockTransactionService(xml);
        }

        [Benchmark]
        public List<Timekeeper> QueryT() => service.Query<Timekeeper>("").ToList();

        [Benchmark(Baseline = true)]
        public List<Benchmarks.TestData.Deserialize.Timekeeper> XmlSerializer()
        {
            //var serializer = new XmlSerializer(typeof(List<Benchmarks.TestData.Deserialize.Timekeeper>), new XmlRootAttribute("Data"));
            using (var stringReader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                var result = (List<Benchmarks.TestData.Deserialize.Timekeeper>)serializer.Deserialize(xmlReader);
                return result;
            }
        }

        [Benchmark]
        public List<Benchmarks.TestData.Deserialize.Timekeeper> XmlSerializerStringReader()
        {
            //var serializer = new XmlSerializer(typeof(List<Benchmarks.TestData.Deserialize.Timekeeper>), new XmlRootAttribute("Data"));
            using (var stringReader = new StringReader(xml))
            {
                var result = (List<Benchmarks.TestData.Deserialize.Timekeeper>)serializer.Deserialize(stringReader);
                return result;
            }
        }

        [Benchmark]
        public List<Timekeeper> XDocument()
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var rows = doc.Root.Elements();

            var tks = rows.Select(r => new Timekeeper
            {
                Name = r.Element("Name").Value,
                Age = int.Parse(r.Element("Age").Value),
                DateOfBirth = ParseDateOfBirth(r.Element("DateOfBirth").Value),
                MatterNumber = ParseMatterNumber(r.Element("MatterNumber").Value)
            });

            DateTime? ParseDateOfBirth(string value)
            {
                if (DateTime.TryParse(value, out var result))
                {
                    return result;
                }
                return null;
            }

            MatterNumber ParseMatterNumber(string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return new MatterNumber(value);
                }
                return null;
            }

            return tks.ToList();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //var result = new TransactionSerivceExtensionsBenchmarks().XDocument();
            var summary = BenchmarkRunner.Run<TransactionSerivceExtensionsBenchmarks>();
        }
    }

    public class DataGenerator
    {
        public string Generate(int num)
        {
            var data = GenerateData(num);

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Name = t.Name,
                Age = t.Age,
                DateOfBirth = t.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                MatterNumber = t.MatterNumber?.ToString() ?? ""
            }).ToList();

            return Serialize(serializable);
        }


        static List<Timekeeper> GenerateData(int size)
        {
            var random = new Random();
            return Enumerable.Range(0, size).Select(i => new Timekeeper
            {
                Name = Guid.NewGuid().ToString(),
                Age = random.Next(),
                DateOfBirth = random.Next(2) == 1 ? RandomDay() : (DateTime?)null,
                MatterNumber = random.Next(2) == 1 ? new MatterNumber(Guid.NewGuid().ToString()) : null
            }).ToList();

            DateTime RandomDay()
            {
                DateTime start = new DateTime(1995, 1, 1);
                int range = (DateTime.Today - start).Days;
                return start.AddDays(random.Next(range));
            }
        }

        static string Serialize(List<Benchmarks.TestData.Timekeeper> data)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var serializer = new XmlSerializer(typeof(Data));
            using var textWriter = new StringWriter();
            serializer.Serialize(textWriter, new Data { Timekeepers = data }, ns);
            return textWriter.ToString();
        }
    }


    public class MockTransactionService : ITransactionService
    {
        string xml;

        public MockTransactionService(string xml)
        {
            this.xml = xml ?? throw new ArgumentNullException(nameof(xml));
        }

        public string GetArchetypeData(string queryXml) => xml;
    }

    public class Timekeeper
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public MatterNumber MatterNumber { get; set; }

        public override string ToString() =>
            $"Name; {Name}, " +
            $"Age: {Age}, " +
            $"Date of birth: {DateOfBirth}, " +
            $"Matter number: {MatterNumber}";
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

namespace Benchmarks.TestData
{
    public class Data
    {
        [XmlElement(ElementName = "Timekeeper")]
        public List<Timekeeper> Timekeepers { get; set; }
    }

    [XmlType(TypeName = "Timekeeper")]
    public class Timekeeper
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public string DateOfBirth { get; set; }

        public string MatterNumber { get; set; }

        public override string ToString() =>
            $"Name; {Name}, " +
            $"Age: {Age}, " +
            $"Date of birth: {DateOfBirth}, " +
            $"Matter number: {MatterNumber}";
    }
}

namespace Benchmarks.TestData.Deserialize
{
    public class Data
    {
        [XmlElement(ElementName = "Timekeeper")]
        public List<Timekeeper> Timekeepers { get; set; }
    }

    [XmlType(TypeName = "Timekeeper")]
    public class Timekeeper
    {
        public string Name { get; set; }

        public int Age { get; set; }

        [XmlIgnore]
        public DateTime? DateOfBirth { get; set; }

        [XmlElement(ElementName = "DateOfBirth")]
        public string DateOfBirthString
        {
            get => DateOfBirth.ToString();
            set => DateOfBirth = value != "" ? DateTime.Parse(value) : (DateTime?)null;
        }

        [XmlIgnore]
        public MatterNumber MatterNumber { get; set; }

        [XmlElement(ElementName = "MatterNumber")]
        public string MatterNumberString
        {
            get => MatterNumber.ToString();
            set
            {
                MatterNumber = value != "" ? new MatterNumber { Value = value } : (MatterNumber)null;
            }
        }

        public override string ToString() =>
            $"Name; {Name}, " +
            $"Age: {Age}, " +
            $"Date of birth: {DateOfBirth}, " +
            $"Matter number: {MatterNumber}";
    }


    public class MatterNumber
    {
        [XmlText]
        public string Value { get; set; }
    }
}
