using System.Collections.Generic;
using System.ComponentModel;

namespace Domore.Notification {
    internal class NotifierSubject1 : NotifierSubject0 {
        protected override bool PreviewPropertyChange(PropertyChangedEventArgs e) {
            return PreviewPropertyChangeLookup[e.PropertyName];
        }

        public Dictionary<string, bool> PreviewPropertyChangeLookup { get; } = [];

        public string N_string_ {
            get => _N_string_.Value;
            set => Change(_N_string_, value);
        }
        private readonly Notified<string> _N_string_ = new(nameof(N_string_));
    }
}
