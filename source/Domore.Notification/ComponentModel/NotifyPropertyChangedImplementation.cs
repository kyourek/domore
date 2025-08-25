using System.ComponentModel;

namespace Domore.ComponentModel;

public abstract class NotifyPropertyChangedImplementation : INotifyPropertyChanged {
    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="e">The arguments passed to the event invocation.</param>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
        PropertyChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raised when a property's value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
}
