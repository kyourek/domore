﻿using Domore.ComponentModel;
using System;
using System.ComponentModel;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.Notification {
    /// <summary>
    /// Base of types that raise <see cref="INotifyPropertyChanged.PropertyChanged"/> events.
    /// </summary>
    public partial class Notifier : NotifyPropertyChangedImplementation {
        private static readonly PropertyChangedEventArgs EmptyEventArgs = new PropertyChangedEventArgs(string.Empty);

        /// <summary>
        /// Gets or sets a value that indicates whether or not <see cref="INotifyPropertyChanged.PropertyChanged"/> events will be raised.
        /// </summary>
        protected internal bool NotifyState { get; set; } = true;

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected internal override void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (NotifyState) {
                base.OnPropertyChanged(e);
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The value of <see cref="PropertyChangedEventArgs.PropertyName"/> raised with the event.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The value of <see cref="PropertyChangedEventArgs.PropertyName"/> raised with the event.</param>
        /// <param name="dependentPropertyNames">The names of additional properties for which <see cref="INotifyPropertyChanged.PropertyChanged"/> events will be raised.</param>
        protected void NotifyPropertyChanged(string propertyName, params string[] dependentPropertyNames) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            if (dependentPropertyNames != null && dependentPropertyNames.Length > 0) {
                foreach (var dependentPropertyName in dependentPropertyNames) {
                    OnPropertyChanged(new PropertyChangedEventArgs(dependentPropertyName));
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged(PropertyChangedEventArgs e) {
            OnPropertyChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
        /// <param name="dependents">Additional arguments for which <see cref="INotifyPropertyChanged.PropertyChanged"/> events will be raised.</param>
        protected void NotifyPropertyChanged(PropertyChangedEventArgs e, params PropertyChangedEventArgs[] dependents) {
            OnPropertyChanged(e);
            if (dependents != null && dependents.Length > 0) {
                foreach (var dependent in dependents) {
                    OnPropertyChanged(dependent);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="notified">The object that changed.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged(Notified notified) {
            if (null == notified) throw new ArgumentNullException(nameof(notified));
            NotifyPropertyChanged(notified.Event);
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="notified">The object that changed.</param>
        /// <param name="dependents">Additional arguments for which <see cref="INotifyPropertyChanged.PropertyChanged"/> events will be raised.</param>
        protected void NotifyPropertyChanged(Notified notified, params Notified[] dependents) {
            NotifyPropertyChanged(notified);
            if (dependents != null && dependents.Length > 0) {
                foreach (var dependent in dependents) {
                    NotifyPropertyChanged(dependent);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event with an empty <see cref="PropertyChangedEventArgs.PropertyName"/>.
        /// </summary>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanged() {
            NotifyPropertyChanged(EmptyEventArgs);
        }

        protected bool Change<T>(Notified<T> notified, T value, params Notified[] dependents) {
            if (notified is null) throw new ArgumentNullException(nameof(notified));
            if (notified.Same(value)) {
                return false;
            }
            notified.Value = value;
            NotifyPropertyChanged(notified, dependents);
            return true;
        }
    }
}
