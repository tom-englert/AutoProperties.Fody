using System;
using System.Reflection;

using AutoProperties;

using TestLibrary;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

public class ClassWithSimpleInterceptors
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }

    public string Property3 => Property2 + "!";
}

public class ClassWithReadonlyProperty
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }

    public string Property3 { get; }
}

public abstract class ClassWithAbstractProperty
{
    private int _field = 42;

    [GetInterceptor]
    protected object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    protected void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public abstract int Property1 { get; set; }

    public abstract string Property2 { get; set; }
}

public class ClassDerivedFromClassWithAbstractProperty : ClassWithAbstractProperty
{
    public override int Property1 { get; set; }

    public override string Property2 { get; set; }
}

public class ClassWithGenericInterceptors
{
    private int _field = 42;

    [GetInterceptor]
    private T GetInterceptor<T>(string propertyName)
    {
        return (T)Convert.ChangeType(_field, typeof(T));
    }

    [SetInterceptor]
    private void SetInterceptor<T>(T value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithMixedInterceptors
{
    private int _field = 42;

    [GetInterceptor]
    private T GetInterceptor<T>(string propertyName)
    {
        return (T)Convert.ChangeType(_field, typeof(T));
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithExternalInterceptorsBase : ExternalInterceptorBase
{
    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithExternalGenericInterceptorsBase : ExternalInterceptorBaseWithGenerics
{
    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithGenericInterceptorsAndFieldReference
{
    [GetInterceptor]
    private T GetInterceptor<T>(string propertyName, T fieldValue)
    {
        return (T)Convert.ChangeType(Convert.ToInt32(fieldValue) + 1, typeof(T));
    }

    [SetInterceptor]
    private void SetInterceptor<T>(T value, string propertyName, out T field)
    {
        field = (T)Convert.ChangeType(Convert.ToInt32(value) + 2, typeof(T));
    }

    public int Property1 { get; set; } = GetDefault() + 1;

    public string Property2 { get; set; } = "8";

    private static int GetDefault() => 6;
}

public class ClassWithMixedGenericInterceptorsAndFieldReference
{
    [GetInterceptor]
    private object GetInterceptor(object fieldValue, FieldInfo fieldInfo)
    {
        return Convert.ChangeType(Convert.ToInt32(fieldValue) + 1, fieldInfo.FieldType);
    }

    [SetInterceptor]
    private void SetInterceptor<T>(T value, string propertyName, out T field)
    {
        field = (T)Convert.ChangeType(Convert.ToInt32(value) + 2, typeof(T));
    }

    public int Property1 { get; set; } = 7;

    public string Property2 { get; set; } = "8";
}

public class ClassWithInterceptorsUsingAllPossibleParameters
{
    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo)
    {
        return Convert.ChangeType(Convert.ToInt32(fieldInfo.GetValue(this)) + 1, fieldInfo.FieldType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo)
    {
        fieldInfo.SetValue(this, Convert.ChangeType(Convert.ToInt32(value) + 2, fieldInfo.FieldType));
    }

    public int Property1 { get; set; } = 7;

    public string Property2 { get; set; } = "8";
}

public class BaseWithPrivateInterceptors
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }
}

public class DerivedFromBaseWithPrivateInterceptors : BaseWithPrivateInterceptors
{
    public DerivedFromBaseWithPrivateInterceptors()
    {
        Property1 = 7;
        Property2 = "8";
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithDoubleInterceptors
{
    private int _field = 42;

    public ClassWithDoubleInterceptors()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [GetInterceptor]
    private object GetInterceptor2(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithMissingGetInterceptor
{
    private int _field = 42;

    public ClassWithMissingGetInterceptor()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithBadGenericInterceptors
{
    private int _field = 42;

    public ClassWithBadGenericInterceptors()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private T GetInterceptor<T, T1>(string propertyName, T1 invalid)
    {
        return (T)Convert.ChangeType(_field, typeof(T));
    }

    [SetInterceptor]
    private void SetInterceptor<T, T1>(T value, string propertyName, T1 invalid)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}


public class ClassWithUnsupportedParameter
{
    private int _field = 42;

    public ClassWithUnsupportedParameter()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType, int invalid)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithInterceptorAndInitializedAutoPropertiesAndIgnoredPropties
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    [InterceptIgnore]
    public int Property1 { get; set; } = 7;

    [InterceptIgnore]
    public string Property2 { get; set; } = "8";
}

public class ClassWithReadonlyPropertiesAndOnlyGetInterceptor
{
    private int _field = 42;

    [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return Convert.ChangeType(_field, propertyType);
    }

    public int Property1 { get; } 

    public string Property2 { get; }
}

public class ClassWithBadReturnTypeInGetter
{
    private int _field = 42;

    public ClassWithBadReturnTypeInGetter()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private int GetInterceptor(string propertyName, Type propertyType, int invalid)
    {
        return 42;
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithBadReturnTypeInGenericGetter
{
    private int _field = 42;

    public ClassWithBadReturnTypeInGenericGetter()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private object GetInterceptor<T>(string propertyName, Type propertyType, T invalid)
    {
        return 42;
    }

    [SetInterceptor]
    private void SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class ClassWithBadReturnTypeInSetter
{
    private int _field = 42;

    public ClassWithBadReturnTypeInSetter()
    {
        Property1 = 7;
        Property2 = "8";
    }

    [GetInterceptor]
    private T GetInterceptor<T>(string propertyName, Type propertyType, T value)
    {
        return (T)Convert.ChangeType(_field, typeof(T));
    }

    [SetInterceptor]
    private object SetInterceptor(object value, string propertyName)
    {
        _field = Convert.ToInt32(value);
        return null;
    }

    public int Property1 { get; set; }

    public string Property2 { get; set; }
}

public class BaseClassWithInterceptors
{
    public BaseClassWithInterceptors Prop { get; set; }

    [GetInterceptor]
    protected T GetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue)
    {
        return fieldValue;
    }

    [SetInterceptor]
    protected void SetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue, T newValue)
    {
        fieldValue = newValue;
    }
}

public class SubClassWithPropertyOverride : BaseClassWithInterceptors
{
    public new SubClassWithPropertyOverride Prop { get; set; }
}

public class BaseClassWithVirtualProperty
{
    public virtual int Prop { get; set; }

    [GetInterceptor]
    protected T GetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue)
    {
        return fieldValue;
    }

    [SetInterceptor]
    protected void SetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue, T newValue)
    {
        fieldValue = newValue;
    }
}

public class SubClassWithVirtualPropertyOverride : BaseClassWithVirtualProperty
{
    public override int Prop { get; set; }
}
