using System.ComponentModel;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.Notification {
    using ComponentModel;

    public partial class Notifier : NotifyPropertyChangedImplementation {
        private static readonly PropertyChangedEventArgs EmptyEventArgs = new PropertyChangedEventArgs(string.Empty);

        protected internal bool NotifyState { get; set; } = true;

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected internal override void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (NotifyState) {
                base.OnPropertyChanged(e);
            }
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void NotifyPropertyChanged(string propertyName, params string[] dependentPropertyNames) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            if (dependentPropertyNames != null) {
                foreach (var dependentPropertyName in dependentPropertyNames) {
                    OnPropertyChanged(new PropertyChangedEventArgs(dependentPropertyName));
                }
            }
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged(PropertyChangedEventArgs e) {
            OnPropertyChanged(e);
        }

        protected void NotifyPropertyChanged(PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
            OnPropertyChanged(e);
            if (dependents != null) {
                foreach (var dependent in dependents) {
                    OnPropertyChanged(dependent);
                }
            }
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged() {
            NotifyPropertyChanged(EmptyEventArgs);
        }
    }
}
