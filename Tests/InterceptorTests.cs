using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

using JetBrains.Annotations;

using Tests;

using Xunit;
using Xunit.Abstractions;

public class InterceptorTests
{
    [NotNull]
    private readonly ITestOutputHelper _testOutputHelper;
    [NotNull]
    private readonly Assembly _assembly = WeaverHelper.Create().Assembly;

    public InterceptorTests([NotNull] ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("ClassWithSimpleInterceptors", 1, "44!")]
    [InlineData("ClassWithGenericInterceptors", 1)]
    [InlineData("ClassWithMixedInterceptors", 1)]
    [InlineData("ClassWithExternalInterceptorsBase", 0)]
    [InlineData("ClassWithExternalGenericInterceptorsBase", 0)]
    [InlineData("ClassWithReadonlyProperty", 1, "44")]
    [InlineData("ClassDerivedFromClassWithAbstractProperty", 0)]
    public void SimpleInterceptorTest([NotNull] string className, int expectedNumberOfFields, [CanBeNull] string property3Value = null)
    {
        var target = _assembly.GetInstance(className);

        Assert.Equal(42, target.Property1);
        Assert.Equal("42", target.Property2);

        target.Property1 = 43;

        Assert.Equal(43, target.Property1);
        Assert.Equal("43", target.Property2);

        target.Property2 = "44";

        Assert.Equal(44, target.Property1);
        Assert.Equal("44", target.Property2);

        if (property3Value != null)
        {
            Assert.Equal(property3Value, target.Property3);
        }

        // verify: backing fields should have been removed if interceptor does not contain a FieldInfo parameter
        Assert.Equal(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Theory]
    [InlineData("ClassWithGenericInterceptorsAndFieldReference", 2)]
    [InlineData("ClassWithInterceptorsUsingAllPossibleParameters", 2)]
    [InlineData("ClassWithMixedGenericInterceptorsAndFieldReference", 2)]
    public void WithBackingFieldAccessTest([NotNull] string className, int expectedNumberOfFields, [CanBeNull] string property3Value = null)
    {
        var target = _assembly.GetInstance(className);

        Assert.Equal(10, target.Property1);
        Assert.Equal("11", target.Property2);

        target.Property1 = 42;

        Assert.Equal(45, target.Property1);
        Assert.Equal("11", target.Property2);

        target.Property2 = "44";

        Assert.Equal(45, target.Property1);
        Assert.Equal("47", target.Property2);

        if (property3Value != null)
        {
            Assert.Equal(property3Value, target.Property3);
        }

        // verify: backing fields should have been removed if interceptor does not contain a FieldInfo parameter
        Assert.Equal(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Theory]
    [InlineData("DerivedFromBaseWithPrivateInterceptors", 2)]
    [InlineData("ClassWithDoubleInterceptors", 3)]
    [InlineData("ClassWithMissingGetInterceptor", 3)]
    [InlineData("ClassWithBadGenericInterceptors", 3)]
    [InlineData("ClassWithUnsupportedParameter", 3)]
    [InlineData("ClassWithInterceptorAndInitializedAutoPropertiesAndIgnoredPropties", 3)]
    [InlineData("ClassWithBadReturnTypeInGetter", 3)]
    [InlineData("ClassWithBadReturnTypeInGenericGetter", 3)]
    [InlineData("ClassWithBadReturnTypeInSetter", 3)]
    public void BadImplementationInterceptorTest([NotNull] string className, int expectedNumberOfFields)
    {
        var target = _assembly.GetInstance(className);

        Assert.Equal(7, target.Property1);
        Assert.Equal("8", target.Property2);

        target.Property1 = 43;

        Assert.Equal(43, target.Property1);
        Assert.Equal("8", target.Property2);

        target.Property2 = "44";

        Assert.Equal(43, target.Property1);
        Assert.Equal("44", target.Property2);

        // verify: backing fields are not removed
        Assert.Equal(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Fact]
    public void ReadOnlyPropertiesTest()
    {
        var target = _assembly.GetInstance("ClassWithReadonlyPropertiesAndOnlyGetInterceptor");

        Assert.Equal(42, target.Property1);
        Assert.Equal("42", target.Property2);
    }

    [Theory]
    [InlineData("SubClassWithPropertyOverride")]
    [InlineData("SubClassWithVirtualPropertyOverride")]
    public void SubClassWithPropertyOverrideTest([NotNull] string className)
    {
        var target = _assembly.GetInstance(className);
    }

    [Fact]
    public void RemoteTests()
    {
        var catalog = new AggregateCatalog();
        var container = new CompositionContainer(catalog);

        catalog.Catalogs.Add(new AssemblyCatalog(_assembly));

        var actions = container.GetExportedValues<Action>();

        foreach (var action in actions)
        {
            var method = action.Method;

            _testOutputHelper.WriteLine($"Run {method.DeclaringType.Name}.{method.Name}");
            action();
        }

    }

    [Theory]
    [InlineData("ClassWithAutoPropertyInitAndGenericBase")]
    public void ClassWithAutoPropertyInitAndGenericBaseTest([NotNull] string className)
    {
        var target = _assembly.GetInstance(className);
        target.Prop1 = 42;
        Assert.Equal(42, target.Prop1); 
        Assert.Equal("Test", target.Prop2); 
        Assert.Equal("Test2", target.Prop3);
    }

    [Theory]
    [InlineData("ParentWithGenericBaseOfInt")]
    public void GenericBaseClassOfInt_Test([NotNull] string className)
    {
        var parent = _assembly.GetInstance(className);
        parent.GenericProperty = 1;
        Assert.Contains("GenericProperty", parent.ChangedProperties);
    }

    [Theory]
    [InlineData("ParentWithGenericBaseOfString")]
    public void GenericBaseClassOfString_Test([NotNull] string className)
    {
        var parent = _assembly.GetInstance(className);
        parent.GenericProperty = "Hello";
        Assert.Contains("GenericProperty", parent.ChangedProperties);
    }

    [Theory]
    [InlineData("SomeClassWithoutGenerics")]
    public void SomeClassWithoutGenerics_Test([NotNull] string className)
    {
        var parent = _assembly.GetInstance(className);
        parent.ValueProperty = 43;
        Assert.Contains("ValueProperty", parent.ChangedProperties);
        Assert.Equal(parent.ValueProperty, 43);

        parent.ReferenceProperty = "Hello";
        Assert.Contains("ReferenceProperty", parent.ChangedProperties);
        Assert.Equal(parent.ReferenceProperty, "Hello");

        parent.ArrayProperty = new[] {"Hello", "World"};
        Assert.Contains("ArrayProperty", parent.ChangedProperties);
        Assert.Equal(parent.ArrayProperty,new[] {"Hello", "World"});
    }
}