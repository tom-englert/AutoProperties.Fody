using System;
using System.Collections;
using System.Collections.Generic;

using AutoProperties;

public class ClassWithExplicitInitializedAutoProperties : ObservableObject
{
    public ClassWithExplicitInitializedAutoProperties()
    {
        Property1 = "Test";
        Property2 = "Test2";
    }

    protected ClassWithExplicitInitializedAutoProperties(string property1, string property2)
    {
        Property1 = property1;
        Property2 = property2;
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

public class DerivedClassWithExplicitInitializedAutoProperties : ClassWithExplicitInitializedAutoProperties
{
    private readonly IList<string> _changes;

    public DerivedClassWithExplicitInitializedAutoProperties()
    {
        _changes = new List<string>();
    }

    public DerivedClassWithExplicitInitializedAutoProperties(IList<string> changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        _changes.Add(propertyName);

        base.OnPropertyChanged(propertyName);
    }
}

[BypassAutoPropertySettersInConstructors(true)]
public class ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters : ObservableObject
{
    private ImplementsPropertyChanged _anotherClass = new ImplementsPropertyChanged();
    private PocoClass _pocoClass = new PocoClass();

    public ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters()
    {
        Property1 = "Test";
        Property2 = "Test2";

        // make sure we don't interfere with properties from other classes, just because they have the same name.
        _anotherClass.Property1 = "Another value";
        _pocoClass.Property1 = "Another value";
    }

    protected ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters(string property1, string property2)
    {
        Property1 = property1;
        Property2 = property2;
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

public class ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySetters : ObservableObject
{
    public ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySetters()
    {
        Property1.SetBackingField("Test");
        Property2.SetBackingField("Test2");
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

public class ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithComplexParameter : ObservableObject
{
    public ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithComplexParameter()
    {
        // That's too complex, SetBackingField will fail.
        Property2.SetBackingField("Test" + Math.Abs(2));
        Property1.SetBackingField(Property2 + "A");
        Property1.SetBackingField(Property2.TrimEnd('2'));
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

public class ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithVariableParameters : ObservableObject
{
    public ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithVariableParameters()
    {
        var value = "Test" + Math.Abs(2);

        Property2.SetBackingField(value);

        var value2 = Property2 + "A";

        Property1.SetBackingField(value2);
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

[BypassAutoPropertySettersInConstructors(true)]
public class ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySettersAndExplicitSetProperty1 : ObservableObject
{
    public ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySettersAndExplicitSetProperty1()
    {
        Property1.SetProperty("Test");
        Property2 = "Test2";
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool IsChanged { get; set; }
}

public class PocoClass
{ 
    public string Property1 { get; set; }
}