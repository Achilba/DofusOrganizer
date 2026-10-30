using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

public class AlmanaxDay
{
    public string DayName { get; set; }
    public string DateText { get; set; }
    public string Resource { get; set; }
    public string QtyText { get; set; }
    /// <summary>Type de l'offrande, par exemple « Équipement · Anneau » ou « Consommable · Pain ».</summary>
    public string TypeText { get; set; }
    /// <summary>« Ressource », « Équipement » ou « Consommable » (sert à colorer la pastille).</summary>
    public string TypeCategory { get; set; }
    public bool HasType => !string.IsNullOrEmpty(TypeText);
    public bool HasQuest { get; set; }
    public string QuestText { get; set; }
    public string Bonus { get; set; }
    public bool IsToday { get; set; }
    public Brush Bg { get; set; }
    public Brush Border { get; set; }
    public string ChipText { get; set; }
    public bool HasChip => !string.IsNullOrEmpty(ChipText);
    public Brush ChipBrush { get; set; }
    public bool ShowPrep { get; set; }
    public bool IsPrep { get; set; }
    public string PrepLabel { get; set; }
    public bool ShowDone { get; set; }
    public bool ShowUndo { get; set; }
    public RelayCommand PrepCommand { get; set; }
    public RelayCommand DoneCommand { get; set; }
    public RelayCommand UndoCommand { get; set; }
    public RelayCommand CopyCommand { get; set; }
    public RelayCommand QuestCommand { get; set; }
}

public class AlmanaxCell
{
    public string Num { get; set; }
    public Brush Bg { get; set; }
    public Brush Border { get; set; }
    public Brush Fg { get; set; }
    public string Text { get; set; }
    public double Opacity { get; set; }
    public System.Windows.Thickness Ring { get; set; }
}

public class AlmanaxWeekRow
{
    public string Label { get; set; }
    public Brush Ring { get; set; }
    public List<AlmanaxCell> Cells { get; set; }
    public RelayCommand SelectCommand { get; set; }
}

