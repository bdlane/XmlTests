using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Tests.TestData;
using TransactionServiceExtensions;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // Arrange
            var data = GenerateData(1000);

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Name = t.Name,
                Age = t.Age,
                DateOfBirth = t.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                MatterNumber = t.MatterNumber?.ToString() ?? ""
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.Query<Timekeeper>("").ToList();

            // Assert
            Assert.Equal(data.Count(), actual.Count());
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

        static string Serialize(List<Tests.TestData.Timekeeper> data)
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

namespace Tests.TestData
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
