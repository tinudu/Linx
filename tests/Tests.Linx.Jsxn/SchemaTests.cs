//namespace Tests.Linx.Jsxn
//{
//    using System;
//    using System.Linq;
//    using System.Reflection;
//    using global::Linx.Jsxn.Schema;
//    using Xunit;

//    public class SchemaTests
//    {
//        private static readonly Lazy<JsxnSchema> _sampleSchema = new Lazy<JsxnSchema>(() =>
//        {
//            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(SchemaTests), "SampleJsxnSchema.xml"))
//            {
//                var builder = JsxnSchemaBuilder.Load(stream);
//                return new JsxnSchema(new[] { builder });
//            }
//        });

//        public static JsxnSchema SampleSchema => _sampleSchema.Value;

//        [Fact]
//        public void TestSampleSchema()
//        {
//            var color = (EnumType)SampleSchema.Types["Color"];
//            Assert.True(color.Flags);
//            Assert.True(color.Values.Select(v => v.Name).SequenceEqual(new[] { "Red", "Green", "Blue" }));

//            var animal = (InterfaceType)SampleSchema.Types["IAnimal"];
//            var carnivore = (InterfaceType)SampleSchema.Types["ICarnivore"];
//            var pet = (InterfaceType)SampleSchema.Types["IPet"];
//            var cat = (ObjectType)SampleSchema.Types["Cat"];
//            var giraffe = (ObjectType)SampleSchema.Types["Giraffe"];
//            var mouse = (ObjectType)SampleSchema.Types["Mouse"];

//            Assert.Equal(0, animal.Implements.Count);
//            Assert.Equal(1, animal.Properties.Count);
//            Assert.True(animal.Properties["NumberOfLegs"].Type == PrimitiveType.Int32);

//            Assert.True(carnivore.Properties["NumberOfLegs"].Type == PrimitiveType.Int32);

//            var adult = (ObjectType)SampleSchema.Types["Adult"];
//            var child = (ObjectType)SampleSchema.Types["Child"];
//            var person = (ObjectType)SampleSchema.Types["Person"];

//        }

//        //[Fact]
//        //public void TestArrayType()
//        //{
//        //    var type = StringType.Instance.Array;
//        //    Assert.Equal(type.ElementType, StringType.Instance);
//        //    Assert.Equal("string[]", type.ToString());
//        //}

//        //[Fact]
//        //public void TestEnumType()
//        //{
//        //    var name = (Identifier)"Color";
//        //    var values = new[] { (Identifier)"Red", (Identifier)"Green", (Identifier)"Blue" };
//        //    var type = new EnumType(name, values);
//        //    Assert.Equal(type.Name, name);
//        //    Assert.True(type.Values.SequenceEqual(values));
//        //    Assert.Equal(type.ToString(), name);
//        //}

//        //[Fact]
//        //public void TestFunctionType()
//        //{
//        //    var argTypes = new JsxnType[] { StringType.Instance.Nullable, IntType.Instance };
//        //    var returnType = StringType.Instance.Nullable.Array;
//        //    var type = FunctionType.GetType(argTypes, returnType);

//        //    Assert.True(type.ArgumentTypes.SequenceEqual(argTypes));
//        //    Assert.Equal(type.ReturnType, returnType);
//        //    Assert.Equal("(string?, int) => string?[]", type.ToString());

//        //    Assert.Equal(type, FunctionType.GetType(argTypes, returnType));
//        //}

//        //[Fact]
//        //public void TestNullableType()
//        //{
//        //    var type = StringType.Instance.Nullable;
//        //    Assert.Equal(type.UnderlyingType, StringType.Instance);
//        //    Assert.Equal("string?", type.ToString());
//        //}

//        ////[Fact]
//        ////public void TestObjectType()
//        ////{
//        ////    var animalProps = new[] { new PropertyDeclaration((Identifier)"NumberOfLegs", IntType.TypeRef) };
//        ////    var odAnimal = new ObjectDeclaration(ObjectModifier.Abstract, (Identifier)"Animal", null, animalProps);
//        ////    Assert.Equal(ObjectModifier.Abstract, odAnimal.Modifier);
//        ////    Assert.Equal("Animal", odAnimal.Name);
//        ////    Assert.Equal("object", odAnimal.BaseTypeName);
//        ////    Assert.True(animalProps.SequenceEqual(odAnimal.Properties));

//        ////    var eGender = new EnumType((Identifier)"Gender", new[] { (Identifier)"Female", (Identifier)"Male" });

