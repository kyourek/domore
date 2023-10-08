using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Domore.Collections.ObjectModel {
    [Guid("17007D13-03B4-459C-973B-11234D626A64")]
    [ComVisible(true)]
#if NETFRAMEWORK
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#endif
    public interface IIndexedCollection : IEnumerable {
        [DispId(1)]
        int Count { get; }
    }

    public interface IIndexedCollection<TIndex, out TItem> : IIndexedCollection, IEnumerable<TItem> {
        TItem this[TIndex index] { get; }
        bool Contains(TIndex index);
        IEnumerable<TIndex> Indexes();
        IEnumerable<TItem> Items(params TIndex[] indexes);
        IEnumerable<TItem> Items(IEnumerable<TIndex> indexes);
    }
}
