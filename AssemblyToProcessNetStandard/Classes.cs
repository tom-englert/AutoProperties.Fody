// ReSharper disable All

using System;
using System.Collections.Generic;
using System.Reflection;

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

        FieldInfo.GetFieldFromHandle(new RuntimeFieldHandle());
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
#pragma warning disable 649
    private bool _x;
#pragma warning restore 649

    public ClassWithExplicitInitializedAutoPropertiesAndExplicitBypassAutoPropertySettersWithComplexParameter()
    {
        Property2.SetBackingField("Test" + Math.Abs(2));
        Property1.SetBackingField(Property2 + "A");
        Property1.SetBackingField(Property2.TrimEnd('2'));
        Property3.SetBackingField(Property1 == "Test2A" || !_x);
    }

    public string Property1 { get; set; }

    public string Property2 { get; set; }

    public bool Property3 { get; set; }

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

public class ClassWithSimpleInterceptors
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType
#if TEST_NETSTANDARD
        , PropertyInfo property
#endif
        )
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; } = 7;

    public string Property2 { get; set; }

    public string Property3 => Property2 + "!";
}
