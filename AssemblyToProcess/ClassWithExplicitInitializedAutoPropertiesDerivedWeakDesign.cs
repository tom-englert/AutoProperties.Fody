using AutoProperties;

public class ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesign : ClassWithExplicitInitializedAutoProperties
{
    public ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesign()
    {
        Property1 = "test";
        Property2 = "test2";
        Property3 = "test3";
    }

    public string Property3 { get; set; }
}

[BypassAutoPropertySettersInConstructors(true)]
public class ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesignAndBypassAutoPropertySetters : ClassWithExplicitInitializedAutoPropertiesAndBypassAutoPropertySetters
{
    public ClassWithExplicitInitializedAutoPropertiesDerivedWeakDesignAndBypassAutoPropertySetters()
    {
        Property1 = "test";
        Property2 = "test2";
        Property3 = "test3";
    }

    public string Property3 { get; set; }
}

