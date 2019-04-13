namespace Tests.Linx.Xml
{
    using System;
    using System.Xml;
    using System.Xml.Linq;
    using global::Linx.Xml;
    using Xunit;

    public sealed class LinxXmlTests
    {
        [Fact]
        public void Single_Present_Success()
        {
            var child = new XElement("child");
            Assert.Equal(child, new XElement("parent", child).Single(child.Name));
        }

        [Fact]
        public void Single_Missing_Fail()
        {
            try
            {
                new XElement("parent").Single("child");
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Missing element 'child' on element 'parent'.", ex.Message);
            }
        }

        [Fact]
        public void Single_Multiple_Fail()
        {
            try
            {
                new XElement("parent", new XElement("child"), new XElement("child")).Single("child");
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Multiple elements 'child' on element 'parent'.", ex.Message);
            }
        }

        [Fact]
        public void SingleOrDefault_Present_Success()
        {
            var child = new XElement("child");
            Assert.Equal(child, new XElement("parent", child).SingleOrDefault(child.Name));
        }

        [Fact]
        public void SingleOrDefault_Missing_Success() => Assert.Null(new XElement("parent").SingleOrDefault("child"));

        [Fact]
        public void SingleOrDefault_Multiple_Fail()
        {
            try
            {
                new XElement("parent", new XElement("child"), new XElement("child")).SingleOrDefault("child");
                throw new Exception("Should fail.");
            }
            catch (Exception ex) { Assert.Equal("Multiple elements 'child' on element 'parent'.", ex.Message); }
        }

        [Fact]
        public void FromAttribute_Present_Success() => Assert.Equal("42", new XElement("e", new XAttribute("a", "42")).FromAttribute("a"));

        [Fact]
        public void FromAttribute_Missing_Fail()
        {
            try
            {
                new XElement("e").FromAttribute("a");
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Missing attribute 'a' on element 'e'.", ex.Message);
            }
        }

        [Fact]
        public void FromAttribute1_Present_Success() => Assert.Equal(42, new XElement("e", new XAttribute("a", "42")).FromAttribute("a", XmlConvert.ToInt32));

        [Fact]
        public void FromAttribute1_Missing_Fail()
        {
            try
            {
                new XElement("e").FromAttribute("a", XmlConvert.ToInt32);
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Missing attribute 'a' on element 'e'.", ex.Message);
            }
        }

        [Fact]
        public void FromAttributeOrDefault_Present_Success() => Assert.Equal("42", new XElement("e", new XAttribute("a", "42")).FromAttributeOrDefault("a"));

        [Fact]
        public void FromAttributeOrDefault_Missing_Fail() => Assert.Null(new XElement("e").FromAttributeOrDefault("a"));

        [Fact]
        public void FromAttributeOrDefault1_Present_Success() => Assert.Equal("The answer is 42", new XElement("e", new XAttribute("a", "42")).FromAttributeOrDefault("a", v => "The answer is " + XmlConvert.ToInt32(v)));

        [Fact]
        public void FromAttributeOrDefault1_Missing_Fail() => Assert.Null(new XElement("e").FromAttributeOrDefault("a", v => "The answer is " + XmlConvert.ToInt32(v)));

        [Fact]
        public void FromAttributeOrNull_Present_Success() => Assert.Equal(42, new XElement("e", new XAttribute("a", 42)).FromAttributeOrNull("a", XmlConvert.ToInt32));

        [Fact]
        public void FromAttributeOrNull_Missing_Success() => Assert.Null(new XElement("e").FromAttributeOrNull("a", XmlConvert.ToInt32));

        [Fact]
        public void FromElement_Present_Success() => Assert.Equal(42, new XElement("e", new XElement("a", "42")).FromElement("a", a => XmlConvert.ToInt32(a.Value)));

        [Fact]
        public void FromElement_Missing_Fail()
        {
            try
            {
                new XElement("e").FromElement("a", a => XmlConvert.ToInt32(a.Value));
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Missing element 'a' on element 'e'.", ex.Message);
            }
        }

        [Fact]
        public void FromElement_Multiple_Fail()
        {
            try
            {
                new XElement("e", new XElement("a", "42"), new XElement("a", "43")).FromElement("a", a => XmlConvert.ToInt32(a.Value));
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Multiple elements 'a' on element 'e'.", ex.Message);
            }
        }

        [Fact]
        public void FromElementOrDefault_Present_Success() => Assert.Equal("The answer is 42", new XElement("e", new XElement("a", "42")).FromElementOrDefault("a", a => "The answer is " + XmlConvert.ToInt32(a.Value)));

        [Fact]
        public void FromElementOrDefault_Missing_Success() => Assert.Null(new XElement("parent").FromElementOrDefault("a", a => "The answer is " + XmlConvert.ToInt32(a.Value)));

        [Fact]
        public void FromElementOrDefault_Multiple_Fail()
        {
            try
            {
                new XElement("e", new XElement("a", "42"), new XElement("a", "43")).FromElementOrDefault("a", a => "The answer is " + XmlConvert.ToInt32(a.Value));
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Multiple elements 'a' on element 'e'.", ex.Message);
            }
        }

        [Fact]
        public void FromElementOrNull_Present_Success() => Assert.Equal(42, new XElement("e", new XElement("a", 42)).FromElementOrNull("a", a => XmlConvert.ToInt32(a.Value)));

        [Fact]
        public void FromElementOrNull_Missing_Success() => Assert.Null(new XElement("e").FromElementOrNull("a", a => XmlConvert.ToInt32(a.Value)));

        [Fact]
        public void FromElementOrNull_Multiple_Fail()
        {
            try
            {
                new XElement("e", new XElement("a", "42"), new XElement("a", "43")).FromElementOrNull("a", e => XmlConvert.ToInt32(e.Value));
                throw new Exception("Should fail.");
            }
            catch (Exception ex)
            {
                Assert.Equal("Multiple elements 'a' on element 'e'.", ex.Message);
            }
        }
    }

    public sealed class TheToDateTimeOffsetMethod
    {
        private static readonly DateTime _dt = new DateTime(2018, 2, 25, 13, 15, 12, 345);
        private static readonly string _dtStr = XmlConvert.ToString(_dt, XmlDateTimeSerializationMode.Unspecified);

        [Fact]
        public void Success()
        {
            Assert.Equal(new DateTimeOffset(_dt, TimeSpan.FromHours(1)), LinxXmlExtensions.ToDateTimeOffset(_dtStr + "+01:00"));
            Assert.Equal(new DateTimeOffset(_dt, TimeSpan.Zero), LinxXmlExtensions.ToDateTimeOffset(_dtStr + "+00:00"));
            Assert.Equal(new DateTimeOffset(_dt, TimeSpan.Zero), LinxXmlExtensions.ToDateTimeOffset(_dtStr + "Z"));
        }

        [Fact]
        public void FailNoTimezone()
        {
            try
            {
                LinxXmlExtensions.ToDateTimeOffset(_dtStr);
                throw new Exception("Should fail.");
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("Timezone must be specified explicitely.", ex.Message);
            }
        }
    }
}