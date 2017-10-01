using System.Reflection;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

public class InterceptorTests
{
    [NotNull]
    private readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Test]
    public void SimpleInterceptorTest()
    {
        var target = assembly.GetInstance("ClassWithSimpleInterceptor");

        Assert.AreEqual(42, target.Property1);
    }
}

