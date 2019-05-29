using System;
using System.Collections.Generic;
using System.Reflection;
using AutoProperties;

public class ParentWithGenericBaseOfString : GenericBaseSome<string>
{
}

public class ParentWithGenericBaseOfInt : GenericBaseSome<int>
{
}

public abstract class GenericBaseSome<TProperty> : ChangeTrackable
{
    public TProperty GenericProperty { get; set; }
}


public abstract class ChangeTrackable
{
    [InterceptIgnore]
    public virtual HashSet<string> ChangedProperties { get; } = new HashSet<string>();

    [SetInterceptor]
    protected void SetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, object newValue, T genericNewValue,
        ref T refToBackingField)
    {
        refToBackingField = genericNewValue;
        ChangedProperties?.Add(name);
    }

    [GetInterceptor]
    protected T GetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, object fieldValue, T genericFieldValue, ref T refToBackingField)
    {
        return genericFieldValue;
    }
}

public class SomeClassWithoutGenerics : ChangeTrackable
{
    public int ValueProperty { get; set; }
    public string ReferenceProperty { get; set; }
    public string[] ArrayProperty { get; set; }
}