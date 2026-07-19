using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes;

namespace AfuSandbox;

public class RadialMenu<T> : IEnumerable<T> //where T : Il2CppObjectBase
{
    private readonly List<object> _entries = [];

    public int Count => _entries.Count;


    public void Add(object value)
    {
        if (value is RadialMenu<T> subMenu)
        {
            if (subMenu._entries.Count == 0) return;

            _entries.Add(subMenu);
        } 
        else if (
            value is T cast
        ) {
            _entries.Add(cast);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this.GetAllRealEntries().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    public EntryType Get(int index, out RadialMenu<T> subMenu, out T entry)
    {
        subMenu = default;
        entry = default;

        if (_entries[index % _entries.Count] is RadialMenu<T> sM)
        {
            subMenu = sM;
            return EntryType.SubMenu;
        }
        else if (_entries[index % _entries.Count] is T cast)
        {
            entry = cast;
            return EntryType.Entry;
        }

        return EntryType.InvalidEntry;
    }

    public EntryType GetIl2Cpp<I>(int index, out RadialMenu<T> subMenu, out T entry) where I : Il2CppObjectBase
    {
        subMenu = default;
        entry = default;

        if (_entries[index % _entries.Count] is RadialMenu<T> sM)
        {
            subMenu = sM;
            return EntryType.SubMenu;
        } else if (_entries[index % _entries.Count] is Il2CppObjectBase entryIl2cpp
            && entryIl2cpp.TryCast<I>() is T cast)
        {
            entry = cast;
            return EntryType.Entry;
        }
        

        return EntryType.InvalidEntry;
    }

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
}

public enum EntryType {
    Entry,
    SubMenu,
    InvalidEntry,
}