//        ////    var mammalProps = new[] { new PropertyDeclaration((Identifier)"Gender", new NamedTypeRef(eGender.Name)) };
//        ////    var odMammal = new ObjectDeclaration(ObjectModifier.Virtual, (Identifier)"Mammal", odAnimal.Name, mammalProps);
//        ////    Assert.Equal(ObjectModifier.Virtual, odMammal.Modifier);
//        ////    Assert.Equal("Mammal", odMammal.Name);
//        ////    Assert.Equal("Animal", odMammal.BaseTypeName);
//        ////    Assert.True(mammalProps.SequenceEqual(odMammal.Properties));

//        ////    var giraffeProps = new[]
//        ////    {
//        ////        new PropertyDeclaration((Identifier)"Name", StringType.TypeRef),
//        ////        new PropertyDeclaration((Identifier)"FavoritePet", new NamedTypeRef(odAnimal.Name).Nullable)
//        ////    };
//        ////    var odGiraffe = new ObjectDeclaration(ObjectModifier.Sealed, (Identifier)"Giraffe", odMammal.Name, giraffeProps);
//        ////    Assert.Equal(ObjectModifier.Sealed, odGiraffe.Modifier);
//        ////    Assert.Equal("Giraffe", odGiraffe.Name);
//        ////    Assert.Equal("Mammal", odGiraffe.BaseTypeName);
//        ////    Assert.True(giraffeProps.SequenceEqual(odGiraffe.Properties));

//        ////    var tb = new TypeBag(new JsxnTypeDeclaration[] { odGiraffe, odMammal, odAnimal });

//        ////    var tAnimal = (ObjectType)tb.Dereference(new NamedTypeRef(odAnimal.Name));
//        ////    Assert.Equal(ObjectModifier.Abstract, tAnimal.Modifier);
//        ////    Assert.Equal(tAnimal.Name, odAnimal.Name);
//        ////    Assert.Equal(tAnimal.BaseType, ObjectType.Object);
//        ////    var expectedProps = animalProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tAnimal, ReflectedType = (NonNullableType)tAnimal });
//        ////    Assert.True(tAnimal.Properties.Select(p => new { p.Name, p.Type, p.DeclaringType, p.ReflectedType }).SequenceEqual(expectedProps));

//        ////    var tMammal = (ObjectType)tb.Dereference(new NamedTypeRef(odMammal.Name));
//        ////    Assert.Equal(ObjectModifier.Virtual, tMammal.Modifier);
//        ////    Assert.Equal(tMammal.Name, odMammal.Name);
//        ////    Assert.Equal(tMammal.BaseType, tAnimal);
//        ////    expectedProps = animalProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tAnimal, ReflectedType = (NonNullableType)tMammal })
//        ////        .Concat(mammalProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tMammal, ReflectedType = (NonNullableType)tMammal }));
//        ////    Assert.True(tMammal.Properties.Select(p => new { p.Name, p.Type, p.DeclaringType, p.ReflectedType }).SequenceEqual(expectedProps));

//        ////    var tGiraffe = (ObjectType)tb.Dereference(new NamedTypeRef(odGiraffe.Name));
//        ////    Assert.Equal(ObjectModifier.Sealed, tGiraffe.Modifier);
//        ////    Assert.Equal(tGiraffe.Name, odGiraffe.Name);
//        ////    Assert.Equal(tGiraffe.BaseType, tMammal);
//        ////    expectedProps = animalProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tAnimal, ReflectedType = (NonNullableType)tGiraffe })
//        ////        .Concat(mammalProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tMammal, ReflectedType = (NonNullableType)tGiraffe }))
//        ////        .Concat(giraffeProps.Select(p => new { p.Name, Type = tb.Dereference(p.Type), DeclaringType = (NonNullableType)tGiraffe, ReflectedType = (NonNullableType)tGiraffe }));
//        ////    Assert.True(tGiraffe.Properties.Select(p => new { p.Name, p.Type, p.DeclaringType, p.ReflectedType }).SequenceEqual(expectedProps));
//        ////}

//        ////[Fact]
//        ////public void TestStructuralType()
//        ////{
//        ////    var props = new[] { new PropertyDeclaration((Identifier)"X", IntType.TypeRef), new PropertyDeclaration((Identifier)"Y", IntType.TypeRef) };

//        ////    {
//        ////        var typeRef = new StructuralTypeRef(true, props);
//        ////        Assert.True(typeRef.IsTuple);
//        ////        Assert.True(typeRef.Properties.SequenceEqual(props));
//        ////        Assert.Equal("(X: int, Y: int)", typeRef.ToString());

//        ////        var type = TypeBag.Predefined.Dereference(typeRef);
//        ////        Assert.True(type.IsTuple);
//        ////        Assert.True(type.Properties.Select(p => new { p.Name, p.Type, p.DeclaringType, p.ReflectedType }).SequenceEqual(props.Select(p => new { p.Name, Type = (JsxnType)IntType.Instance, DeclaringType = (NonNullableType)type, ReflectedType = (NonNullableType)type })));
//        ////        Assert.Equal("(X: int, Y: int)", type.ToString());

