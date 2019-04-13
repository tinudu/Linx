namespace Linx.Jsxn.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

    /// <summary>
    /// Parsed JsxnSchema XML.
    /// </summary>
    public sealed class JsxnSchemaBuilder
    {
        /// <summary>
        /// Gets the schema target namespace.
        /// </summary>
        public static XNamespace TargetNamespace { get; } = "urn:Linx.Jsxn/JsxnSchemaV1.xsd";

        private static readonly XName _eImplements = TargetNamespace + "Implements";
        private static readonly XName _eInterface = TargetNamespace + "Interface";
        private static readonly XName _eEnum = TargetNamespace + "Enum";
        private static readonly XName _eObject = TargetNamespace + "Object";
        private static readonly XName _eProperty = TargetNamespace + "Property";
        private static readonly XName _eValue = TargetNamespace + "Value";
        private static readonly XName _aExtends = "extends";
        private static readonly XName _aFlags = "flags";
        private static readonly XName _aModifier = "modifier";
        private static readonly XName _aName = "name";
        private static readonly XName _aIsCalculated = "isCalculated";
        private static readonly XName _aType = "type";

        private static readonly Lazy<XmlSchemaSet> _xsds = new Lazy<XmlSchemaSet>(() =>
        {
            var xsds = new XmlSchemaSet();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(JsxnSchemaBuilder), "JsxnSchemaV1.xsd"))
            {
                var xsd = XmlSchema.Read(stream, null);
                xsds.Add(xsd);
            }
            xsds.Compile();
            return xsds;
        });

        /// <summary>
        /// Create from XML.
        /// </summary>
        public static JsxnSchemaBuilder Load(Stream input)
        {
            lock (_xsds)
            {
                var vReader = XmlReader.Create(input, new XmlReaderSettings { Schemas = _xsds.Value, ValidationType = ValidationType.Schema, ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings });
                var doc = XDocument.Load(vReader);
                return new JsxnSchemaBuilder(doc);
            }
        }

        /// <summary>
        /// Create from XML.
        /// </summary>
        public static JsxnSchemaBuilder Load(TextReader input)
        {
            lock (_xsds)
            {
                var vReader = XmlReader.Create(input, new XmlReaderSettings { Schemas = _xsds.Value, ValidationType = ValidationType.Schema, ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings });
                var doc = XDocument.Load(vReader);
                return new JsxnSchemaBuilder(doc);
            }
        }

        /// <summary>
        /// Create from XML.
        /// </summary>
        public static JsxnSchemaBuilder Load(XmlReader input)
        {
            lock (_xsds)
            {
                var vReader = XmlReader.Create(input, new XmlReaderSettings { Schemas = _xsds.Value, ValidationType = ValidationType.Schema, ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings });
                var doc = XDocument.Load(vReader);
                return new JsxnSchemaBuilder(doc);
            }
        }

        internal IReadOnlyCollection<ISchemaType> Types { get; }

        private JsxnSchemaBuilder(XDocument doc)
        {
            var types = new List<ISchemaType>();
            var typeNames = new HashSet<string>();
            void Add(ISchemaType type)
            {
                if (!typeNames.Add(type.Name)) throw new Exception($"Duplicate type name '{type.Name}'.");
                types.Add(type);
            }

            foreach (var eEnum in doc.Root.Elements(_eEnum))
                Add(new EnumType((Identifier)eEnum.Attribute(_aName).Value, XmlConvert.ToBoolean(eEnum.Attribute(_aFlags).Value), eEnum.Elements(_eValue).Select(e => (Identifier)e.Value).ToList().AsReadOnly()));

            foreach (var eInterface in doc.Root.Elements(_eInterface))
            {
                var type = new InterfaceType
                {
                    Name = (Identifier)eInterface.Attribute(_aName).Value,
                    Implements = eInterface.Elements(_eImplements).Select(e => (Identifier)e.Value).ToList(),
                    Properties = eInterface.Elements(_eProperty).Select(e => new Property
                    {
                        Name = (Identifier)e.Attribute(_aName).Value,
                        TypeRef = e.Attribute(_aType).Value
                    }).ToList()
                };
                Add(type);
            }

            foreach (var eObject in doc.Root.Elements(_eObject))
            {
                var modifier = (ObjectModifier)Enum.Parse(typeof(ObjectModifier), eObject.Attribute(_aModifier).Value);
                var type = new ObjectType
                {
                    Name = (Identifier)eObject.Attribute(_aName).Value,
                    Modifier = modifier,
                    Extends = (Identifier)eObject.Attribute(_aExtends).Value,
                    Implements = eObject.Elements(_eImplements).Select(e => (Identifier)e.Value).ToList(),
                    Properties = eObject.Elements(_eProperty).Select(e => new ObjectProperty
                    {
                        Name = (Identifier)e.Attribute(_aName).Value,
                        TypeRef = e.Attribute(_aType).Value,
                        IsCalculated = modifier != ObjectModifier.Abstract && XmlConvert.ToBoolean(e.Attribute(_aIsCalculated).Value)
                    }).ToList()
                };
                Add(type);
            }

            Types = types.AsReadOnly();
        }

        internal interface ISchemaType { Identifier Name { get; } }

        internal class Property
        {
            public Identifier Name { get; set; }
            public string TypeRef { get; set; }
        }

        internal sealed class InterfaceType : ISchemaType
        {
            public Identifier Name { get; set; }
            public IReadOnlyCollection<Identifier> Implements { get; set; }
            public IReadOnlyCollection<Property> Properties { get; set; }
        }

        internal sealed class ObjectProperty : Property
        {
            public bool IsCalculated { get; set; }
        }

        internal sealed class ObjectType : ISchemaType
        {
            public Identifier Name { get; set; }
            public ObjectModifier Modifier { get; set; }
            public Identifier Extends { get; set; }
            public IReadOnlyCollection<Identifier> Implements { get; set; }
            public IReadOnlyCollection<ObjectProperty> Properties { get; set; }
        }
    }
}
