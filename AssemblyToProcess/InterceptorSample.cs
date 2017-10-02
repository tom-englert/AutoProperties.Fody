using System;
using System.Configuration;
using System.Reflection;

using AutoProperties;

namespace AssemblyToProcess
{
    public class MyConfiguration1
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

    public class MyConfiguration2
    {
        [GetInterceptor]
        private object GetValue(string name) => ConfigurationManager.AppSettings[name];
        [SetInterceptor]
        private void SetValue(string name, object value) => ConfigurationManager.AppSettings[name] = value?.ToString();

        public string Value1 { get; set; }
        public string Value2 { get; set; }
    }

    public class MyConfiguration3
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

        public string Value { get; set; }
    }
}

/*
public class MyConfiguration
{
    private static readonly PropertyInfo <Value>k__PropertyInfo = typeof(MyConfiguration).GetProperty(nameof(Value));

    private T GetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object fieldValue, T genricFieldValue, ref T refToBackingField)
    {
        return default(T);
    }

    private void SetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo, object newValue, T genricNewValue, out T refToBackingField)
    {
        refToBackingField = default(T);
    }

    public string Value
    {
        get
        {
            return this.GetValue<string>(nameof(Value), typeof(string), MyConfiguration.<Value>k__PropertyInfo, FieldInfo.GetFieldFromHandle(__fieldref(MyConfiguration.<Value>k__BackingField)), (object)this.<Value>k__BackingField, this.<Value>k__BackingField, ref this.<Value>k__BackingField);
        }
        set
        {
            this.SetValue<string>(nameof(Value), typeof(string), MyConfiguration.<Value>k__PropertyInfo, FieldInfo.GetFieldFromHandle(__fieldref(MyConfiguration.<Value>k__BackingField)), (object)value, value, out this.<Value>k__BackingField);
        }
    }
}
*/