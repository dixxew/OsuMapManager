using Avalonia.Controls;
using NAudio.Wave;
using ReactiveUI;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;

namespace MapManager.GUI.ViewModels;

public class AudioPlayerViewModel : ReactiveObject
{
    private IWavePlayer _wavePlayer;
    private AudioFileReader _audioFileReader;
    private SoundTouchWaveProvider _soundTouchProvider;
    private bool _isPlaying;
    private Timer _progressTimer;
    private readonly HashSet<string> _favorites = new();
    private bool _isPopupOpen;
    private string _popupTime;

    private bool _isLoopEnabled;
    private bool _isRandomEnabled;
    private float _playbackRate = 1.0f; // Скорость воспроизведения

    public AudioPlayerViewModel()
    {
        _progressTimer = new Timer(200);
        _progressTimer.Elapsed += UpdateProgress;
    }

    private string _selectedPlaybackRate = "1";

    public string SelectedPlaybackRate
    {
        get => _selectedPlaybackRate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPlaybackRate, value);
            PlaybackRate = float.Parse(value, CultureInfo.InvariantCulture);
            }
    }

    public bool IsFavorite => _favorites.Contains(AudioFilePath);

    public string FormattedSongProgress => TimeSpan.FromSeconds(SongProgress).ToString(@"m\:ss");
    public string FormattedSongDuration => TimeSpan.FromSeconds(SongDuration).ToString(@"m\:ss");

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isPlaying, value);
            if (_isPlaying)
                _progressTimer.Start();
            else
                _progressTimer.Stop();
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

    public float PlaybackRate
    {
        get => _playbackRate;
        set
        {
            if (value >= 0.5f && value <= 2.0f) // Ограничиваем диапазон скорости
            {
                this.RaiseAndSetIfChanged(ref _playbackRate, value);
                SetPlaybackRate();
            }
        }
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

    private string _audioFilePath;
    public string AudioFilePath
    {
        get => _audioFilePath;
        set => this.RaiseAndSetIfChanged(ref _audioFilePath, value);
    }

    private float _volume = 0f;
    public float Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            if (_wavePlayer != null)
                _wavePlayer.Volume = Volume;
        }
    }
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isPopupOpen, value);
    }

    public string PopupTime
    {
        get => _popupTime;
        set => this.RaiseAndSetIfChanged(ref _popupTime, value);
    }


    public void SetSongPosition(double positionInSeconds)
    {
        if (_audioFileReader != null && positionInSeconds >= 0 && positionInSeconds <= SongDuration)
        {
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(positionInSeconds);
            SongProgress = positionInSeconds;
        }
    }

    public void SetSongAndPlay(string audioPath)
    {
        StopCommand();

        AudioFilePath = audioPath;

        if (AudioFilePath.Contains(".ogg"))
            return;

        _wavePlayer = new WaveOutEvent();
        _audioFileReader = new AudioFileReader(audioPath);

        // Создаём SoundTouchWaveProvider для изменения скорости
        _soundTouchProvider = new SoundTouchWaveProvider(_audioFileReader.ToWaveProvider());
        _soundTouchProvider.Tempo = PlaybackRate; // Устанавливаем начальную скорость

        _wavePlayer.Init(_soundTouchProvider);
        _wavePlayer.Volume = Volume;
        _wavePlayer.Play();

        SongDuration = _audioFileReader.TotalTime.TotalSeconds;

        IsPlaying = true;
        this.RaisePropertyChanged(nameof(IsFavorite));
    }

    public void PlayPauseCommand()
    {
        if (!_isPlaying)
        {
            _wavePlayer?.Play();
            IsPlaying = true;
        }
        else
        {
            _wavePlayer?.Pause();
            IsPlaying = false;
        }
    }

    public void StopCommand()
    {
        _wavePlayer?.Stop();
        _audioFileReader?.Dispose();
        _audioFileReader = null;
        _wavePlayer?.Dispose();
        _wavePlayer = null;
        SongProgress = 0;
        IsPlaying = false;
    }

    public void NextCommand()
    {
        if (IsRandomEnabled)
        {
            AppStore.MainWindowVM.SelectRandomBeatmapSet();
        }
        else
        {
            AppStore.MainWindowVM.SelectNextBeatmapSet();
        }
    }

    public void PrevCommand()
    {
        AppStore.MainWindowVM.SelectPrevBeatmapSet();
    }

    private void SetPlaybackRate()
    {
        if (_soundTouchProvider != null)
        {
            _soundTouchProvider.Tempo = PlaybackRate;
        }
    }

    private void UpdateProgress(object? sender, ElapsedEventArgs e)
    {
        if (_audioFileReader != null && _wavePlayer?.PlaybackState == PlaybackState.Playing)
        {
            // Обновляем прогресс песни
            SongProgress = _audioFileReader.CurrentTime.TotalSeconds;

            // Проверяем, если песня достигла конца
            if (SongProgress >= SongDuration - 0.1) // Допустимый допуск для точности
            {
                // Останавливаем текущий трек, если ещё не остановлен
                _wavePlayer.Stop();

                if (IsLoopEnabled)
                {
                    // Повтор текущей песни
                    SetSongAndPlay(AudioFilePath);
                }
                else
                {
                    // Переход к следующей песне
                    NextCommand();
                }
            }
        }
    }


    public void ToggleFavorite()
    {
        if (!string.IsNullOrEmpty(AudioFilePath))
        {
            if (_favorites.Contains(AudioFilePath))
            {
                _favorites.Remove(AudioFilePath);
                this.RaisePropertyChanged(nameof(IsFavorite));
            }
            else
            {
                _favorites.Add(AudioFilePath);
                this.RaisePropertyChanged(nameof(IsFavorite));
            }
        }
    }

}
