using OsuParsers.Database.Objects;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;
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

        private RankedStatus _rankedStatus;
        public RankedStatus RankedStatus
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

        private  double? _standardStarRating;
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

        public TimeSpan TotalTime;


        public string Duration { get; set; }

        public int ObjectsCount => CirclesCount + SlidersCount + SpinnersCount;

        public List<string> TagsList { get; set; }

        public string FolderName { get; set; }


        // Метод создания Beatmap из DbBeatmap
        public static Beatmap FromDbBeatmap(DbBeatmap dbBeatmap)
        {
            return new Beatmap
            {
                BeatmapId = dbBeatmap.BeatmapId,
                BeatmapSetId = dbBeatmap.BeatmapSetId,
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
                StandardStarRating = dbBeatmap.StandardStarRating.TryGetValue(Mods.None, out double stars) ? stars : 0,
                TagsList = dbBeatmap.Tags.Split(' ').ToList(),
                Duration = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime).ToString(@"m\:ss"),
                TotalTime = TimeSpan.FromMilliseconds(dbBeatmap.TotalTime),
                FolderName = dbBeatmap.FolderName,
                Scores = new List<Score>() // Пустой список на случай отсутствия скорингов
            };
        }

        public static void AddScores(Beatmap beatmap, List<OsuParsers.Database.Objects.Score> scores)
        {
            beatmap.Scores = scores.Select(s => new Score(
                 s.Ruleset,               // Ruleset
                 s.OsuVersion,           // OsuVersion
                 beatmap.MD5Hash,        // BeatmapMD5Hash (берём MD5Hash из текущего Beatmap)
                 s.PlayerName,           // PlayerName
                 s.ReplayMD5Hash,        // ReplayMD5Hash
                 s.Count300,             // Count300
                 s.Count100,             // Count100
                 s.Count50,              // Count50
                 s.CountGeki,            // CountGeki
                 s.CountKatu,            // CountKatu
                 s.CountMiss,            // CountMiss
                 s.ReplayScore,          // ReplayScore
                 s.Combo,                // Combo
                 s.PerfectCombo,         // PerfectCombo
                 s.Mods,                 // Mods
                 s.ScoreTimestamp,       // ScoreTimestamp
                 s.ScoreId               // ScoreId
             )).ToList();
        }
    }
}
