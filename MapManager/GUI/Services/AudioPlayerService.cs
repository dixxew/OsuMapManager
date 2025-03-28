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
    private IWavePlayer _wavePlayer;
    private AudioFileReader _audioFileReader;
    private SoundTouchWaveProvider _soundTouchProvider;
    private float _playbackRate = 1.0f;
    private readonly SettingsService _settingsService;

    public double SongProgress => _audioFileReader?.CurrentTime.TotalSeconds ?? 0;
    public double SongDuration => _audioFileReader?.TotalTime.TotalSeconds ?? 0;
    public bool HasFile => _audioFileReader != null;

    public bool IsRandomEnabled;
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
        throw new NotImplementedException();
    }

    public void PlayNext()
    {
        throw new NotImplementedException();
    }
}
