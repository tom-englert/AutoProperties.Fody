// ReSharper disable All

using System.Collections.Generic;
using System.ComponentModel;

using PropertyChanged;

public abstract class ObservableObject : INotifyPropertyChanged
{
    [DoNotNotify]
    public IList<string> PropertyChangedCalls { get; } = new List<string>();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedCalls.Add(propertyName);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}