//        ////        Assert.Equal(type, TypeBag.Predefined.Dereference(typeRef));
//        ////    }

//        ////    {
//        ////        var typeRef = new StructuralTypeRef(false, props);
//        ////        Assert.False(typeRef.IsTuple);
//        ////        Assert.True(typeRef.Properties.SequenceEqual(props));
//        ////        Assert.Equal("{ X: int, Y: int }", typeRef.ToString());

//        ////        var type = TypeBag.Predefined.Dereference(typeRef);
//        ////        Assert.False(type.IsTuple);
//        ////        Assert.True(type.Properties.Select(p => new { p.Name, p.Type, p.DeclaringType, p.ReflectedType }).SequenceEqual(props.Select(p => new { p.Name, Type = (JsxnType)IntType.Instance, DeclaringType = (NonNullableType)type, ReflectedType = (NonNullableType)type })));
//        ////        Assert.Equal("{ X: int, Y: int }", type.ToString());

//        ////        Assert.Equal(type, TypeBag.Predefined.Dereference(typeRef));
//        ////    }
//        ////}

//        //[Fact]
//        //public void TestStructuralType()
//        //{
//        //    var props = new[] { new KeyValuePair<Identifier, JsxnType>((Identifier)"A", IntType.Instance), new KeyValuePair<Identifier, JsxnType>((Identifier)"B", StringType.Instance.Nullable) };
//        //    var type = StructuralType.GetType(props);

//        //    // IEnumerable<>
//        //    Assert.True(type.SequenceEqual(props));

//        //    // Count
//        //    Assert.Equal(2, type.Count);

//        //    // Keys, Values
//        //    Assert.True(props.Select(p => p.Key).SequenceEqual(type.Keys));
//        //    Assert.True(props.Select(p => p.Value).SequenceEqual(type.Values));

//        //    // Indexer
//        //    Assert.True(props.All(p => p.Value == type[p.Key]));
//        //    Assert.Throws<KeyNotFoundException>(() => type[(Identifier)"Bla"]);

//        //    // ContainsKey
//        //    Assert.True(props.All(p => type.ContainsKey(p.Key)));
//        //    Assert.False(type.ContainsKey((Identifier)"Bla"));

//        //    // TryGetValue
//        //    Assert.True(props.All(p => type.TryGetValue(p.Key, out var value) && value == p.Value));
//        //    Assert.False(type.TryGetValue((Identifier)"Bla", out var bla) && bla == null);

//        //    // ToString()
//        //    Assert.Equal("{ A: int, B: string? }", type.ToString());

//        //    // Singleton
//        //    Assert.Equal(type, StructuralType.GetType(props));
//        //}

//        //[Fact]
//        //public void TestTupleType()
//        //{
//        //    {
//        //        var props = new[] { new KeyValuePair<Identifier?, JsxnType>(null, IntType.Instance), new KeyValuePair<Identifier?, JsxnType>(null, IntType.Instance) };
//        //        var type = TupleType.GetType(props);
//        //        Assert.True(type.Properties.SequenceEqual(props));
//        //        Assert.Throws<KeyNotFoundException>(() => type.GetPropertyType((Identifier)"Bla"));
//        //        Assert.Null(type.TryGetPropertyType((Identifier)"Bla"));
//        //        Assert.Equal("(int, int)", type.ToString());
//        //        Assert.Equal(TupleType.GetType(props), type);
//        //    }

//        //    {
//        //        var props = new[] { new KeyValuePair<Identifier?, JsxnType>((Identifier)"X", IntType.Instance), new KeyValuePair<Identifier?, JsxnType>((Identifier)"Y", IntType.Instance) };
//        //        var type = TupleType.GetType(props);
//        //        Assert.True(type.Properties.SequenceEqual(props));
//        //        Assert.Equal(type.GetPropertyType((Identifier)"X"), IntType.Instance);
//        //        Assert.Equal(type.TryGetPropertyType((Identifier)"X"), IntType.Instance);
//        //        Assert.Equal(type.GetPropertyType((Identifier)"Y"), IntType.Instance);
//        //        Assert.Equal(type.TryGetPropertyType((Identifier)"Y"), IntType.Instance);
//        //        Assert.Throws<KeyNotFoundException>(() => type.GetPropertyType((Identifier)"Bla"));
//        //        Assert.Null(type.TryGetPropertyType((Identifier)"Bla"));
//        //        Assert.Equal("(X:int, Y:int)", type.ToString());
//        //        Assert.Equal(TupleType.GetType(props), type);
//        //    }
//        //}
//    }
//}
