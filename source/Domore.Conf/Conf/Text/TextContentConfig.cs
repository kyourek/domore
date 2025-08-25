using Domore.Conf.Converters;
using System.Collections.Generic;

namespace Domore.Conf.Text;

internal sealed class TextContentConfig {
    [ConfListItems(Separator = "\n")]
    public List<string> Include {
        get => _Include ?? (_Include = new());
        set => _Include = value;
    }
    private List<string> _Include;
}
