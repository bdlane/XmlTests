using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using Benchmarks.TestData;
using Data;
using TransactionServiceExtensions;

namespace Benchmarks
{
    [RankColumn(NumeralSystem.Arabic)]
    //[MemoryDiagnoser]
    public class TransactionSerivceExtensionsBenchmarks
    {
        private string xml;

        [Params(1, 100, 1000, 10000)]
        public int NumberOfRecords
        {
            set
            {
                xml = new DataGenerator().Generate(value);
                service = new MockTransactionService(xml);
            }
        }

        private ITransactionService service;
        private static TypeConverter converter;

        // Need to cache the generated assembly when using the
        // XmlSerialzier(type, XmlRootAttribute) constructor
        // https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer?redirectedfrom=MSDN&view=net-5.0#dynamically-generated-assemblies
        private readonly XmlSerializer serializer =
            new XmlSerializer(typeof(List<Benchmarks.TestData.Deserialize.Timekeeper>), new XmlRootAttribute("Data"));

        //[Benchmark]
        //public int Convert()
        //{
        //    var converter = TypeDescriptor.GetConverter(typeof(int));

        //    return (int)converter.ConvertFromInvariantString("123");
        //}

        //[Benchmark]
        //public int ConvertCached()
        //{
        //    if (converter == null)
        //    {
        //        converter = TypeDescriptor.GetConverter(typeof(int));
        //    };

        //    return (int)converter.ConvertFromInvariantString("123");
        //}

        //[Benchmark]
        //public List<TimekeeperProps> QueryTProps() => service.Query<TimekeeperProps>("").ToList();


        [Benchmark]
        public List<TimekeeperCtor> QueryTCtor() => service.Query<TimekeeperCtor>("").ToList();

        [Benchmark]
        public List<TimekeeperSimpleCtor> QueryTCtor2() => service.Query2<TimekeeperSimpleCtor>("").ToList();

        [Benchmark(Baseline = true)]
        public List<Benchmarks.TestData.Deserialize.Timekeeper> XmlSerializerImpl()
        {
            using (var stringReader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                var result = (List<Benchmarks.TestData.Deserialize.Timekeeper>)serializer.Deserialize(xmlReader);
                return result;
            }
        }

        //    //[Benchmark]
        //    //public List<Benchmarks.TestData.Deserialize.Timekeeper> XmlSerializerStringReader()
        //    //{
        //    //    using (var stringReader = new StringReader(xml))
        //    //    {
        //    //        var result = (List<Benchmarks.TestData.Deserialize.Timekeeper>)serializer.Deserialize(stringReader);
        //    //        return result;
        //    //    }
        //    //}

        //    //[Benchmark]
        //    //public List<TimekeeperProps> XDocumentImpl()
        //    //{
        //    //    var doc = XDocument.Parse(xml);
        //    //    var rows = doc.Root.Elements();

        //    //    var tks = rows.Select(r => new TimekeeperProps
        //    //    {
        //    //        Name = r.Element("Name").Value,
        //    //        Age = int.Parse(r.Element("Age").Value),
        //    //        DateOfBirth = ParseDateOfBirth(r.Element("DateOfBirth").Value),
        //    //        MatterNumber = ParseMatterNumber(r.Element("MatterNumber").Value)
        //    //    });

        //    //    return tks.ToList();
        //    //}

        //    //DateTime? ParseDateOfBirth(string value)
        //    //{
        //    //    if (DateTime.TryParse(value, out var result))
        //    //    {
        //    //        return result;
        //    //    }
        //    //    return null;
        //    //}

        //    //MatterNumber ParseMatterNumber(string value)
        //    //{
        //    //    if (!string.IsNullOrEmpty(value))
        //    //    {
        //    //        return new MatterNumber(value);
        //    //    }
        //    //    return null;
        //    //}

        //    //[Benchmark]
        //    //public List<TimekeeperProps> XmlReaderImpl()
        //    //{
        //    //    return Parse().ToList();

        //    //    IEnumerable<TimekeeperProps> Parse()
        //    //    {
        //    //        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true });

        //    //        reader.ReadToFollowing("Data");

        //    //        while (reader.ReadToFollowing("Timekeeper"))
        //    //        {
        //    //            var timekeeper = new TimekeeperProps();

        //    //            reader.Read();
        //    //            timekeeper.Name = reader.ReadInnerXml();
        //    //            timekeeper.Age = int.Parse(reader.ReadInnerXml());
        //    //            timekeeper.DateOfBirth = ParseDateOfBirth(reader.ReadInnerXml());
        //    //            timekeeper.MatterNumber = ParseMatterNumber(reader.ReadInnerXml());

        //    //            yield return timekeeper;
        //    //        }
        //    //    }
        //}
    }

    [RankColumn(NumeralSystem.Arabic)]
    public class CreateInstanceBenchmarks
    {
        private static ConstructorInfo ctor;

        [Benchmark]
        public Timekeeper NewImpl()
        {
            return new Timekeeper();
        }

        [Benchmark]
        public Timekeeper ActivatorCreateInstance()
        {
            return Activator.CreateInstance<Timekeeper>();
        }

        [Benchmark]
        public Timekeeper CtorInvoke()
        {
            return (Timekeeper)typeof(Timekeeper).GetConstructors().Single().Invoke(new object[] { });
        }

        [Benchmark]
        public Timekeeper CtorInvokeCached()
        {
            if (ctor is null )
            {
                ctor = typeof(Timekeeper).GetConstructors().Single();
            }

            return (Timekeeper)ctor.Invoke(new object[] { });
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //var result = new TransactionSerivceExtensionsBenchmarks().XmlReaderImpl();
            //var summary = BenchmarkRunner.Run<TransactionSerivceExtensionsBenchmarks>();
            var summary = BenchmarkRunner.Run<CreateInstanceBenchmarks>();
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


        static List<TimekeeperProps> GenerateData(int size)
        {
            var random = new Random();
            return Enumerable.Range(0, size).Select(i => new TimekeeperProps
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
            var serializer = new XmlSerializer(typeof(Benchmarks.TestData.Data));
            using var textWriter = new StringWriter();
            serializer.Serialize(textWriter, new Benchmarks.TestData.Data { Timekeepers = data }, ns);
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

    public class TimekeeperProps
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

    public class TimekeeperCtor
    {
        public TimekeeperCtor(string name, int age, DateTime? dateOfBirth, MatterNumber matterNumber)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Age = age;
            DateOfBirth = dateOfBirth;
            MatterNumber = matterNumber;
        }

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
            this.s = s ?? throw new ArgumentNullException(nameof(s));
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
            return value is null ? null : new MatterNumber(value as string);
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
