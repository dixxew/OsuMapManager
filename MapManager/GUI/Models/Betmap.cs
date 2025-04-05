using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using osu_database_reader.Components.Player;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapManager.GUI.Models
{
    public class Beatmap : ReactiveObject
    {
        public int BeatmapId { get; set; }
        public int BeatmapSetId { get; set; }
        public string Name => $"{Artist} - {Title}";

        private int _bytesOfBeatmapEntry;
        public int BytesOfBeatmapEntry
        {
            get => _bytesOfBeatmapEntry;
            set => this.RaiseAndSetIfChanged(ref _bytesOfBeatmapEntry, value);
        }

        private string _artist;
        public string Artist
        {
            get => _artist;
            set
            {
                this.RaiseAndSetIfChanged(ref _artist, value);
                this.RaisePropertyChanged(nameof(Name));
            }
        }

        private string _artistUnicode;
        public string ArtistUnicode
        {
            get => _artistUnicode;
            set => this.RaiseAndSetIfChanged(ref _artistUnicode, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                this.RaiseAndSetIfChanged(ref _title, value);
                this.RaisePropertyChanged(nameof(Name));
            }
        }

        private string _titleUnicode;
        public string TitleUnicode
        {
            get => _titleUnicode;
            set => this.RaiseAndSetIfChanged(ref _titleUnicode, value);
        }

        private string _creator;
        public string Creator
        {
            get => _creator;
            set => this.RaiseAndSetIfChanged(ref _creator, value);
        }

        private string _difficulty;
        public string Difficulty
        {
            get => _difficulty;
            set => this.RaiseAndSetIfChanged(ref _difficulty, value);
        }

        private string _audioFileName;
        public string AudioFileName
        {
            get => _audioFileName;
            set => this.RaiseAndSetIfChanged(ref _audioFileName, value);
        }

        private string _md5Hash;
        public string MD5Hash
        {
            get => _md5Hash;
            set => this.RaiseAndSetIfChanged(ref _md5Hash, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private SubmissionStatus _rankedStatus;
        public SubmissionStatus RankedStatus
        {
            get => _rankedStatus;
            set => this.RaiseAndSetIfChanged(ref _rankedStatus, value);
        }

        private ushort _circlesCount;
        public ushort CirclesCount
        {
            get => _circlesCount;
            set => this.RaiseAndSetIfChanged(ref _circlesCount, value);
        }

        private ushort _slidersCount;
        public ushort SlidersCount
        {
            get => _slidersCount;
            set => this.RaiseAndSetIfChanged(ref _slidersCount, value);
        }

        private ushort _spinnersCount;
        public ushort SpinnersCount
        {
            get => _spinnersCount;
            set => this.RaiseAndSetIfChanged(ref _spinnersCount, value);
        }

        private DateTime _lastModifiedTime;
        public DateTime LastModifiedTime
        {
            get => _lastModifiedTime;
            set => this.RaiseAndSetIfChanged(ref _lastModifiedTime, value);
        }

        private float _approachRate;
        public float ApproachRate
        {
            get => _approachRate;
            set => this.RaiseAndSetIfChanged(ref _approachRate, value);
        }

        private float _circleSize;
        public float CircleSize
        {
            get => _circleSize;
            set => this.RaiseAndSetIfChanged(ref _circleSize, value);
        }

        private float _hpDrain;
        public float HPDrain
        {
            get => _hpDrain;
            set => this.RaiseAndSetIfChanged(ref _hpDrain, value);
        }

        private float _overallDifficulty;
        public float OverallDifficulty
        {
            get => _overallDifficulty;
            set => this.RaiseAndSetIfChanged(ref _overallDifficulty, value);
        }

        private double? _standardStarRating;
        public double? StandardStarRating
        {
            get => _standardStarRating;
            set => this.RaiseAndSetIfChanged(ref _standardStarRating, value);
        }

        private List<Score> _scores;
        public List<MapManager.GUI.Models.Score> Scores
        {
            get => _scores;
            set => this.RaiseAndSetIfChanged(ref _scores, value);
        }

        private bool _isUnplayed;
        public bool IsUnplayed
        {
            get => _isUnplayed;
            set =>
                this.RaiseAndSetIfChanged(ref _isUnplayed, value);
        }

        public TimeSpan TotalTime;


        public string Duration { get; set; }

        public int ObjectsCount => CirclesCount + SlidersCount + SpinnersCount;

        public List<string> TagsList { get; set; }

        public string FolderName { get; set; }


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
                TagsList = dbBeatmap.SongTags.Split(' ').ToList(),
                Duration = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime).ToString(@"m\:ss"),
                TotalTime = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime),
                FolderName = dbBeatmap.FolderName,
                Scores = new List<Score>() // Пустой список на случай отсутствия скорингов
            };
        }

        public static void AddReplays(Beatmap beatmap, List<Replay> scores)
        {
            beatmap.Scores = scores.Select((s, i) => new Score(
                 i + 1,
                 s.OsuVersion,           // OsuVersion
                 beatmap.MD5Hash,        // BeatmapMD5Hash (берём MD5Hash из текущего Beatmap)
                 s.PlayerName,           // PlayerName
                 s.BeatmapHash,        // ReplayMD5Hash
                 s.Count300,             // Count300
                 s.Count100,             // Count100
                 s.Count50,              // Count50
                 s.CountGeki,            // CountGeki
                 s.CountKatu,            // CountKatu
                 s.CountMiss,            // CountMiss
                 s.Score,          // ReplayScore
                 s.Combo,                // Combo
                 s.FullCombo,         // PerfectCombo
                 (int)s.Mods,                 // Mods
                 s.TimePlayed,       // ScoreTimestamp
                 s.ScoreId               // ScoreId
             )).ToList();
        }
    }
}
