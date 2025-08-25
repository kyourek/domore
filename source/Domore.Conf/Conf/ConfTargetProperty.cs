using Domore.Conf.Converters;
using Domore.Conf.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace Domore.Conf;

internal class ConfTargetProperty {
    public object Target { get; }
    public IConfKeyPart Key { get; }
    public ConfPropertyCache Cache { get; }

    private ConfProperty Property => field ??=
        Cache.Get(TargetType, Key.Content);

    public string IndexString => _IndexString ??=
        string.Join("", Key.Indices.Select(i => $"[{string.Join(",", i.Parts.Select(p => p.Content))}]"));
    private string _IndexString;

    public object[] Index {
        get {
            if (field == null) {
                var indices = Key.Indices;
                if (indices.Count == 0) {
                    return null;
                }
                var parameters = PropertyInfo.GetIndexParameters();
                object convert(string s, Type type) {
                    var t = Nullable.GetUnderlyingType(type) ?? type;
                    if (t != null) {
                        if (t.IsEnum) {
                            return new ConfEnumFlagsConverter().Convert(s, t);
                        }
                    }
                    return Convert.ChangeType(s, type);
                }
                field = [.. indices[0] // TODO: Allow multiple indices.
                    .Parts
                    .Select((v, i) => convert(v.Content, parameters[i].ParameterType))];
            }
            return field;
        }
    }

    public Type TargetType => field ??=
        Target.GetType();

    public PropertyInfo PropertyInfo => field ??=
        Property.PropertyInfo;

    public Type PropertyType => field ??=
        PropertyInfo.PropertyType;

    public ConfAttribute ConfAttribute => field ??=
        PropertyInfo.GetConfAttribute();

    public ConfConverterAttribute ConverterAttribute => field ??=
        PropertyInfo.GetConverterAttribute();

    public bool Exists => _Exists ??= PropertyInfo != null;
    private bool? _Exists;

    public bool Populate => _Populate ??= ConfAttribute?.IgnoreSet != true;
    private bool? _Populate;

    public ConfItemProperty Item {
        get {
            if (field == null) {
                if (Key.Indices.Count > 0) {
                    var itemTarget = PropertyValue;
                    if (itemTarget == null) {
                        itemTarget = PropertyValue = Activator.CreateInstance(PropertyInfo.PropertyType);
                    }
                    field = ConfItemProperty.Create(itemTarget, new ItemKey(Key.Indices[0]), Cache);
                }
            }
            return field;
        }
    }

    public virtual object PropertyValue {
        get => PropertyInfo.GetValue(Target, null);
        set => PropertyInfo.SetValue(Target, value, null);
    }

    public ConfTargetProperty(object target, IConfKeyPart key, ConfPropertyCache cache) {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    private sealed class ItemKey : IConfKeyPart {
        public string Content { get; }
        public IConfKeyIndex Index { get; }
        public IConfCollection<IConfKeyIndex> Indices { get; }

        public ItemKey(IConfKeyIndex index) {
            Index = index;
            Content = "Item";
            Indices = new ConfCollection<IConfKeyIndex>(Index);
        }
    }
}
