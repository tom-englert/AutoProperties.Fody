using System.Reflection;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

public class InterceptorTests
{
    [NotNull]
    private readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Test]
    [TestCase("ClassWithSimpleInterceptors", 1)]
    [TestCase("ClassWithGenericInterceptors", 1)]
    [TestCase("ClassWithMixedInterceptors", 1)]
    [TestCase("ClassWithExternalInterceptorsBase", 0)]
    [TestCase("ClassWithExternalGenericInterceptorsBase", 0)]
    [TestCase("ClassWithInterceptorsUsingAllPossibleParameters", 3)]
    [TestCase("ClassWithReadonlyProperty", 1)]
    [TestCase("ClassDerivedFromClassWithAbstractProperty", 0)]
    public void SimpleInterceptorTest([NotNull] string className, int expectedNumberOfFields)
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
}

