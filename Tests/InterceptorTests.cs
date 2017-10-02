using System.Reflection;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

public class InterceptorTests
{
    [NotNull]
    private readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Test]
    [TestCase("ClassWithSimpleInterceptors", 1, "44!")]
    [TestCase("ClassWithGenericInterceptors", 1)]
    [TestCase("ClassWithMixedInterceptors", 1)]
    [TestCase("ClassWithExternalInterceptorsBase", 0)]
    [TestCase("ClassWithExternalGenericInterceptorsBase", 0)]
    [TestCase("ClassWithReadonlyProperty", 1, "44")]
    [TestCase("ClassDerivedFromClassWithAbstractProperty", 0)]
    public void SimpleInterceptorTest([NotNull] string className, int expectedNumberOfFields, [CanBeNull] string property3Value = null)
    {
        var target = assembly.GetInstance(className);

        Assert.AreEqual(42, target.Property1);
        Assert.AreEqual("42", target.Property2);

        target.Property1 = 43;

        Assert.AreEqual(43, target.Property1);
        Assert.AreEqual("43", target.Property2);

        target.Property2 = "44";

        Assert.AreEqual(44, target.Property1);
        Assert.AreEqual("44", target.Property2);

        if (property3Value != null)
        {
            Assert.AreEqual(property3Value, target.Property3);
        }

        // verify: backing fields should have been removed if interceptor does not contain a FieldInfo parameter
        Assert.AreEqual(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Test]
    [TestCase("ClassWithGenericInterceptorsAndFieldReference", 2)]
    [TestCase("ClassWithInterceptorsUsingAllPossibleParameters", 2)]
    [TestCase("ClassWithMixedGenericInterceptorsAndFieldReference", 2)]
    public void WithBackingFieldAccessTest([NotNull] string className, int expectedNumberOfFields, [CanBeNull] string property3Value = null)
    {
        var target = assembly.GetInstance(className);

        Assert.AreEqual(8, target.Property1);
        Assert.AreEqual("9", target.Property2);

        target.Property1 = 42;

        Assert.AreEqual(45, target.Property1);
        Assert.AreEqual("9", target.Property2);

        target.Property2 = "44";

        Assert.AreEqual(45, target.Property1);
        Assert.AreEqual("47", target.Property2);

        if (property3Value != null)
        {
            Assert.AreEqual(property3Value, target.Property3);
        }

        // verify: backing fields should have been removed if interceptor does not contain a FieldInfo parameter
        Assert.AreEqual(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Test]
    [TestCase("DerivedFromBaseWithPrivateInterceptors", 2)]
    [TestCase("ClassWithDoubleInterceptors", 3)]
    [TestCase("ClassWithMissingGetInterceptor", 3)]
    [TestCase("ClassWithBadGenericInterceptors", 3)]
    [TestCase("ClassWithUnsupportedParameter", 3)]
    [TestCase("ClassWithInterceptorAndInitializedAutoProperties", 3)]
    [TestCase("ClassWithInterceptorAndInitializedAutoPropertiesAndIgnoredPropties", 3)]
    public void BadImplementationInterceptorTest([NotNull] string className, int expectedNumberOfFields)
    {
        var target = assembly.GetInstance(className);

        Assert.AreEqual(7, target.Property1);
        Assert.AreEqual("8", target.Property2);

        target.Property1 = 43;

        Assert.AreEqual(43, target.Property1);
        Assert.AreEqual("8", target.Property2);

        target.Property2 = "44";

        Assert.AreEqual(43, target.Property1);
        Assert.AreEqual("44", target.Property2);

        // verify: backing fields are not removed
        Assert.AreEqual(expectedNumberOfFields, ((object)target).GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length);
    }

    [Test]
    public void ReadOnlyPropertiesTest()
    {
        var target = assembly.GetInstance("ClassWithReadonlyPropertiesAndOnlyGetInterceptor");

        Assert.AreEqual(42, target.Property1);
        Assert.AreEqual("42", target.Property2);
    }
}