public class AlmanaxViewModel : ObservableObject
{
    private static readonly string[] Months = { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre" };
    private static readonly string[] DayNames = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    private readonly MainViewModel _main;
    private DateTime _week;
    private int _month;
    private bool _monthView;

    public ObservableCollection<AlmanaxDay> Days { get; } = new();
    public ObservableCollection<AlmanaxDay> Upcoming { get; } = new();
    private bool _upcoming;
    public RelayCommand ToggleUpcomingCommand { get; }
    public RelayCommand CopyUpcomingCommand { get; }
    public bool IsUpcomingView => _upcoming;
    public string UpcomingButtonText => _upcoming ? "Retour à la semaine" : "7 prochains jours";
    public ObservableCollection<AlmanaxWeekRow> MonthRows { get; } = new();

    public RelayCommand PrevCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand TodayCommand { get; }
    public RelayCommand ToggleViewCommand { get; }
    public RelayCommand PrevMonthCommand { get; }
    public RelayCommand NextMonthCommand { get; }
    public RelayCommand AllTodoCommand { get; }
    public RelayCommand AllPrepCommand { get; }
    public RelayCommand AllDoneCommand { get; }
    /// <summary>« Fait » de la carte de l'accueil : marque l'Almanax d'aujourd'hui comme réalisé.</summary>
    public RelayCommand TodayDoneCommand { get; }
    public RelayCommand ZaapCommand => _main.CopyAlmanaxZaapCommand;

    public AlmanaxViewModel(MainViewModel main)
    {
        _main = main;
        _week = MondayOf(DateTime.Today);
        _month = DateTime.Today.Month - 1;

        PrevCommand = new RelayCommand(() => Move(-1));
        NextCommand = new RelayCommand(() => Move(+1));
        TodayCommand = new RelayCommand(() => { _week = MondayOf(DateTime.Today); _monthView = false; _upcoming = false; Refresh(); });
        ToggleUpcomingCommand = new RelayCommand(() => { _upcoming = !_upcoming; _monthView = false; Refresh(); });
        CopyUpcomingCommand = new RelayCommand(CopyUpcoming);
        ToggleViewCommand = new RelayCommand(() => { _upcoming = false; _monthView = !_monthView; if (_monthView) _month = _week.AddDays(3).Month - 1; Refresh(); });
        PrevMonthCommand = new RelayCommand(() => { _month = Math.Max(0, _month - 1); Refresh(); });
        NextMonthCommand = new RelayCommand(() => { _month = Math.Min(11, _month + 1); Refresh(); });
        AllTodoCommand = new RelayCommand(() => ApplyWeek(d => true, "todo"));
        AllPrepCommand = new RelayCommand(() => ApplyWeek(d => d >= DateTime.Today && Kind(d) != "done", "prep"));
        AllDoneCommand = new RelayCommand(() => ApplyWeek(d => d <= DateTime.Today, "done"));
        TodayDoneCommand = new RelayCommand(() => { SetDay(DateTime.Today, "done"); _main.RequestSave(); Refresh(); });
        Refresh();
    }

    public bool IsMonthView => _monthView;
    public bool IsWeekView => !_monthView && !_upcoming;
    public string ViewButtonText => _monthView ? "Retour à la semaine" : "Calendrier";
    public string WeekLabel { get; private set; }
    public string MonthTitle { get; private set; }
    public string WeekCounter { get; private set; }
    public string YearCounter { get; private set; }
    public string WeekShort { get; private set; }
    public string YearLabel => "Année " + DateTime.Today.Year;
    public double WeekPct { get; private set; }
    public double YearPct { get; private set; }
    public string[] WeekdayHeaders { get; } = { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" };

    /// <summary>Rappel (carte de l'accueil et point orange de la mini-barre) : tant que l'Almanax du jour n'est pas réalisé, même s'il est déjà préparé.</summary>
    public bool NeedsReminder
    {
        get
        {
            var k = Key(DateTime.Today);
            return !_main.Data.AlmanaxDone.Contains(k);
        }
    }

    /// <summary>Texte de l'offrande du jour pour l'Accueil.</summary>
    public string TodaySummary
    {
        get
        {
            var o = Offer(DateTime.Today);
            return o.Resource == "—" ? "Almanax du jour indisponible" : o.Resource + "  ×" + o.Qty;
        }
    }

    public bool IsSunday => DateTime.Today.DayOfWeek == DayOfWeek.Sunday;

    public void Reload() { _week = MondayOf(DateTime.Today); Refresh(); }

    public void ShowNextWeek()
    {
        _week = MondayOf(DateTime.Today).AddDays(7);
        _monthView = false;
        _upcoming = false;
        Refresh();
    }

    // ───── logique ─────

    private static DateTime MondayOf(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
    private static string Key(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static (string Resource, int Qty, string Quest) Offer(DateTime d)
        => AlmanaxData.Offers.TryGetValue(d.ToString("dd/MM", CultureInfo.InvariantCulture), out var o) ? o : ("—", 0, "");

    private string Kind(DateTime d)
    {
        var k = Key(d);
        if (_main.Data.AlmanaxDone.Contains(k)) return "done";
        if (d.Date < DateTime.Today) return "missed";
        return _main.Data.AlmanaxPrepared.Contains(k) ? "prep" : "todo";
    }

    private void SetDay(DateTime d, string kind)
    {
        var k = Key(d);
        _main.Data.AlmanaxDone.Remove(k);
        _main.Data.AlmanaxPrepared.Remove(k);
        if (kind == "done") _main.Data.AlmanaxDone.Add(k);
        else if (kind == "prep") _main.Data.AlmanaxPrepared.Add(k);
    }

    private void ApplyWeek(Func<DateTime, bool> pick, string kind)
    {
        for (int i = 0; i < 7; i++)
        {
            var d = _week.AddDays(i);
            if (pick(d)) SetDay(d, kind);
        }
        _main.RequestSave();
        Refresh();
    }

    private DateTime MinWeek => MondayOf(new DateTime(DateTime.Today.Year, 1, 1));
    private DateTime MaxWeek => MondayOf(new DateTime(DateTime.Today.Year, 12, 31));
    public bool CanPrev => _week > MinWeek;
    public bool CanNext => _week < MaxWeek;
    public bool CanPrevMonth => _month > 0;
    public bool CanNextMonth => _month < 11;

    private void Move(int dir)
    {
        var w = _week.AddDays(7 * dir);
        if (w < MinWeek || w > MaxWeek) return;
        _week = w;
        Refresh();
    }

    private static Brush B(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private (Brush bg, Brush border, Brush fg, string chip) Look(string kind) => kind switch
    {
        "done" => (B("#243DD68C"), B("#3DD68C"), B("#7BE8B0"), "Validé"),
        "prep" => (B("#24F2963C"), B("#F2963C"), B("#FFB366"), "Préparée"),
        "missed" => (B("#1FFF6B5F"), B("#FF6B5F"), B("#FF9A90"), "Manqué"),
        _ => (B("#161A21"), B("#2B313C"), B("#B4BAC6"), "À préparer")
    };

    /// <summary>Construit la ligne d'un jour (semaine et liste des 7 prochains jours).</summary>
    private AlmanaxDay MakeDay(DateTime d)
    {
        var kind = Kind(d);
        var look = Look(kind);
        var o = Offer(d);
        var date = d;
        var bonus = (d.Year == 2026 && AlmanaxData.Bonus2026.TryGetValue(d.ToString("dd/MM", CultureInfo.InvariantCulture), out var b))
            ? b : "Modificateur indisponible pour cette date.";
        bool isPrep = kind == "prep";

        return new AlmanaxDay
        {
            DayName = DayNames[((int)d.DayOfWeek + 6) % 7],
            DateText = d.Day + " " + Months[d.Month - 1],
            Resource = o.Resource,
            QtyText = o.Qty > 0 ? "×" + o.Qty : "",
            TypeText = AlmanaxTypes.For(o.Resource),
            TypeCategory = AlmanaxTypes.CategoryOf(AlmanaxTypes.For(o.Resource)),
            HasQuest = !string.IsNullOrEmpty(o.Quest),
            QuestText = "Quête : " + o.Quest + " ↗",
            Bonus = bonus,
            IsToday = d.Date == DateTime.Today,
            Bg = look.bg,
            Border = look.border,
            ChipText = look.chip,
            ChipBrush = look.fg,
            ShowPrep = d >= DateTime.Today && kind != "done",
            PrepLabel = isPrep ? "Annuler" : "Préparer la ressource",
            IsPrep = isPrep,
            ShowDone = (d.Date == DateTime.Today || kind == "missed") && kind != "done",
            ShowUndo = kind == "done",
            PrepCommand = new RelayCommand(() => { SetDay(date, isPrep ? "todo" : "prep"); _main.RequestSave(); Refresh(); }),
            DoneCommand = new RelayCommand(() => { SetDay(date, "done"); _main.RequestSave(); Refresh(); }),
            UndoCommand = new RelayCommand(() => { SetDay(date, "todo"); _main.RequestSave(); Refresh(); }),
            CopyCommand = new RelayCommand(() => _main.CopyWithToast(o.Resource, "Copié  " + o.Resource)),
            QuestCommand = new RelayCommand(() => OpenQuest(date))
        };
    }

    public void Refresh()
    {
        var year = DateTime.Today.Year;
        var s = _week;
        var e = _week.AddDays(6);
        bool same = s.Month == e.Month;
        WeekLabel = same
            ? "Semaine du lundi " + s.Day + " au dimanche " + e.Day + " " + Months[e.Month - 1]
            : "Semaine du lundi " + s.Day + " " + Months[s.Month - 1] + " au dimanche " + e.Day + " " + Months[e.Month - 1];
        MonthTitle = char.ToUpper(Months[_month][0]) + Months[_month].Substring(1) + " " + year;

        // ── 7 jours de la semaine ──
        Days.Clear();
        int weekDone = 0;
        for (int i = 0; i < 7; i++)
        {
            var d = _week.AddDays(i);
            if (Kind(d) == "done") weekDone++;
            Days.Add(MakeDay(d));
        }

        Upcoming.Clear();
        for (int i = 0; i < 7; i++) Upcoming.Add(MakeDay(DateTime.Today.AddDays(i)));

        int yearDone = _main.Data.AlmanaxDone.Count(k => k.StartsWith(year + "-"));
        int yearDays = DateTime.IsLeapYear(year) ? 366 : 365;
        WeekCounter = weekDone + "/7";
        YearCounter = yearDone + "/" + yearDays;
        WeekPct = 100.0 * weekDone / 7;
        YearPct = 100.0 * yearDone / yearDays;
        WeekShort = same
            ? s.Day + " – " + e.Day + " " + Months[e.Month - 1]
            : s.Day + " " + Months[s.Month - 1] + " – " + e.Day + " " + Months[e.Month - 1];

        // ── vue mensuelle ──
        MonthRows.Clear();
        var first = new DateTime(year, _month + 1, 1);
        var last = first.AddMonths(1).AddDays(-1);
        for (var rs = MondayOf(first); rs <= last; rs = rs.AddDays(7))
        {
            var cells = new List<AlmanaxCell>();
            for (int j = 0; j < 7; j++)
            {
                var t = rs.AddDays(j);
                var l = Look(Kind(t));
                cells.Add(new AlmanaxCell
                {
                    Num = t.Day.ToString(), Bg = l.bg, Fg = l.fg, Text = l.chip,
                    Border = t.Date == DateTime.Today ? B("#F2B544") : l.border,
                    Opacity = t.Month == _month + 1 ? 1 : 0.4,
                    Ring = new System.Windows.Thickness(t.Date == DateTime.Today ? 2.5 : 1.5)
                });
            }
            var rowStart = rs;
            MonthRows.Add(new AlmanaxWeekRow
            {
                Label = "Semaine du " + rs.Day + " au " + rs.AddDays(6).Day + " " + Months[rs.AddDays(6).Month - 1],
                Ring = rs == _week ? B("#F2B544") : Brushes.Transparent,
                Cells = cells,
                SelectCommand = new RelayCommand(() => { _week = rowStart; _monthView = false; Refresh(); })
            });
        }

        foreach (var p in new[] { nameof(IsMonthView), nameof(IsWeekView), nameof(IsUpcomingView), nameof(UpcomingButtonText), nameof(ViewButtonText), nameof(WeekLabel), nameof(MonthTitle),
                                  nameof(WeekCounter), nameof(YearCounter), nameof(TodaySummary), nameof(IsSunday),
                                  nameof(WeekShort), nameof(WeekPct), nameof(YearPct), nameof(NeedsReminder), nameof(YearLabel),
                                  nameof(CanPrev), nameof(CanNext), nameof(CanPrevMonth), nameof(CanNextMonth) })
            Raise(p);
    }

    /// <summary>Copie la liste des 7 prochaines offrandes (pratique pour préparer les ressources).</summary>
    private void CopyUpcoming()
    {
        var lines = Upcoming.Select(u => u.DayName + " " + u.DateText + " : " + u.Resource + (string.IsNullOrEmpty(u.QtyText) ? "" : " " + u.QtyText));
        _main.CopyWithToast(string.Join("\n", lines), "Liste copiée");
    }

    private void OpenQuest(DateTime d)
    {
        var url = d.ToString("MMdd", CultureInfo.InvariantCulture) == "1003"
            ? "https://www.dofuspourlesnoobs.com/la-fete-aux-tire-fesses.html"
            : "https://www.dofuspourlesnoobs.com/calendrier-de-lalmanax.html";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}
