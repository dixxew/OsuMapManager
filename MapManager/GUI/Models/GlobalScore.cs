using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace MapManager.GUI.Models;

public class GlobalScore : ReactiveObject
{
    private long _id;
    public long Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private long? _bestId;
    public long? BestId
    {
        get => _bestId;
        set => this.RaiseAndSetIfChanged(ref _bestId, value);
    }

    private long _userId;
    public long UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    private double _accuracy;
    public double Accuracy
    {
        get => _accuracy;
        set => this.RaiseAndSetIfChanged(ref _accuracy, value);
    }

    private IReadOnlyList<string> _mods;
    public IReadOnlyList<string> Mods
    {
        get => _mods;
        set {
        this.RaiseAndSetIfChanged(ref _mods, value);
            this.RaisePropertyChanged(nameof(ModsString));

        }
    }

    public string ModsString => string.Join(" ", Mods);


    private long _totalScore;
    public long TotalScore
    {
        get => _totalScore;
        set => this.RaiseAndSetIfChanged(ref _totalScore, value);
    }

    private int _maxCombo;
    public int MaxCombo
    {
        get => _maxCombo;
        set => this.RaiseAndSetIfChanged(ref _maxCombo, value);
    }

    private GlobalScoreStatistics _statistics;
    public GlobalScoreStatistics Statistics
    {
        get => _statistics;
        set => this.RaiseAndSetIfChanged(ref _statistics, value);
    }

    private double? _performancePoints;
    public double? PerformancePoints
    {
        get => _performancePoints;
        set => this.RaiseAndSetIfChanged(ref _performancePoints, value);
    }

    private string _rank;
    public string Rank
    {
        get => _rank;
        set => this.RaiseAndSetIfChanged(ref _rank, value);
    }

    private long? _globalRank;
    public long? GlobalRank
    {
        get => _globalRank;
        set => this.RaiseAndSetIfChanged(ref _globalRank, value);
    }

    private GlobalUserCompact _user;
    public GlobalUserCompact User
    {
        get => _user;
        set => this.RaiseAndSetIfChanged(ref _user, value);
    }
}
public class GlobalScoreStatistics : ReactiveObject
{
    private int _count50;
    public int Count50
    {
        get => _count50;
        set => this.RaiseAndSetIfChanged(ref _count50, value);
    }

    private int _count100;
    public int Count100
    {
        get => _count100;
        set => this.RaiseAndSetIfChanged(ref _count100, value);
    }

    private int _count300;
    public int Count300
    {
        get => _count300;
        set => this.RaiseAndSetIfChanged(ref _count300, value);
    }

    private int _countGeki;
    public int CountGeki
    {
        get => _countGeki;
        set => this.RaiseAndSetIfChanged(ref _countGeki, value);
    }

    private int _countKatu;
    public int CountKatu
    {
        get => _countKatu;
        set => this.RaiseAndSetIfChanged(ref _countKatu, value);
    }

    private int _countMiss;
    public int CountMiss
    {
        get => _countMiss;
        set => this.RaiseAndSetIfChanged(ref _countMiss, value);
    }
}
public class GlobalUserCompact : ReactiveObject
{
    private Uri _avatarUrl;
    public Uri AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _avatarUrl, value);
            LoadAvatarAsync();
        }
    }

    private string _countryCode;
    public string CountryCode
    {
        get => _countryCode;
        set => this.RaiseAndSetIfChanged(ref _countryCode, value);
    }

    private string _defaultGroup;
    public string DefaultGroup
    {
        get => _defaultGroup;
        set => this.RaiseAndSetIfChanged(ref _defaultGroup, value);
    }

    private long _id;
    public long Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    private bool _isBot;
    public bool IsBot
    {
        get => _isBot;
        set => this.RaiseAndSetIfChanged(ref _isBot, value);
    }

    private bool _isOnline;
    public bool IsOnline
    {
        get => _isOnline;
        set => this.RaiseAndSetIfChanged(ref _isOnline, value);
    }

    private bool _isSupporter;
    public bool IsSupporter
    {
        get => _isSupporter;
        set => this.RaiseAndSetIfChanged(ref _isSupporter, value);
    }

    private DateTimeOffset? _lastVisit;
    public DateTimeOffset? LastVisit
    {
        get => _lastVisit;
        set => this.RaiseAndSetIfChanged(ref _lastVisit, value);
    }

    private bool _pmFriendsOnly;
    public bool PmFriendsOnly
    {
        get => _pmFriendsOnly;
        set => this.RaiseAndSetIfChanged(ref _pmFriendsOnly, value);
    }

    private string _profileColour;
    public string ProfileColour
    {
        get => _profileColour;
        set => this.RaiseAndSetIfChanged(ref _profileColour, value);
    }

    private string _username;
    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    private Bitmap _avatar;

    public Bitmap Avatar
    {
        get => _avatar;
        set => this.RaiseAndSetIfChanged(ref _avatar, value);
    }
    private async void LoadAvatarAsync()
    {
        if (_avatarUrl == null)
        {
            Avatar = null;
            return;
        }
        var _httpClient = new HttpClient();
        try
        {
            // Загрузка данных аватарки
            using var response = await _httpClient.GetAsync(_avatarUrl);
            response.EnsureSuccessStatusCode();

            // Чтение данных как поток
            using var stream = await response.Content.ReadAsStreamAsync();

            // Создание Bitmap из потока
            Avatar = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Avatar = null; // Сбрасываем аватарку в случае ошибки
        }
    }

}
