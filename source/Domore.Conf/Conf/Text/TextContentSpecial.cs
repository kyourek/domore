using Domore.Conf.Converters;
using System.Collections.Generic;

namespace Domore.Conf.Text;

internal sealed class TextContentSpecial {
    [ConfListItems(Separator = "\n")]
    public List<string> Include { get; set; }

    public Dictionary<string, string> Key { get; set; }
}
