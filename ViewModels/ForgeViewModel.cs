using System.Collections.ObjectModel;
using System.Globalization;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.Services;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une des trois cases de rune (simple, Pa, Ra) d'une ligne : points apportés et pourcentage affiché.</summary>
public class ForgeRuneCell : ObservableObject
{
    private bool _visible;
    private string _label = "";
    private string _text = "";
    private string _tone = "";

    public bool Visible { get => _visible; set => Set(ref _visible, value); }
    public string Label { get => _label; set => Set(ref _label, value); }
    public string Text { get => _text; set => Set(ref _text, value); }
    /// <summary>good, warn, bad ou vide : sert à colorer le pourcentage.</summary>
    public string Tone { get => _tone; set => Set(ref _tone, value); }
}

/// <summary>Valeurs numériques lues d'une ligne (null si une saisie est invalide).</summary>
public record ForgeInput(double Value, double Min, double Max, double Weight);

/// <summary>Une ligne de l'objet : caractéristique, valeur, jets min et max, et les pourcentages de ses runes.</summary>
public class ForgeLineRow : ObservableObject
{
    private static readonly double[] Mult = { 1, 3, 10 };
    private readonly Action _changed;
    private ForgePreset _preset;
    private string _value = "", _min = "", _max = "", _weight = "";

    public ForgeLineRow(ForgePreset preset, Action changed, Action<ForgeLineRow> remove)
    {
        _changed = changed;
        _preset = preset;
        _weight = Fmt(preset.Weight);
        RemoveCommand = new RelayCommand(() => remove(this));
    }

    public RelayCommand RemoveCommand { get; }
    public ForgeRuneCell Rune1 { get; } = new();
    public ForgeRuneCell Rune2 { get; } = new();
    public ForgeRuneCell Rune3 { get; } = new();

    public ForgePreset Preset
    {
        get => _preset;
        set
        {
            if (value == null || value == _preset) return;
            _preset = value;
            _weight = Fmt(value.Weight);
            Raise();
            Raise(nameof(WeightText));
            _changed();
        }
    }

    public string ValueText { get => _value; set { if (Set(ref _value, value ?? "")) _changed(); } }
    public string MinText { get => _min; set { if (Set(ref _min, value ?? "")) _changed(); } }
    public string MaxText { get => _max; set { if (Set(ref _max, value ?? "")) _changed(); } }
    public string WeightText { get => _weight; set { if (Set(ref _weight, value ?? "")) _changed(); } }

    public static string Fmt(double d) => d.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',');

    public static bool TryNum(string s, out double d)
        => double.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out d);

    public ForgeInput Read()
    {
        if (!TryNum(_value, out var v) || !TryNum(_min, out var mn) || !TryNum(_max, out var mx) || !TryNum(_weight, out var w)) return null;
        if (w <= 0 || mx < mn) return null;
        return new ForgeInput(v, mn, mx, w);
    }

    /// <summary>Remplit les champs sans déclencher de recalcul (chargement).</summary>
    public void Load(ForgeLineData d)
    {
        _value = d.Value.HasValue ? Fmt(d.Value.Value) : "";
        _min = d.Min.HasValue ? Fmt(d.Min.Value) : "";
        _max = d.Max.HasValue ? Fmt(d.Max.Value) : "";
        _weight = Fmt(d.Weight);
        Raise(nameof(ValueText)); Raise(nameof(MinText)); Raise(nameof(MaxText)); Raise(nameof(WeightText));
    }

    public ForgeLineData ToData()
    {
        double? N(string s) => TryNum(s, out var d) ? d : null;
        return new ForgeLineData { Stat = _preset.Name, Value = N(_value), Min = N(_min), Max = N(_max), Weight = TryNum(_weight, out var w) ? w : _preset.Weight };
    }

    /// <summary>Calcule les trois cases pour le mode choisi (0 = SC, 1 = SN, 2 = EC).</summary>
    public void Compute(ForgeInput inp, double quality, int level, int mode)
    {
        var cells = new[] { Rune1, Rune2, Rune3 };
        var res = new ForgeResult?[3];
        int best = -1;
        for (int k = 0; k < 3; k++)
        {
            var c = cells[k];
            c.Visible = k < _preset.Sizes;
            if (!c.Visible) continue;
            double pts = _preset.Base * Mult[k];
            c.Label = "+" + Fmt(pts);
            if (inp == null) { c.Text = "—"; c.Tone = ""; continue; }
            double runeWeight = pts * inp.Weight;
            res[k] = ForgeCalc.Chances(Math.Max(0, inp.Value) * inp.Weight, runeWeight, inp.Value + pts, inp.Min, inp.Max, inp.Weight, quality, level);
            if (best < 0 || res[k].Value.Sc > res[best].Value.Sc) best = k;
        }
        for (int k = 0; k < 3; k++)
        {
            if (!cells[k].Visible || res[k] == null) continue;
            var r = res[k].Value;
            double shown = mode == 0 ? r.Sc : mode == 1 ? r.Sn : r.Ec;
            string txt = shown < 10 && shown != Math.Floor(shown)
                ? shown.ToString("0.0", CultureInfo.GetCultureInfo("fr-FR")) : Math.Floor(shown + 0.5).ToString("0", CultureInfo.InvariantCulture);
            // ★ : la rune qui donne le plus de succès critiques sur cette ligne (seulement si elle apporte vraiment quelque chose)
            bool star = k == best && r.Sc > 2.05 && mode == 0;
            cells[k].Text = (star ? "★ " : "") + txt + " %";
            cells[k].Tone = mode == 0 ? (shown >= 30 ? "good" : shown >= 10 ? "warn" : "bad")
                          : mode == 1 ? (shown >= 40 ? "good" : shown >= 25 ? "warn" : "bad")
                          : (shown <= 30 ? "good" : shown <= 60 ? "warn" : "bad");
        }
    }
}

