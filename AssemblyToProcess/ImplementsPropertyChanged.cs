// ReSharper disable All
using AutoProperties;

using PropertyChanged;

[AddINotifyPropertyChangedInterface]
public class ImplementsPropertyChanged
{
    private readonly InnerImplementsPropertyChanged _inner = new InnerImplementsPropertyChanged();

    public string Property1 { get; set; }

    public void TestSetter()
    {
        Property1.SetBackingField("Test");
        _inner.Property1.SetBackingField("Test1");
    }

    [AddINotifyPropertyChangedInterface]
    public class InnerImplementsPropertyChanged
    {
        public string Property1 { get; set; }
    }
}

