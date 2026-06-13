using MapManager.GUI.Models;
using ReactiveUI;
using System;
using System.Reactive;

namespace MapManager.GUI.ViewModels;

public class OsuPpsFiltersViewModel : ViewModelBase
{
    public event Action? FiltersChanged;

    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    public OsuPpsFiltersViewModel()
    {
        ResetCommand = ReactiveCommand.Create(Reset);
    }

    // ── Mods toggles ─────────────────────────────────────────────────────────

    private bool _modNm;  public bool ModNm  { get => _modNm;  set { this.RaiseAndSetIfChanged(ref _modNm,  value); FiltersChanged?.Invoke(); } }
    private bool _modHd;  public bool ModHd  { get => _modHd;  set { this.RaiseAndSetIfChanged(ref _modHd,  value); FiltersChanged?.Invoke(); } }
    private bool _modHr;  public bool ModHr  { get => _modHr;  set { this.RaiseAndSetIfChanged(ref _modHr,  value); FiltersChanged?.Invoke(); } }
    private bool _modDt;  public bool ModDt  { get => _modDt;  set { this.RaiseAndSetIfChanged(ref _modDt,  value); FiltersChanged?.Invoke(); } }
    private bool _modHdDt; public bool ModHdDt { get => _modHdDt; set { this.RaiseAndSetIfChanged(ref _modHdDt, value); FiltersChanged?.Invoke(); } }
    private bool _modHt;  public bool ModHt  { get => _modHt;  set { this.RaiseAndSetIfChanged(ref _modHt,  value); FiltersChanged?.Invoke(); } }
    private bool _modEz;  public bool ModEz  { get => _modEz;  set { this.RaiseAndSetIfChanged(ref _modEz,  value); FiltersChanged?.Invoke(); } }
    private bool _modFl;  public bool ModFl  { get => _modFl;  set { this.RaiseAndSetIfChanged(ref _modFl,  value); FiltersChanged?.Invoke(); } }

    // ── PP range ─────────────────────────────────────────────────────────────

    private double _minPp = 0;
    public double MinPp
    {
        get => _minPp;
        set { this.RaiseAndSetIfChanged(ref _minPp, value); FiltersChanged?.Invoke(); }
    }

    private double _maxPp = 2000;
    public double MaxPp
    {
        get => _maxPp;
        set { this.RaiseAndSetIfChanged(ref _maxPp, value); FiltersChanged?.Invoke(); }
    }

    // ── Length range (seconds) ────────────────────────────────────────────────

    private int _minLength = 0;
    public int MinLength
    {
        get => _minLength;
        set
        {
            this.RaiseAndSetIfChanged(ref _minLength, value);
            this.RaisePropertyChanged(nameof(MinLengthLabel));
            FiltersChanged?.Invoke();
        }
    }

    private int _maxLength = 900;
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxLength, value);
            this.RaisePropertyChanged(nameof(MaxLengthLabel));
            FiltersChanged?.Invoke();
        }
    }

    public string MinLengthLabel => FormatSeconds(_minLength);
    public string MaxLengthLabel => FormatSeconds(_maxLength);

    private static string FormatSeconds(int s) => $"{s / 60}:{s % 60:D2}";

    // ── Filter logic ──────────────────────────────────────────────────────────

    public bool Passes(OsuPpsEntry e)
    {
        bool anyMod = ModNm || ModHd || ModHr || ModDt || ModHdDt || ModHt || ModEz || ModFl;
        if (anyMod)
        {
            bool ok = (ModNm   && e.Mods == 0)    ||
                      (ModHd   && e.Mods == 8)    ||
                      (ModHr   && e.Mods == 16)   ||
                      (ModDt   && e.Mods == 64)   ||
                      (ModHdDt && e.Mods == 72)   ||
                      (ModHt   && e.Mods == 256)  ||
                      (ModEz   && e.Mods == 2)    ||
                      (ModFl   && e.Mods == 1024);
            if (!ok) return false;
        }

        if (e.Pp99 < _minPp || e.Pp99 > _maxPp) return false;
        if (e.LengthSeconds < _minLength || e.LengthSeconds > _maxLength) return false;

        return true;
    }

    public void Reset()
    {
        ModNm = ModHd = ModHr = ModDt = ModHdDt = ModHt = ModEz = ModFl = false;
        _minPp = 0;     this.RaisePropertyChanged(nameof(MinPp));
        _maxPp = 2000;  this.RaisePropertyChanged(nameof(MaxPp));
        _minLength = 0;   this.RaisePropertyChanged(nameof(MinLength)); this.RaisePropertyChanged(nameof(MinLengthLabel));
        _maxLength = 900; this.RaisePropertyChanged(nameof(MaxLength)); this.RaisePropertyChanged(nameof(MaxLengthLabel));
        FiltersChanged?.Invoke();
    }
}
