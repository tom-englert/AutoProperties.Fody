using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using Tests;

using Xunit;

public class AccessBackingFieldTests
{
    [NotNull]
    private readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Theory]
    // default behavior (baseline)
    [InlineData("ClassWithInlineInitializedAutoProperties", 
        "Test", "Test2", false, new string[0])]
    [InlineData("ClassWithExplicitInitializedAutoProperties", 
        "Test", "Test2", true, new[]{ "IsChanged", "Property1", "Property2" })]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesign", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2", "Property1", "Property2", "Property3" })]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesDerivedProperDesign", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2", "Property3" })]
    [InlineData("ClassWithAutoPropertiesInitializedInSeparateMethod", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [InlineData("ClassWithExplicitInitializedBackingFieldProperties", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    // with class level [BypassAutoPropertySettersInConstructors(true)]
    [InlineData("ClassWithInlineInitializedAutoPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", false, new string[0])]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", false, new string[0])]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesignAndBypassAutoPropertySetters", 
        "test", "test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesDerivedProperDesignAndBypassAutoPropertySetters", 
        "test", "test2", false, new string[0])]
    [InlineData("ClassWithAutoPropertiesInitializedInSeparateMethodAndBypassAutoPropertySetters", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    [InlineData("ClassWithExplicitInitializedBackingFieldPropertiesAndBypassAutoPropertySetters", 
        "Test", "Test2", true, new[] { "IsChanged", "Property1", "Property2" })]
    // with .SetBackingField..
    [InlineData("ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySetters",
        "Test", "Test2", false, new string[0])]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithVariableParameters",
        "Test2A", "Test2", false, new string[0])]
    [InlineData("ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithComplexParameter",
        "Test", "Test2", false, new string[0])]
    // with class level [BypassAutoPropertySettersInConstructors(true)] and .SetProperty...
    [InlineData("ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySettersAndExplicitSetProperty1",
        "Test", "Test2", true, new[] { "IsChanged", "Property1" })]

    public void Test([NotNull] string className, [CanBeNull] string property1Value, [CanBeNull] string property2Value, bool isChangedStateAfterConstructor, [NotNull, ItemNotNull] string[] expectedPropertyChangedCallsInConstructor)
    {
        var instance = assembly.GetInstance(className);

        var eventCount = 0;
        ((INotifyPropertyChanged)instance).PropertyChanged += (sender, args) =>
        {
            eventCount++;
        };

        Assert.Equal(property1Value, instance.Property1);
        Assert.Equal(property2Value, instance.Property2);

        var actualPropertyChangedCalls = (IList<string>)instance.PropertyChangedCalls;
        Debug.WriteLine("PropertyChanged calls: " + string.Join(", ", actualPropertyChangedCalls));

        Assert.True(expectedPropertyChangedCallsInConstructor.SequenceEqual(actualPropertyChangedCalls));
        Assert.Equal(isChangedStateAfterConstructor, instance.IsChanged);

        var initial = isChangedStateAfterConstructor ? 1 : 2;

        instance.Property1 = "a";
        Assert.Equal(initial, eventCount);
        Assert.True(instance.IsChanged);

        instance.IsChanged = false;
        Assert.Equal(initial + 1, eventCount);

        instance.Property2 = "b";
        Assert.Equal(initial + 3, eventCount);
        Assert.True(instance.IsChanged);
    }

    [Fact]
    public void DerivedClassWithoutAutoPropertyTweakingCrashesTest()
    {
        // ReSharper disable PossibleNullReferenceException
        Assert.Throws<TargetInvocationException>(() =>
        {
            assembly.GetInstance("DerivedClassWithExplicitInitializedAutoProperties");
        });
        // ReSharper restore PossibleNullReferenceException
    }
}