/// <summary>Page Forgemagie : on saisit les lignes de l'objet, la page donne les chances de chaque rune.</summary>
public class ForgeViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private bool _loading;
    private string _levelText = "200";
    private string _quality = "";

    public ObservableCollection<ForgeLineRow> Lines { get; } = new();
    public IReadOnlyList<ForgePreset> Presets => ForgeData.Presets;
    public string[] Modes { get; } = { "Succès critique", "Succès neutre", "Échec critique" };

    public ForgeViewModel(MainViewModel main)
    {
        _main = main;
        AddLineCommand = new RelayCommand(() => AddLine(ForgeData.Presets[0], true));
        ClearCommand = new RelayCommand(() => { Lines.Clear(); Recalc(); Raise(nameof(HasNoLine)); });
        Reload();
    }

    public RelayCommand AddLineCommand { get; }
    public RelayCommand ClearCommand { get; }
    public bool HasNoLine => Lines.Count == 0;
    public string QualityText { get => _quality; private set => Set(ref _quality, value); }

    public string LevelText
    {
        get => _levelText;
        set
        {
            if (!Set(ref _levelText, value ?? "")) return;
            if (ForgeLineRow.TryNum(_levelText, out var d)) { _main.Data.ForgeLevel = (int)d; _main.RequestSave(); }
            Recalc();
        }
    }

    public string Mode
    {
        get => Modes[Math.Clamp(_main.Data.ForgeMode, 0, 2)];
        set
        {
            int i = Array.IndexOf(Modes, value);
            if (i < 0 || i == _main.Data.ForgeMode) return;
            _main.Data.ForgeMode = i;
            _main.RequestSave();
            Raise();
            Recalc(false);
        }
    }

    /// <summary>Au démarrage et après un import ou une réinitialisation.</summary>
    public void Reload()
    {
        _loading = true;
        Lines.Clear();
        _levelText = _main.Data.ForgeLevel.ToString(CultureInfo.InvariantCulture);
        foreach (var d in _main.Data.ForgeLines)
        {
            var row = NewRow(ForgeData.Find(d.Stat));
            row.Load(d);
            Lines.Add(row);
        }
        _loading = false;
        Raise(nameof(LevelText));
        Raise(nameof(Mode));
        Raise(nameof(HasNoLine));
        Recalc(false);
    }

    private ForgeLineRow NewRow(ForgePreset p) => new(p, () => Recalc(), RemoveLine);

    private void AddLine(ForgePreset p, bool recalc)
    {
        Lines.Add(NewRow(p));
        Raise(nameof(HasNoLine));
        if (recalc) Recalc();
    }

    private void RemoveLine(ForgeLineRow row)
    {
        Lines.Remove(row);
        Raise(nameof(HasNoLine));
        Recalc();
    }

    private void Recalc(bool save = true)
    {
        if (_loading) return;
        int level = ForgeLineRow.TryNum(_levelText, out var lv) ? (int)lv : 200;
        var inputs = Lines.Select(l => l.Read()).ToList();
        double total = 0, maxSum = 0;
        foreach (var i in inputs)
        {
            if (i == null) continue;
            total += Math.Max(0, i.Value) * i.Weight;
            maxSum += Math.Max(0, i.Max) * i.Weight;
        }
        double q = maxSum > 0 ? total / maxSum : 0;
        QualityText = maxSum > 0 ? "Qualité de l'objet : " + Math.Round(q * 100).ToString("0", CultureInfo.InvariantCulture) + " %" : "";
        int mode = Math.Clamp(_main.Data.ForgeMode, 0, 2);
        for (int k = 0; k < Lines.Count; k++) Lines[k].Compute(inputs[k], q, level, mode);
        if (save)
        {
            _main.Data.ForgeLines = Lines.Select(l => l.ToData()).ToList();
            _main.RequestSave();
        }
    }
}
