using MapManager.GUI.Models;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class AudioPlayerService
{
    private readonly SettingsService _settingsService;
    private readonly BeatmapDataService _beatmapDataService;

    public AudioPlayerService(SettingsService settingsService, BeatmapDataService beatmapDataService)
    {
        _settingsService = settingsService;
        _beatmapDataService = beatmapDataService;

        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;
    }


    private IWavePlayer _wavePlayer;
    private AudioFileReader _audioFileReader;
    private SoundTouchWaveProvider _soundTouchProvider;
    private float _playbackRate = 1.0f;
    private float _volume = 0.05f;
    private Stack<BeatmapSet> RandomBeatmapsHistory = new();
    private bool _isRandomEnabled = false;

    public double SongProgress => _audioFileReader?.CurrentTime.TotalSeconds ?? 0;
    public double SongDuration => _audioFileReader?.TotalTime.TotalSeconds ?? 0;
    public bool HasFile => _audioFileReader != null;
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
    public bool IsFavorite;



    public void SetSongAndPlay(BeatmapSet beatmapSet, int selectedBeatmapId)
    {
        var audioFilePath = Path.Combine(_settingsService.OsuDirPath, "Songs", beatmapSet.FolderName,
            beatmapSet.Beatmaps.First(b => b.BeatmapId == selectedBeatmapId).AudioFileName);
        SongChanged(beatmapSet.IsFavorite, SongDuration, true);
        if (!audioFilePath.Contains(".ogg"))
        {
            Play(audioFilePath);
        }
    }
    public void Play(string filePath)
    {
        Stop();

        _wavePlayer = new WaveOutEvent();
        _audioFileReader = new AudioFileReader(filePath);
        _soundTouchProvider = new SoundTouchWaveProvider(_audioFileReader.ToWaveProvider())
        {
            Tempo = _playbackRate
        };

        _wavePlayer.Init(_soundTouchProvider);
        _wavePlayer.Play();
    }
    public void Pause() => _wavePlayer?.Pause();
    public void Resume() => _wavePlayer?.Play();
    public void Stop()
    {
        _wavePlayer?.Stop();
        _audioFileReader?.Dispose();
        _audioFileReader = null;
        _wavePlayer?.Dispose();
        _wavePlayer = null;
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
            _beatmapDataService.SelectBeatmapSet(RandomBeatmapsHistory.Pop());
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
