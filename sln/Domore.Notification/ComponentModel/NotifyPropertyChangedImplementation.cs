using System.ComponentModel;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.ComponentModel {
    public abstract class NotifyPropertyChangedImplementation : INotifyPropertyChanged {
        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected internal virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raised when a property's value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
