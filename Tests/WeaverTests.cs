using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

[TestFixture]
public class WeaverTests
{
    [NotNull]
    private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();

    [Test]
    public void OutputWeaverErrors()
    {
        foreach (var message in _weaverHelper.Errors)
        {
            TestContext.Out.WriteLine(message);
        }

        Assert.AreEqual(9, _weaverHelper.Errors.Count);
    }

    [Test]
    public void OutputWeaverMessages()
    {
        foreach (var message in _weaverHelper.Messages)
        {
            TestContext.Out.WriteLine(message);
        }
    }

    [Test]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public void ValidatePropertyChangedIsInjected()
    {
        var assembly = _weaverHelper.Assembly;
        var instance = assembly.GetInstance("ImplementsPropertyChanged");
        var calls = new List<string>();

        var inpc = (INotifyPropertyChanged)instance;
        inpc.PropertyChanged += (sender, args) => calls.Add(args.PropertyName);

        instance.Property1 = "Test";

        Assert.IsTrue(new[] { "Property1" }.SequenceEqual(calls));
    }

#if (DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(_weaverHelper.OriginalAssemblyPath, _weaverHelper.NewAssemblyPath);
    }
#endif
}