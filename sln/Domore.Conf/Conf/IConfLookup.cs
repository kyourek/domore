using System.Collections.Generic;

namespace Domore.Conf {
    public interface IConfLookup {
        int Count { get; }
        bool Contains(string key);
        string Value(string key);
        IEnumerable<string> All(string key);
    }
}
