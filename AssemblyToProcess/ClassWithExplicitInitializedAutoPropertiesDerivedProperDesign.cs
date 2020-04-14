using AutoProperties;
// ReSharper disable CheckNamespace

public class ClassWithExplicitInitializedAutoPropertiesDerivedProperDesign : ClassWithExplicitInitializedAutoProperties
{
    public ClassWithExplicitInitializedAutoPropertiesDerivedProperDesign()
        : base("test", "test2")
    {
        Property3 = "test3";
    }

    public string Property3 { get; set; }
}

[BypassAutoPropertySettersInConstructors(true)]
public class ClassWithExplicitInitializedAutoPropertiesDerivedProperDesignAndBypassAutoPropertySetters : ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters
{
    public ClassWithExplicitInitializedAutoPropertiesDerivedProperDesignAndBypassAutoPropertySetters()
        : base("test", "test2")
    {
        Property3 = "test3";
    }

    public string Property3 { get; set; }
}

