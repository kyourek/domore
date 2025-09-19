using System;
using System.Collections;
using System.Reflection;

namespace Domore.Conf;

internal class ConfItemProperty : ConfTargetProperty {
    public override object PropertyValue {
        get => PropertyInfo.GetValue(Target, Index);
        set => PropertyInfo.SetValue(Target, value, Index);
    }

    public bool IndexExists {
        get {
            try {
                PropertyInfo.GetValue(Target, Index);
            }
            catch {
                return false;
            }
            return true;
        }
    }

    public ConfItemProperty(object target, IConfKeyPart key, ConfPropertyCache cache) : base(target, key, cache) {
    }

    public static ConfItemProperty Create(object target, IConfKeyPart key, ConfPropertyCache cache) {
        if (target is IList list) {
            return new ListProperty(list, key, cache);
        }
        if (target is IDictionary dictionary) {
            return new DictionaryProperty(dictionary, key, cache);
        }
        return new ConfItemProperty(target, key, cache);
    }

    private sealed class ListProperty : ConfItemProperty {
        public IList List { get; }
        public new int Index => (int)base.Index[0];

        public sealed override object PropertyValue {
            get => List.Count > Index ? List[Index] : null;
            set {
                var type = PropertyType;
                var list = List;
                var index = Index;
                var @default = type.IsValueType
                    ? Activator.CreateInstance(type)
                    : null;

                while (list.Count < index) {
                    list.Add(@default);
                }
                if (list.Count > index) {
                    list[index] = value;
                }
                else {
                    list.Add(value);
                }
            }
        }

        public ListProperty(IList list, IConfKeyPart key, ConfPropertyCache cache) : base(list, key, cache) {
            List = list ?? throw new ArgumentNullException(nameof(list));
        }
    }

    private sealed class DictionaryProperty : ConfItemProperty {
        public IDictionary Dictionary { get; }
        public new object Index => base.Index[0];

        public sealed override object PropertyValue {
            get => Dictionary[Index];
            set => Dictionary[Index] = value;
        }

        public DictionaryProperty(IDictionary dictionary, IConfKeyPart key, ConfPropertyCache cache) : base(dictionary, key, cache) {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }
    }
}
