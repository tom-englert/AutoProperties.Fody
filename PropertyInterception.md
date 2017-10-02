**NOTE:** This weaver completely replaces the existing getters and setters of all auto-properties! 
If you combine this weaver with others that modify property accessors, 
like e.g. [PropertyChanged.Fody](https://github.com/Fody/PropertyChanged), 
make sure AutoProperties is the first weaver listed in your `FodyWeavers.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Weavers>
  <AutoProperties />
  <PropertyChanged />
</Weavers>
```
---

### Intercepting getter and setter of auto-properties

Intercepting the property accessors is useful when e.g. writing a class that provides 
transparent access to store values like e.g. configuration store via properties. 
Instead of implementing every property as reading or writing the store value, 
you can simply add auto-properties for every item and implement reading and writing once in the interceptor.


Instead of 
```C#
public class MyConfiguration
{
    public string Value1
    {
        get => ConfigurationManager.AppSettings[nameof(Value1)];
        set => ConfigurationManager.AppSettings[nameof(Value1)] = value;
    }
    public string Value2
    {
        get => ConfigurationManager.AppSettings[nameof(Value2)];
        set => ConfigurationManager.AppSettings[nameof(Value2)] = value;
    }
}
```
you can write 
```C#
public class MyConfiguration
{
    [GetInterceptor]
    private object GetValue(string name) => ConfigurationManager.AppSettings[name];
    [SetInterceptor]
    private void SetValue(string name, object value) => ConfigurationManager.AppSettings[name] = value?.ToString();

    public string Value1 { get; set; }
    public string Value2 { get; set; }
}
```

Especially if there are many properties, you will save a lot of annoying and error prone typing.

### How it works

This add-in scans all classes for methods with a `[GetInterceptor]` and `[SetInterceptor]` attribute. If a class defines such methods, all getters and setters of all auto-properties in the class will be replaced by calls to the interceptors.

The interceptors must confirm to the following rules:

- The interceptors can be implemented as generics to avoid boxing value types.
- The getter must return `System.Object` or `T` if the getter is generic.
- The setter must not return a value
- The number and order of the parameters is not important. Just use the parameters you need in any order.
- The interceptors will accept any parameters of the following types, so you can get any information you need to implement your interceptor:

| Type                                  | Getter                              | Setter                              |
|---------------------------------------|-------------------------------------|-------------------------------------|
| `System.String`                     | property name                       | property name                       |
| `System.Type`                       | property type                       | property type                       | 
| `System.Reflection.PropertyInfo`  | property info of the property       | property info of the property       |
| `System.Reflection.FieldInfo`     | field info of the backing field     | field info of the backing field     |
| `System.Object`                     | value of the backing field          | new value of the property           |
| `T` (generic)                         | value of the backing field          | new value of the property          |
| `Out/Ref T` (generic)                | reference of the backing field      | reference of the backing field      |


e.g.
```C#
public class MyConfiguration
{
    [GetInterceptor]
    private T GetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object fieldValue, T genricFieldValue, ref T refToBackingField)
    {
        return default(T);
    }

    [SetInterceptor]
    private void SetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object newValue, T genricNewValue, out T refToBackingField)
    {
        refToBackingField = default(T);
    }

    public string Property { get; set; }
}
```

will become:
```C#
public class MyConfiguration
{
    private static readonly PropertyInfo <Property>k__PropertyInfo = typeof(MyConfiguration).GetProperty(nameof(Property));
    private string <Property>k__BackingField;

    private T GetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object fieldValue, T genricFieldValue, ref T refToBackingField)
    {
        return default(T);
    }

    private void SetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object newValue, T genricNewValue, out T refToBackingField)
    {
        refToBackingField = default(T);
    }

    public string Property
    {
        get
        {
            return this.GetValue<string>(nameof(Property), typeof(string), MyConfiguration.<Property>k__PropertyInfo, FieldInfo.GetFieldFromHandle(__fieldref(MyConfiguration.<Property>k__BackingField)), (object)this.<Property>k__BackingField, this.<Property>k__BackingField, ref this.<Property>k__BackingField);
        }
        set
        {
            this.SetValue<string>(nameof(Property), typeof(string), MyConfiguration.<Property>k__PropertyInfo, FieldInfo.GetFieldFromHandle(__fieldref(MyConfiguration.<Property>k__BackingField)), (object)value, value, out this.<Property>k__BackingField);
        }
    }
}
```

If you use any parameter that refers to the backing field, the backing field will be preserved. If you don't, the backing field will be removed.

A full working sample implementation can be found e.g. in [ResXResourceManager](https://github.com/tom-englert/ResXResourceManager/blob/master/ResXManager.Model/Configuration.cs)

