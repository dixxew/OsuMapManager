using Avalonia.Controls;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using NAudio.Wave;
using ReactiveUI;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

namespace MapManager.GUI.ViewModels;

public class AudioPlayerViewModel : ReactiveObject
{
    private readonly AudioPlayerService _audioPlayerService;
    private bool _isPlaying;
    private bool _isLoopEnabled;
    private bool _isRandomEnabled;
    private bool _isFavorite;
    private string _selectedPlaybackRate = "1";
    private Timer _progressTimer;




    public AudioPlayerViewModel(AudioPlayerService audioPlayerService)
    {
        _audioPlayerService = audioPlayerService;
        _audioPlayerService.OnSongChanged += OnSongChanged;
        _audioPlayerService.OnSongProgressChanged += OnSongProgressChanged;

        _progressTimer = new Timer(200);
        _progressTimer.Elapsed += (s, e) => SongProgress = _audioPlayerService.SongProgress;
    }




    private bool _isPopupOpen;
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isPopupOpen, value);
    }

    public string SelectedPlaybackRate
    {
        get => _selectedPlaybackRate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPlaybackRate, value);
            _audioPlayerService.SetPlaybackRate(float.Parse(value, CultureInfo.InvariantCulture));
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
    }

    public string FormattedSongProgress => TimeSpan.FromSeconds(SongProgress).ToString(@"m\:ss");
    public string FormattedSongDuration => TimeSpan.FromSeconds(SongDuration).ToString(@"m\:ss");

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isPlaying, value);
            if (_isPlaying) _progressTimer.Start();
            else _progressTimer.Stop();
        }
    }

    public bool IsLoopEnabled
    {
        get => _isLoopEnabled;
        set => this.RaiseAndSetIfChanged(ref _isLoopEnabled, value);
    }

    public bool IsRandomEnabled
    {
        get => _isRandomEnabled;
        set => this.RaiseAndSetIfChanged(ref _isRandomEnabled, value);
    }

    private double _songProgress;
    public double SongProgress
    {
        get => _songProgress;
        set
        {
            this.RaiseAndSetIfChanged(ref _songProgress, value);
            this.RaisePropertyChanged(nameof(FormattedSongProgress));
        }
    }

    private double _songDuration;
    public double SongDuration
    {
        get => _songDuration;
        private set
        {
            this.RaiseAndSetIfChanged(ref _songDuration, value);
            this.RaisePropertyChanged(nameof(FormattedSongDuration));
        }
    }


    private string _popupTime;
    public string PopupTime
    {
        get => _popupTime;
        set => this.RaiseAndSetIfChanged(ref _popupTime, value);
    }

    private double _hoveredPosition;
    public double HoveredPosition
    {
        get => _hoveredPosition;
        set => this.RaiseAndSetIfChanged(ref _hoveredPosition, value);
    }




    public void PlayPauseCommand()
    {
        if (IsPlaying)
        {
            _audioPlayerService.Pause();
            IsPlaying = false;
        }
        else
        {
            _audioPlayerService.Resume();
            IsPlaying = true;
        }
    }

    public void StopCommand()
    {
        _audioPlayerService.Stop();
        IsPlaying = false;
        SongProgress = 0;
    }

    public void NextCommand() => _audioPlayerService.PlayNext();
    public void PrevCommand() => _audioPlayerService.PlayPrev();
    public void ToggleRandom()
    {
        _audioPlayerService.IsRandomEnabled = !_audioPlayerService.IsRandomEnabled;
        IsRandomEnabled = !IsRandomEnabled;
    }
    public void ToggleRepeat()
    {
        _audioPlayerService.IsRepeatEnabled = !_audioPlayerService.IsRepeatEnabled;
        IsLoopEnabled = !IsLoopEnabled;
    }
    public void ToggleFavorite()
    {
        _audioPlayerService.IsFavorite = !_audioPlayerService.IsFavorite;
        IsFavorite = !IsFavorite;
    }

    public void ClosePopup()
    {
        IsPopupOpen = false;
    }
    public void SetSongPosition(double positionInSeconds)
    {
        throw new NotImplementedException();
    }
    public void UpdatePopupState(double relativePosition, double progressBarWidth, double progressBarMinimum, double progressBarMaximum)
    {
        HoveredPosition = progressBarMinimum + relativePosition * (progressBarMaximum - progressBarMinimum);
        PopupTime = TimeSpan.FromSeconds(HoveredPosition).ToString(@"m\:ss");
    }





    private void OnSongProgressChanged(double songProgress)
    {
        SongProgress = songProgress;
    }

    private void OnSongChanged(bool isFavorite, double songDuration, bool isPlaying)
    {
        IsFavorite = isFavorite;
        SongDuration = songDuration;
        IsPlaying = isPlaying;
    }
}
