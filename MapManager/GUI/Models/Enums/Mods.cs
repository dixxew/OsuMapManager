using System.Collections.Generic;

namespace MapManager.GUI.Models.Enums;
public enum Mods
{
    nm = 0,
    nf = 1,
    ez = 2,
    td = 4,
    hd = 8,
    hr = 0x10,
    sd = 0x20,
    dt = 0x40,
    rx = 0x80,
    ht = 0x100,
    nc = 0x200,
    fl = 0x400,
    ap = 0x800,
    so = 0x1000,
    rx2 = 0x2000,
    pf = 0x4000,
    k4 = 0x8000,
    k5 = 0x10000,
    k6 = 0x20000,
    k7 = 0x40000,
    k8 = 0x80000,
    fi = 0x100000,
    rd = 0x200000,
    cm = 0x400000,
    tg = 0x800000,
    k9 = 0x1000000,
    kc = 0x2000000,
    k1 = 0x4000000,
    k3 = 0x8000000,
    k2 = 0x10000000,
    v2 = 0x20000000
}
public static class ModsMapper
{
    private static readonly Dictionary<int, Mods> IntToMyModsMapping = new()
    {
        { 0, Mods.nm },
        { 1, Mods.nf },
        { 2, Mods.ez },
        { 4, Mods.td },
        { 8, Mods.hd },
        { 0x10, Mods.hr },
        { 0x20, Mods.sd },
        { 0x40, Mods.dt },
        { 0x80, Mods.rx },
        { 0x100, Mods.ht },
        { 0x200, Mods.nc },
        { 0x400, Mods.fl },
        { 0x800, Mods.ap },
        { 0x1000, Mods.so },
        { 0x2000, Mods.rx2 },
        { 0x4000, Mods.pf },
        { 0x8000, Mods.k4 },
        { 0x10000, Mods.k5 },
        { 0x20000, Mods.k6 },
        { 0x40000, Mods.k7 },
        { 0x80000, Mods.k8 },
        { 0x100000, Mods.fi },
        { 0x200000, Mods.rd },
        { 0x400000, Mods.cm },
        { 0x800000, Mods.tg },
        { 0x1000000, Mods.k9 },
        { 0x2000000, Mods.kc },
        { 0x4000000, Mods.k1 },
        { 0x8000000, Mods.k3 },
        { 0x10000000, Mods.k2 },
        { 0x20000000, Mods.v2 }
    };

    public static List<string> GetMappedMods(int mods)
    {
        var result = new List<string>();

        foreach (var mapping in IntToMyModsMapping)
        {
            if ((mods & mapping.Key) == mapping.Key)
            {
                result.Add(mapping.Value.ToString());
            }
        }

        if (result.Count == 0)
            result.Add(Mods.nm.ToString());

        return result;
    }
}

