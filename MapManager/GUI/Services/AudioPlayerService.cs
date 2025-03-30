using MapManager.GUI.Models;
using NAudio.Wave;
using OsuSharp.Domain;
using ReactiveUI;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MapManager.GUI.Services;
public class AudioPlayerService
{
    private readonly SettingsService _settingsService;
    private readonly BeatmapDataService _beatmapDataService;
    private Timer _progressTimer;

    public AudioPlayerService(SettingsService settingsService, BeatmapDataService beatmapDataService)
    {
        _settingsService = settingsService;
        _beatmapDataService = beatmapDataService;

        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;


        _progressTimer = new Timer(200);
        _progressTimer.Elapsed += (s, e) => SongProgressChanged(SongProgress);
    }


    private IWavePlayer _wavePlayer;
    private WaveStream _waveStream;
    private SoundTouchWaveProvider _soundTouchProvider;
    private float _playbackRate = 1.0f;
    private float _volume = 0.05f;
    private Stack<BeatmapSet> RandomBeatmapsHistory = new();
    private bool _isRandomEnabled = false;
    private bool _isFavorite;

    public double SongProgress => _waveStream?.CurrentTime.TotalSeconds ?? 0;
    public double SongDuration => _waveStream?.TotalTime.TotalSeconds ?? 0;
    public bool HasFile => _waveStream != null;
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            if (_wavePlayer != null)
                _wavePlayer.Volume = value;
        }
    }
    public bool IsRandomEnabled
    {
        get => _isRandomEnabled;
        set
        {
            _isRandomEnabled = value;
            if (!value)
                RandomBeatmapsHistory.Clear();
        }
    }
    public bool IsRepeatEnabled;
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            _isFavorite = value;
            SetCurrentSongFavorite(value);
        }
    }


    public void SetSongAndPlay(BeatmapSet beatmapSet, int selectedBeatmapId)
    {
        var audioFilePath = Path.Combine(_settingsService.OsuDirPath, "Songs", beatmapSet.FolderName,
            beatmapSet.Beatmaps.First(b => b.BeatmapId == selectedBeatmapId).AudioFileName);

        Play(audioFilePath);
        _isFavorite = beatmapSet.IsFavorite;
        SongChanged(beatmapSet.IsFavorite, SongDuration, true);
    }
    public void Play(string filePath)
    {
        Stop();
        _progressTimer.Start();
        _wavePlayer = new WaveOutEvent();

        IWaveProvider waveProvider;
        if (filePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            _waveStream = new NAudio.Vorbis.VorbisWaveReader(filePath);
        else
            _waveStream = new AudioFileReader(filePath);


        _soundTouchProvider = new SoundTouchWaveProvider(_waveStream)
        {
            Tempo = _playbackRate
        };

        _wavePlayer.Init(_soundTouchProvider);
        _wavePlayer.Play();
        _wavePlayer.PlaybackStopped += PlaybackStopped;
    }




    public void Pause() => _wavePlayer?.Pause();
    public void Resume() => _wavePlayer?.Play();
    public void Stop()
    {
        if (_wavePlayer != null)
        {
            _wavePlayer.PlaybackStopped -= PlaybackStopped;
            _wavePlayer.Stop();
            _wavePlayer.Dispose();
            _wavePlayer = null;
        }
        _progressTimer.Stop();
        _wavePlayer?.Stop();
        _waveStream?.Dispose();
        _waveStream = null;
        _wavePlayer?.Dispose();
        _wavePlayer = null;
    }
    public void SetSongProgress(double positionInSeconds)
    {
        _waveStream.CurrentTime = TimeSpan.FromSeconds(positionInSeconds);
    }
    public void SetPlaybackRate(float rate)
    {
        if (rate < 0.5f || rate > 2.0f) return;
        _playbackRate = rate;
        if (_soundTouchProvider != null)
        {
            _soundTouchProvider.Tempo = _playbackRate;
        }
    }
    public void PlayPrev()
    {
        if (!IsRandomEnabled || RandomBeatmapsHistory.Count == 0)
            _beatmapDataService.SelectPrevBeatmapSet();
        else
        {
            _beatmapDataService.SelectBeatmapSetAndBeatmap(RandomBeatmapsHistory.Pop());
        }
    }
    public void PlayNext()
    {
        if (!IsRandomEnabled)
            _beatmapDataService.SelectNextBeatmapSet();
        else
        {
            RandomBeatmapsHistory.Push(_beatmapDataService.SelectedBeatmapSet);
            _beatmapDataService.SelectRandomBeatmapSet();
        }

    }



    private void SetCurrentSongFavorite(bool value)
    {
        _beatmapDataService.ToggleSelectedBeatmapSetFavorite(value);
    }
    private void PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is null)
            if (IsRepeatEnabled)
                Repeat();
            else
                PlayNext();
    }
    private void Repeat()
    {
        _waveStream.Position = 0;
        _wavePlayer.Play();
    }

    private void OnSelectedBeatmapChanged()
    {
        SetSongAndPlay(_beatmapDataService.SelectedBeatmapSet, _beatmapDataService.SelectedBeatmap.BeatmapId);
    }
    public event Action<bool, double, bool> OnSongChanged;
    private void SongChanged(bool isFavorite, double songDuration, bool isPlaying)
    {
        OnSongChanged?.Invoke(isFavorite, songDuration, isPlaying);
    }
    public event Action<double> OnSongProgressChanged;

    private void SongProgressChanged(double songProgress)
    {
        OnSongProgressChanged?.Invoke(songProgress);
    }
}
