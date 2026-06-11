using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using osu_database_reader.Components.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapManager.GUI.Models
{
    public class Beatmap
    {
        public int BeatmapId { get; set; }
        public int BeatmapSetId { get; set; }
        public string Name => $"{Artist} - {Title}";

        public int BytesOfBeatmapEntry { get; set; }
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Creator { get; set; }
        public string Difficulty { get; set; }
        public string AudioFileName { get; set; }
        public string MD5Hash { get; set; }
        public string FileName { get; set; }
        public SubmissionStatus RankedStatus { get; set; }
        public ushort CirclesCount { get; set; }
        public ushort SlidersCount { get; set; }
        public ushort SpinnersCount { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public float ApproachRate { get; set; }
        public float CircleSize { get; set; }
        public float HPDrain { get; set; }
        public float OverallDifficulty { get; set; }
        public double? StandardStarRating { get; set; }
        public List<Score> Scores { get; set; }
        public bool IsUnplayed { get; set; }
        public TimeSpan TotalTime { get; set; }
        public string Duration { get; set; }
        public int ObjectsCount => CirclesCount + SlidersCount + SpinnersCount;
        public string FolderName { get; set; }

        public List<string> TagsList { get; set; }

        public static Beatmap FromBeatmapEntry(BeatmapEntry dbBeatmap)
        {
            return new Beatmap
            {
                BeatmapId = dbBeatmap.BeatmapId,
                BeatmapSetId = dbBeatmap.BeatmapSetId,
                Artist = dbBeatmap.Artist,
                ArtistUnicode = dbBeatmap.ArtistUnicode,
                Title = dbBeatmap.Title,
                TitleUnicode = dbBeatmap.TitleUnicode,
                Difficulty = dbBeatmap.Version,
                Creator = dbBeatmap.Creator,
                AudioFileName = dbBeatmap.AudioFileName,
                MD5Hash = dbBeatmap.BeatmapChecksum,
                FileName = dbBeatmap.BeatmapFileName,
                RankedStatus = dbBeatmap.RankedStatus,
                CirclesCount = dbBeatmap.CountHitCircles,
                SlidersCount = dbBeatmap.CountSliders,
                SpinnersCount = dbBeatmap.CountSpinners,
                LastModifiedTime = dbBeatmap.LastModifiedTime,
                ApproachRate = dbBeatmap.ApproachRate,
                CircleSize = dbBeatmap.CircleSize,
                HPDrain = dbBeatmap.HPDrainRate,
                IsUnplayed = dbBeatmap.Unplayed,
                OverallDifficulty = dbBeatmap.OveralDifficulty,
                StandardStarRating = dbBeatmap.DiffStarRatingStandard.TryGetValue(osu.Shared.Mods.None, out double stars) ? stars : 0,
                // string.Intern: теги массово повторяются между картами, интернирование дедуплицирует строки
                TagsList = dbBeatmap.SongTags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(string.Intern).ToList(),
                Duration = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime).ToString(@"m\:ss"),
                TotalTime = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime),
                FolderName = dbBeatmap.FolderName,
                Scores = new List<Score>()
            };
        }

        public static void AddReplays(Beatmap beatmap, List<Replay> scores)
        {
            beatmap.Scores = scores.Select((s, i) => new Score(
                i + 1,
                s.OsuVersion,
                beatmap.MD5Hash,
                s.PlayerName,
                s.BeatmapHash,
                s.Count300,
                s.Count100,
                s.Count50,
                s.CountGeki,
                s.CountKatu,
                s.CountMiss,
                s.Score,
                s.Combo,
                s.FullCombo,
                (int)s.Mods,
                s.TimePlayed,
                s.ScoreId
            )).ToList();
        }
    }
}
