using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes;

namespace AfuSandbox;

public class RadialMenu<T> : IEnumerable<T> where T : Il2CppObjectBase
{
    private readonly List<object> _entries = [];

    public List<T> GetAllRealEntries()
    {
        List<T> realEntries = [];
        
        for (int i = 0; i < _entries.Count; i++)
        {
            switch (this.Get(i, out var subMenu, out var entry))
            {
                case EntryType.SubMenu:
                    realEntries.Concat(subMenu.GetAllRealEntries());
                    continue;

                case EntryType.Entry:
                    realEntries.Add(entry);
                    continue;
            }
        }

        return realEntries;
    }

    public void Add(object value)
    {
        if (value is RadialMenu<T> sM)
        {
            _entries.Add(sM);
        } 
        else if (value is Il2CppSystem.Object entryIl2cpp
            && entryIl2cpp.TryCast<T>() is T cast)
        {
            _entries.Add(cast);
        }
    }

    public EntryType Get(int index, out RadialMenu<T> subMenu, out T entry)
    {
        subMenu = default;
        entry = default;

        if (_entries[index % _entries.Count] is RadialMenu<T> sM)
        {
            subMenu = sM;
            return EntryType.SubMenu;
        } 
        else if (_entries[index % _entries.Count] is Il2CppSystem.Object entryIl2cpp
            && entryIl2cpp.TryCast<T>() is T cast)
        {
            entry = cast;
            return EntryType.Entry;
        }

        return EntryType.InvalidEntry;
    }


    public IEnumerator<T> GetEnumerator()
    {
        return this.GetAllRealEntries().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();
}

public enum EntryType {
    Entry,
    SubMenu,
    InvalidEntry,
}
