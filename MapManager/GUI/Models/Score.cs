using DynamicData;
using MapManager.GUI.Models.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MapManager.GUI.Models;

public class Score : ReactiveObject
{
    public int Index { get; set; }

    public int OsuVersion { get; set; }

    public string BeatmapMD5Hash { get; set; }

    public string PlayerName { get; set; }

    public string ReplayMD5Hash { get; set; }

    public ushort Count300 { get; set; }

    public ushort Count100 { get; set; }

    public ushort Count50 { get; set; }

    public ushort CountGeki { get; set; }

    public ushort CountKatu { get; set; }

    public ushort CountMiss { get; set; }

    public int ReplayScore { get; set; }

    public ushort Combo { get; set; }

    public bool PerfectCombo { get; set; }

    public ObservableCollection<string> Mods { get; set; } = new();

    public DateTime ScoreTimestamp { get; set; }

    public long? ScoreId { get; set; }

    private double _accuracy;

    public double Accuracy
    {
        get => _accuracy;
        set => this.RaiseAndSetIfChanged(ref _accuracy, value);
    }

    public Score(
        int index, int osuVersion,
        string beatmapMD5Hash, string playerName,
        string replayMD5Hash, ushort count300, ushort count100,
        ushort count50, ushort countGeki, ushort countKatu,
        ushort countMiss, int replayScore, ushort combo,
        bool perfectCombo, int mods,
        DateTime scoreTimestamp, long? scoreId)
    {
        Index = index;
        OsuVersion = osuVersion;
        BeatmapMD5Hash = beatmapMD5Hash;
        PlayerName = playerName;
        ReplayMD5Hash = replayMD5Hash;
        Count300 = count300;
        Count100 = count100;
        Count50 = count50;
        CountGeki = countGeki;
        CountKatu = countKatu;
        CountMiss = countMiss;
        ReplayScore = replayScore;
        Combo = combo;
        PerfectCombo = perfectCombo;
        Mods.AddRange(ModsMapper.GetMappedMods((int)mods));
        ScoreTimestamp = scoreTimestamp;
        ScoreId = scoreId;
        Accuracy = CalculateAccuracy(count300, count100, count50, countMiss);
    }

    public string Rank
    {
        get
        {
            int total = Count300 + Count100 + Count50 + CountMiss;
            if (total == 0) return "D";

            double ratio300 = (double)Count300 / total;
            double ratio50  = (double)Count50  / total;

            string baseRank;
            if (ratio300 == 1.0)
                baseRank = "X";
            else if (ratio300 > 0.9 && ratio50 <= 0.01 && CountMiss == 0)
                baseRank = "S";
            else if ((ratio300 > 0.8 && CountMiss == 0) || ratio300 > 0.9)
                baseRank = "A";
            else if ((ratio300 > 0.7 && CountMiss == 0) || ratio300 > 0.8)
                baseRank = "B";
            else if (ratio300 > 0.6)
                baseRank = "C";
            else
                baseRank = "D";

            // XH / SH для Hidden или Flashlight
            bool silver = Mods.Contains("hd") || Mods.Contains("fl");
            if (silver && (baseRank == "X" || baseRank == "S"))
                return baseRank + "H";

            return baseRank;
        }
    }

    public static double CalculateAccuracy(ushort count300, ushort count100, ushort count50, ushort countMiss)
    {
        // Числитель формулы
        double numerator = 300 * count300 + 100 * count100 + 50 * count50;

        // Знаменатель формулы
        double denominator = 300 * (count300 + count100 + count50 + countMiss);

        // Проверка на деление на 0
        if (denominator == 0)
            return 0;

        // Расчёт Accuracy
        return numerator / denominator;
    }
}