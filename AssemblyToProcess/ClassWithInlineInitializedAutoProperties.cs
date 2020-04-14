using AutoProperties;
// ReSharper disable CheckNamespace

public class ClassWithInlineInitializedAutoProperties : ObservableObject
{
    public string Property1 { get; set; } = "Test";

    public string Property2 { get; set; } = "Test2";

    public bool IsChanged { get; set; }
}

[BypassAutoPropertySettersInConstructors(true)]
public class ClassWithInlineInitializedAutoPropertiesAndBypassAutoPropertySetters : ObservableObject
{
    public string Property1 { get; set; } = "Test";

    public string Property2 { get; set; } = "Test2";

    public bool IsChanged { get; set; }
}

