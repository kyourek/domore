# Domore.Notification

A small base class for implementing `INotifyPropertyChanged`, `INotifyPropertyChanging`, and `INotifyDataErrorInfo` in view models and other observable objects.

Install the package with `dotnet add package Domore.Notification`.

## Raise change events

Derive from `Notifier` and call `Change` from property setters. `Change` compares the new value with the field. When the value is different, it raises `PropertyChanging`, sets the field, raises `PropertyChanged`, and returns `true`. When nothing changed, it raises no events and returns `false`.

```csharp
using Domore.Notification;

public sealed class Person : Notifier {
    public string FirstName {
        get => _FirstName;
        set => Change(ref _FirstName, value, nameof(FirstName), nameof(FullName));
    }
    private string _FirstName;

    public int Age {
        get => _Age;
        set => Change(ref _Age, value);
    }
    private int _Age;

    public string FullName => $"{FirstName}".Trim();
}
```

```csharp
var person = new Person();
person.PropertyChanging += (s, e) => Console.WriteLine($"Changing: {e.PropertyName}");
person.PropertyChanged += (s, e) => Console.WriteLine($"Changed:  {e.PropertyName}");

person.FirstName = "Ada"; // Changing: FirstName, Changing: FullName, Changed: FirstName, Changed: FullName
person.FirstName = "Ada"; // (no events)
person.Age = 36;          // Changing: Age, Changed: Age
```

The property name comes from `[CallerMemberName]`, so `Change(ref field, value)` is enough inside a setter. Any extra names passed after the property name are dependent properties, which are notified along with it. On .NET Framework 4.0, `[CallerMemberName]` isn't available, so pass the property name explicitly.

`Change` has overloads for the built-in numeric types, `bool`, `char`, `string`, and their nullable forms, which compare with `==` to avoid boxing. A generic overload handles every other type using `EqualityComparer<T>.Default`. Overloads that take `PropertyChangedEventArgs` let you reuse cached event arguments instead of property names.

## Wrap values with `Notified<T>`

`Notified<T>` holds a property's value along with its name and cached event arguments, so the backing store and its name live in one place:

```csharp
public sealed class Person : Notifier {
    public string LastName {
        get => _LastName.Value;
        set => Change(_LastName, value, _FullName);
    }
    private readonly Notified<string> _LastName = new(nameof(LastName));
    private readonly Notified<string> _FullName = new(nameof(FullName));

    public string FullName => $"{LastName}".Trim();
}
```

Any `Notified` instances passed after the value are dependent properties that are notified along with it.

## Raise events manually

`NotifyPropertyChanged` and `NotifyPropertyChanging` raise events directly. They accept property names, event arguments, or `Notified` instances, each with optional dependents. Calling `NotifyPropertyChanged()` with no arguments raises `PropertyChanged` with an empty property name, which tells bindings that all properties may have changed.

## Suppress or veto changes

Set `NotifyState` to `false` to stop raising events without stopping values from changing. This is useful for batch updates:

```csharp
public void Load(string firstName, int age) {
    NotifyState = false;
    try {
        FirstName = firstName;
        Age = age;
    }
    finally {
        NotifyState = true;
    }
    NotifyPropertyChanged();
}
```

Override `PreviewPropertyChange` to veto a change before it happens. It's called on every `Change` call, before the equality check. Returning `false` leaves the value unchanged, raises no events, and makes `Change` return `false`:

```csharp
public sealed class Document : Notifier {
    public bool IsReadOnly { get; set; }

    public string Title {
        get => _Title;
        set => Change(ref _Title, value);
    }
    private string _Title;

    protected override bool PreviewPropertyChange(PropertyChangedEventArgs e) {
        return IsReadOnly == false;
    }
}
```

`OnPropertyChanged` and `OnPropertyChanging` are also virtual, for types that need to intercept every event.

## Report validation errors

Derive from `Notifier.WithErrorInfo` to implement `INotifyDataErrorInfo` (not available on .NET Framework 4.0). Use `AddError`, `RemoveError`, and `ClearErrors` to manage errors. Each call that changes the set of errors raises `ErrorsChanged`.

```csharp
public sealed class Account : Notifier.WithErrorInfo {
    public string Email {
        get => _Email;
        set {
            if (Change(ref _Email, value)) {
                ClearErrors(nameof(Email));
                if (value?.Contains("@") != true) {
                    AddError(nameof(Email), "Email must contain '@'.");
                }
            }
        }
    }
    private string _Email;
}
```

`HasErrors` and `GetErrors` are protected on the class and exposed publicly through `INotifyDataErrorInfo`. A `null` or blank property name refers to errors for the entire object.
