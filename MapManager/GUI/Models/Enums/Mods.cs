using OsuParsers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    private static readonly Dictionary<OsuParsers.Enums.Mods, Mods> ModsToMyModsMapping = new()
    {
        { OsuParsers.Enums.Mods.None, Mods.nm },
        { OsuParsers.Enums.Mods.NoFail, Mods.nf },
        { OsuParsers.Enums.Mods.Easy, Mods.ez },
        { OsuParsers.Enums.Mods.TouchDevice, Mods.td },
        { OsuParsers.Enums.Mods.Hidden, Mods.hd },
        { OsuParsers.Enums.Mods.HardRock, Mods.hr },
        { OsuParsers.Enums.Mods.SuddenDeath, Mods.sd },
        { OsuParsers.Enums.Mods.DoubleTime, Mods.dt },
        { OsuParsers.Enums.Mods.Relax, Mods.rx },
        { OsuParsers.Enums.Mods.HalfTime, Mods.ht },
        { OsuParsers.Enums.Mods.Nightcore, Mods.nc },
        { OsuParsers.Enums.Mods.Flashlight, Mods.fl },
        { OsuParsers.Enums.Mods.Autoplay, Mods.ap },
        { OsuParsers.Enums.Mods.SpunOut, Mods.so },
        { OsuParsers.Enums.Mods.Relax2, Mods.rx2 },
        { OsuParsers.Enums.Mods.Perfect, Mods.pf },
        { OsuParsers.Enums.Mods.Key4, Mods.k4 },
        { OsuParsers.Enums.Mods.Key5, Mods.k5 },
        { OsuParsers.Enums.Mods.Key6, Mods.k6 },
        { OsuParsers.Enums.Mods.Key7, Mods.k7 },
        { OsuParsers.Enums.Mods.Key8, Mods.k8 },
        { OsuParsers.Enums.Mods.FadeIn, Mods.fi },
        { OsuParsers.Enums.Mods.Random, Mods.rd },
        { OsuParsers.Enums.Mods.Cinema, Mods.cm },
        { OsuParsers.Enums.Mods.Target, Mods.tg },
        { OsuParsers.Enums.Mods.Key9, Mods.k9 },
        { OsuParsers.Enums.Mods.KeyCoop, Mods.kc },
        { OsuParsers.Enums.Mods.Key1, Mods.k1 },
        { OsuParsers.Enums.Mods.Key3, Mods.k3 },
        { OsuParsers.Enums.Mods.Key2, Mods.k2 },
        { OsuParsers.Enums.Mods.ScoreV2, Mods.v2 }
    };

    public static List<string> GetMappedMods(OsuParsers.Enums.Mods mods)
    {
        var result = new List<string>();

        foreach (var mapping in ModsToMyModsMapping)
        {
            if (mapping.Key != OsuParsers.Enums.Mods.None && (mods & mapping.Key) == mapping.Key)
            {
                result.Add(mapping.Value.ToString()); // Добавляем только активные моды, кроме None
            }
        }

        // Если не найдено активных модов, добавляем nm
        if (result.Count == 0)
        {
            result.Add(Mods.nm.ToString());
        }

        return result;
    }
}

