using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Tests;

public class TypesWithInitializedPropertiesTests
{
    readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Test]
    // default behavior (baseline)
    [TestCase("ClassWithInlineInitializedAutoProperties", 
        "Test", "Test2", false, new string[0])]
    [TestCase("ClassWithExplicitInitializedAutoProperties", 
        "Test", "Test2", true, new[]{ "IsChanged", "Property1", "Property2" })]
    //[TestCase("DerivedClassWithExplicitInitializedAutoProperties",
    //    "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [TestCase("ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesign", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2", "Property1", "Property2", "Property3" })]
    [TestCase("ClassWithExplicitInitializedAutoPropertiesDerivedProperDesign", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2", "Property3" })]
    [TestCase("ClassWithAutoPropertiesInitializedInSeparateMethod", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [TestCase("ClassWithExplicitInitializedBackingFieldProperties", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    // with class level [BypassAutoPropertySettersInConstructors(true)]
    [TestCase("ClassWithInlineInitializedAutoPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", false, new string[0])]
    [TestCase("ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", false, new string[0])]
    [TestCase("ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesignAndBypassAutoPropertySetters", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [TestCase("ClassWithExplicitInitializedAutoPropertiesDerivedProperDesignAndBypassAutoPropertySetters", 
        "test", "test2", false, new string[0])]
    [TestCase("ClassWithAutoPropertiesInitializedInSeparateMethodAndBypassAutoPropertySetters", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [TestCase("ClassWithExplicitInitializedBackingFieldPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    // with .SetBackingField..
    [TestCase("ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySetters",
        "Test", "Test2", false, new string[0])]
    // with class level [BypassAutoPropertySettersInConstructors(true)] and .SetProperty...
    [TestCase("ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySettersAndExplicitSetProperty1",
        "Test", "Test2", true, new[] { "IsChanged", "Property1" })]

    public void TypesWithInitializedPropertiesTest(string className, string property1Value, string property2Value, bool isChangedStateAfterConstructor, string[] expectedPropertyChangedCallsInConstructor)
    {
        var instance = assembly.GetInstance(className);

        var eventCount = 0;
        ((INotifyPropertyChanged)instance).PropertyChanged += (sender, args) =>
        {
            eventCount++;
        };

        Assert.AreEqual(property1Value, instance.Property1);
        Assert.AreEqual(property2Value, instance.Property2);

        var actualPropertyChangedCalls = (IList<string>)instance.PropertyChangedCalls;
        Debug.WriteLine("PropertyChanged calls: " + string.Join(", ", actualPropertyChangedCalls));

        Assert.IsTrue(expectedPropertyChangedCallsInConstructor.SequenceEqual(actualPropertyChangedCalls));
        Assert.AreEqual(isChangedStateAfterConstructor, instance.IsChanged);

        var initial = isChangedStateAfterConstructor ? 1 : 2;

        instance.Property1 = "a";
        Assert.AreEqual(initial, eventCount);
        Assert.IsTrue(instance.IsChanged);

        instance.IsChanged = false;
        Assert.AreEqual(initial + 1, eventCount);

        instance.Property2 = "b";
        Assert.AreEqual(initial + 3, eventCount);
        Assert.IsTrue(instance.IsChanged);
    }
}
