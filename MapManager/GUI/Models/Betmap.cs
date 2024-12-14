using OsuParsers.Database.Objects;
using OsuParsers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Models;
public class Beatmap : DbBeatmap
{
    // Свойство для привязки звездного рейтинга без модификаторов для всех режимов
    public double? StandardStarRatingNone =>
        StandardStarRating != null && StandardStarRating.TryGetValue(Mods.None, out var value)
            ? value
            : (double?)null;

    public double? TaikoStarRatingNone =>
        TaikoStarRating != null && TaikoStarRating.TryGetValue(Mods.None, out var value)
            ? value
            : (double?)null;

    public double? CatchStarRatingNone =>
        CatchStarRating != null && CatchStarRating.TryGetValue(Mods.None, out var value)
            ? value
            : (double?)null;

    public double? ManiaStarRatingNone =>
        ManiaStarRating != null && ManiaStarRating.TryGetValue(Mods.None, out var value)
            ? value
            : (double?)null;

    public string Duration =>
        TotalTime != null ? TimeSpan.FromMilliseconds(TotalTime).ToString(@"m\:ss") : "null";

    public int ObjectsCount =>
        CirclesCount + SlidersCount + SpinnersCount;

    public List<string> TagsList => Tags.Split(' ').ToList();

    public static Beatmap FromDbBeatmap(DbBeatmap dbBeatmap)
    {
        var beatmap = new Beatmap
        {
            BytesOfBeatmapEntry = dbBeatmap.BytesOfBeatmapEntry,
            Artist = dbBeatmap.Artist,
            ArtistUnicode = dbBeatmap.ArtistUnicode,
            Title = dbBeatmap.Title,
            TitleUnicode = dbBeatmap.TitleUnicode,
            Creator = dbBeatmap.Creator,
            Difficulty = dbBeatmap.Difficulty,
            AudioFileName = dbBeatmap.AudioFileName,
            MD5Hash = dbBeatmap.MD5Hash,
            FileName = dbBeatmap.FileName,
            RankedStatus = dbBeatmap.RankedStatus,
            CirclesCount = dbBeatmap.CirclesCount,
            SlidersCount = dbBeatmap.SlidersCount,
            SpinnersCount = dbBeatmap.SpinnersCount,
            LastModifiedTime = dbBeatmap.LastModifiedTime,
            ApproachRate = dbBeatmap.ApproachRate,
            CircleSize = dbBeatmap.CircleSize,
            HPDrain = dbBeatmap.HPDrain,
            OverallDifficulty = dbBeatmap.OverallDifficulty,
            SliderVelocity = dbBeatmap.SliderVelocity,
            StandardStarRating = dbBeatmap.StandardStarRating ?? new Dictionary<Mods, double>(),
            TaikoStarRating = dbBeatmap.TaikoStarRating ?? new Dictionary<Mods, double>(),
            CatchStarRating = dbBeatmap.CatchStarRating ?? new Dictionary<Mods, double>(),
            ManiaStarRating = dbBeatmap.ManiaStarRating ?? new Dictionary<Mods, double>(),
            DrainTime = dbBeatmap.DrainTime,
            TotalTime = dbBeatmap.TotalTime,
            AudioPreviewTime = dbBeatmap.AudioPreviewTime,
            BeatmapId = dbBeatmap.BeatmapId,
            BeatmapSetId = dbBeatmap.BeatmapSetId,
            ThreadId = dbBeatmap.ThreadId,
            StandardGrade = dbBeatmap.StandardGrade,
            TaikoGrade = dbBeatmap.TaikoGrade,
            CatchGrade = dbBeatmap.CatchGrade,
            ManiaGrade = dbBeatmap.ManiaGrade,
            LocalOffset = dbBeatmap.LocalOffset,
            StackLeniency = dbBeatmap.StackLeniency,
            Ruleset = dbBeatmap.Ruleset,
            Source = dbBeatmap.Source,
            Tags = dbBeatmap.Tags,
            OnlineOffset = dbBeatmap.OnlineOffset,
            TitleFont = dbBeatmap.TitleFont,
            IsUnplayed = dbBeatmap.IsUnplayed,
            LastPlayed = dbBeatmap.LastPlayed,
            IsOsz2 = dbBeatmap.IsOsz2,
            FolderName = dbBeatmap.FolderName,
            LastCheckedAgainstOsuRepo = dbBeatmap.LastCheckedAgainstOsuRepo,
            IgnoreBeatmapSound = dbBeatmap.IgnoreBeatmapSound,
            IgnoreBeatmapSkin = dbBeatmap.IgnoreBeatmapSkin,
            DisableStoryboard = dbBeatmap.DisableStoryboard,
            DisableVideo = dbBeatmap.DisableVideo,
            VisualOverride = dbBeatmap.VisualOverride,
            ManiaScrollSpeed = dbBeatmap.ManiaScrollSpeed
        };

        return beatmap;
    }

}
