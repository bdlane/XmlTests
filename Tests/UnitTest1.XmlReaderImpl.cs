using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AutoFixture.Xunit2;
using Data;
using FluentAssertions;
using TransactionServiceExtensions.XmlReaderImpl;
using TransactionServiceExtensions;
using Xunit;

namespace Tests.XmlReaderImpl
{
    public class UnitTest1
    {
        // Handling empty string - is it null? or empty string? no way to tell.
        // Null reference types - type converter, return null?
        [Fact]
        public void Properties()
        {
            // Arrange
            var data = GenerateData(3);

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
            var actual = service.QueryReader<TimekeeperProp>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void Constructor()
        {
            // Arrange
            var data = GenerateData(3);

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
            var actual = service.QueryReader<TimekeeperCtor>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void Constructor2()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => new TimekeeperSimpleCtor(d.Name, d.Age));

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Name = t.Name,
                Age = t.Age,
                //DateOfBirth = t.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                //MatterNumber = t.MatterNumber?.ToString() ?? ""
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<TimekeeperSimpleCtor>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Constructor3()
        {
            // Arrange
            var data = GenerateData(3);
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
            var actual = service.QueryReader2<TimekeeperCtor>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void Properties2()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => new TimekeeperSimpleProp { Name = d.Name, Age = d.Age });

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Name = t.Name,
                Age = t.Age
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<TimekeeperSimpleProp>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Properties3()
        {
            // Arrange
            var data = GenerateData(3);

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
            var actual = service.QueryReader2<TimekeeperProp>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void QueryString()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => d.Name);

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Name = t.Name
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<string>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void QueryInt()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => d.Age);

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Age = t.Age
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<int>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void QueryNullableInt()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => (int?)d.Age);

            var serializable = data.Select(t => new TestData.Timekeeper
            {
                Age = t.Age
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<int?>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void QueryNullableDateTime()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d => d.DateOfBirth);

            var serializable = data.Select(t => new TestData.SingleValueRow
            {
                Value = t.DateOfBirth?.ToString("yyyy-MM-dd") ?? ""
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<DateTime?>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory, AutoData]
        public void QueryEnum(IEnumerable<Size> sizes)
        {
            // Arrange
            var expected = sizes;

            var serializable = sizes.Select(v => new TestData.SingleValueRow
            {
                Value = ((int)v).ToString()
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReader2<Size>("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void QueryDict()
        {
            // Arrange
            var data = GenerateData(3);
            var expected = data.Select(d =>
            {
                return new Dictionary<string, string>
                {
                    { nameof(d.Name), d.Name },
                    { nameof(d.Age), d.Age.ToString() },
                    { nameof(d.DateOfBirth), d.DateOfBirth?.ToString("yyyy-MM-dd") }
                };
            });

            var serializable = data.Select(v => new TestData.Timekeeper
            {
                Name = v.Name,
                Age = v.Age,
                DateOfBirth = v.DateOfBirth?.ToString("yyyy-MM-dd") ?? ""
            }).ToList();

            var xml = Serialize(serializable);

            var service = new MockTransactionService(xml);

            // Act
            var actual = service.QueryReaderT("").ToList();

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        // Nullable int
        // DateTime
        // Nullable DateTime

        static List<TimekeeperProp> GenerateData(int size)
        {
            var random = new Random();
            return Enumerable.Range(0, size).Select(i => new TimekeeperProp
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

        static string Serialize(List<TestData.Timekeeper> data)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var serializer = new XmlSerializer(typeof(TestData.Data));
            using var textWriter = new StringWriter();
            serializer.Serialize(textWriter, new TestData.Data { Timekeepers = data }, ns);
            return textWriter.ToString();
        }


        static string Serialize(List<TestData.SingleValueRow> data)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var serializer = new XmlSerializer(typeof(TestData.SingleValueData));
            using var textWriter = new StringWriter();
            serializer.Serialize(textWriter, new TestData.SingleValueData { Rows = data }, ns);
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

    public class TimekeeperProp
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
            return value is string s ? new MatterNumber(s) : null;
        }
    }

    public enum Size
    {
        Small = 0,
        Medium,
        Large
    }
}

namespace Tests.XmlReaderImpl.TestData
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

    [XmlRoot(ElementName = "Data")]
    public class SingleValueData
    {
        [XmlElement(ElementName = "Timekeeper")]
        public List<SingleValueRow> Rows { get; set; }
    }

    [XmlType(TypeName = "Row")]
    public class SingleValueRow
    {
        public string Value { get; set; }
    }
}
