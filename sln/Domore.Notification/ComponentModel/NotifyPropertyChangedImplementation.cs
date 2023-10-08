using System.ComponentModel;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.ComponentModel {
    public class NotifyPropertyChangedImplementation : INotifyPropertyChanged {
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected internal virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
