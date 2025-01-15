using Domore.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if !NET40
using System.Runtime.CompilerServices;
#endif

namespace Domore.Notification {
    /// <summary>
    /// Base of types that raise <see cref="INotifyPropertyChanged.PropertyChanged"/> events.
    /// </summary>
    public partial class Notifier : NotifyPropertyChangedImplementation, INotifyPropertyChanging {
        private static readonly PropertyChangedEventArgs EmptyEventArgs = new(string.Empty);

        internal bool PreviewPropertyChange(string propertyName) {
            return PreviewPropertyChange(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets or sets a value that indicates whether or not <see cref="INotifyPropertyChanged.PropertyChanged"/> and <see cref="INotifyPropertyChanging.PropertyChanging"/> events will be raised.
        /// </summary>
        protected internal bool NotifyState { get; set; } = true;

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (NotifyState) {
                base.OnPropertyChanged(e);
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e) {
            if (NotifyState) {
                PropertyChanging?.Invoke(this, e);
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
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="propertyName">The value of <see cref="PropertyChangingEventArgs.PropertyName"/> raised with the event.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanging(string propertyName) {
            if (PropertyChanging != null) {
                OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The value of <see cref="PropertyChangedEventArgs.PropertyName"/> raised with the event.</param>
        /// <param name="dependentPropertyNames">The names of additional properties for which <see cref="INotifyPropertyChanged.PropertyChanged"/> events will be raised.</param>
        protected void NotifyPropertyChanging(string propertyName, params string[] dependentPropertyNames) {
            if (PropertyChanging != null) {
                OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
                if (dependentPropertyNames != null && dependentPropertyNames.Length > 0) {
                    foreach (var dependentPropertyName in dependentPropertyNames) {
                        OnPropertyChanging(new PropertyChangingEventArgs(dependentPropertyName));
                    }
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
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanging(PropertyChangingEventArgs e) {
            OnPropertyChanging(e);
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="e">The arguments passed to the event invocation.</param>
        /// <param name="dependents">Additional arguments for which <see cref="INotifyPropertyChanging.PropertyChanging"/> events will be raised.</param>
        protected void NotifyPropertyChanging(PropertyChangingEventArgs e, params PropertyChangingEventArgs[] dependents) {
            if (PropertyChanging != null) {
                OnPropertyChanging(e);
                if (dependents != null && dependents.Length > 0) {
                    foreach (var dependent in dependents) {
                        OnPropertyChanging(dependent);
                    }
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
            NotifyPropertyChanged(notified.PropertyChangedEventArgs);
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
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="notified">The object that changed.</param>
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void NotifyPropertyChanging(Notified notified) {
            if (null == notified) throw new ArgumentNullException(nameof(notified));
            NotifyPropertyChanging(notified.PropertyChangingEventArgs);
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanging.PropertyChanging"/> event.
        /// </summary>
        /// <param name="notified">The object that changed.</param>
        /// <param name="dependents">Additional arguments for which <see cref="INotifyPropertyChanging.PropertyChanging"/> events will be raised.</param>
        protected void NotifyPropertyChanging(Notified notified, params Notified[] dependents) {
            if (PropertyChanging != null) {
                NotifyPropertyChanging(notified);
                if (dependents != null && dependents.Length > 0) {
                    foreach (var dependent in dependents) {
                        NotifyPropertyChanging(dependent);
                    }
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

        /// <summary>
        /// Called before the equality check in the various Change methods. When overridden, the method is called once for every call of
        /// any of the Change methods, regardless of whether or not a change will occur.
        /// </summary>
        /// <param name="e">The argument that contains the name of the property that may change.</param>
        protected virtual bool PreviewPropertyChange(PropertyChangedEventArgs e) {
            return true;
        }

        protected bool Change<T>(Notified<T> notified, T value, params Notified[] dependents) {
            if (null == notified) throw new ArgumentNullException(nameof(notified));
            var prev = PreviewPropertyChange(notified.PropertyChangedEventArgs);
            if (prev == false) {
                return false;
            }
            var changed = notified.Change(value, _ => NotifyPropertyChanging(notified, dependents));
            if (changed) {
                NotifyPropertyChanged(notified, dependents);
                return true;
            }
            return false;
        }

        public event PropertyChangingEventHandler PropertyChanging;

#if NET45_OR_GREATER || NETCOREAPP
        /// <summary>
        /// An implementation of <see cref="INotifyDataErrorInfo"/>.
        /// </summary>
        public abstract class WithErrorInfo : Notifier, INotifyDataErrorInfo {
            private event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

            private readonly Dictionary<string, HashSet<object>> Errors = [];

            private void NotifyErrorsChanged(DataErrorsChangedEventArgs e) {
                OnErrorsChanged(e);
            }

            private void NotifyErrorsChanged(string propertyName) {
                NotifyErrorsChanged(new DataErrorsChangedEventArgs(propertyName));
            }

            /// <summary>
            /// Gets a flag that indicates whether or not any errors are present.
            /// </summary>
            protected bool HasErrors {
                get {
                    lock (Errors) {
                        return Errors.Count > 0;
                    }
                }
            }

            /// <summary>
            /// Gets the errors associated with <paramref name="propertyName"/>.
            /// </summary>
            /// <param name="propertyName">The name of the property whose associated errors should be returned.</param>
            /// <returns>The errors associated with the specified property.</returns>
            protected IEnumerable GetErrors(string propertyName) {
                propertyName = string.IsNullOrWhiteSpace(propertyName)
                    ? ""
                    : propertyName;
                lock (Errors) {
                    return Errors.TryGetValue(propertyName, out var list)
                        ? list
                        : null;
                }
            }

            /// <summary>
            /// Adds an error associated with <paramref name="propertyName"/>.
            /// </summary>
            /// <param name="propertyName">The name of the property to which the error is associated.</param>
            /// <param name="error">The error to be added.</param>
            /// <returns>True if the error was added. Otherwise, false.</returns>
            protected bool AddError(string propertyName, object error) {
                propertyName = string.IsNullOrWhiteSpace(propertyName)
                    ? ""
                    : propertyName;
                lock (Errors) {
                    if (Errors.TryGetValue(propertyName, out var list) == false) {
                        Errors[propertyName] = list = [];
                    }
                    var added = list.Add(error);
                    if (added == false) {
                        return false;
                    }
                }
                NotifyErrorsChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Removes an error associated with <paramref name="propertyName"/>.
            /// </summary>
            /// <param name="propertyName">The name of the property to which the error is associated.</param>
            /// <param name="error">The error to be removed.</param>
            /// <returns>True if an error was removed. Otherwise, false.</returns>
            protected bool RemoveError(string propertyName, object error) {
                propertyName = string.IsNullOrWhiteSpace(propertyName)
                    ? ""
                    : propertyName;
                lock (Errors) {
                    if (Errors.TryGetValue(propertyName, out var list) == false) {
                        return false;
                    }
                    var removed = list.Remove(error);
                    if (removed == false) {
                        return false;
                    }
                    if (list.Count == 0) {
                        Errors.Remove(propertyName);
                    }
                }
                NotifyErrorsChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Clears all errors associated with <paramref name="propertyName"/>.
            /// </summary>
            /// <param name="propertyName">The name of the property to which the errors are associated.</param>
            /// <returns>True if any errors were cleared. Otherwise, false.</returns>
            protected bool ClearErrors(string propertyName) {
                propertyName = string.IsNullOrWhiteSpace(propertyName)
                    ? ""
                    : propertyName;
                lock (Errors) {
                    var removed = Errors.Remove(propertyName);
                    if (removed == false) {
                        return false;
                    }
                }
                NotifyErrorsChanged(propertyName);
                return true;
            }

            /// <summary>
            /// Clears all errors.
            /// </summary>
            /// <returns>True if any errors were cleared. Otherwise, false.</returns>
            protected bool ClearErrors() {
                lock (Errors) {
                    if (Errors.Count == 0) {
                        return false;
                    }
                    Errors.Clear();
                }
                NotifyErrorsChanged("");
                return true;
            }

            /// <summary>
            /// Raises the <see cref="INotifyDataErrorInfo.ErrorsChanged"/> event.
            /// </summary>
            /// <param name="e">The event arguments.</param>
            protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e) {
                ErrorsChanged?.Invoke(this, e);
            }

            event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged {
                add {
                    ErrorsChanged += value;
                }
                remove {
                    ErrorsChanged -= value;
                }
            }

            bool INotifyDataErrorInfo.HasErrors => HasErrors;

            IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName) {
                return GetErrors(propertyName);
            }
        }
#endif
    }
}
