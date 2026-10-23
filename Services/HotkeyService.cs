using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Services;

/// <summary>
/// Raccourcis via RegisterHotKey. Seul « Afficher / masquer » (Ctrl+Alt+O par défaut) est global ;
/// les autres ne sont enregistrés que lorsque DOFUS est au premier plan.
/// Les raccourcis sont modifiables dans Paramètres (voir ShortcutDefs).
/// </summary>
public class HotkeyService : IDisposable
{
    public const int ToggleId = 1, InvitesId = 10, NextId = 11, NextAltId = 12, PrevId = 13, MainCharId = 14;

    private readonly HwndSource _source;
    private bool _contextual;
    private bool _swapKeys;
    private Func<string, ShortcutSpec> _get = ShortcutDefs.Default;
    private int _hookVk; // touche captée par le hook clavier (F12 uniquement), 0 si aucune

    public event Action<int> Pressed;

    // F12 est réservée par Windows (débogueur) : RegisterHotKey la refuse.
    // On la capte donc avec un hook clavier, uniquement quand DOFUS est au premier plan.
    private IntPtr _hook = IntPtr.Zero;
    private Native.LowLevelKeyboardProc _hookProc;

    public HotkeyService()
    {
        var p = new HwndSourceParameters("DofusTeamOrganizerHotkeys")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE : fenêtre invisible dédiée aux messages
            WindowStyle = 0
        };
        _source = new HwndSource(p);
        _source.AddHook(Hook);
        RegisterToggle();

        _hookProc = KeyboardHook;
        _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _hookProc, Native.GetModuleHandle(null), 0);
    }

    /// <summary>Applique les raccourcis choisis (appelé au démarrage et après chaque modification).</summary>
    public void Apply(Func<string, ShortcutSpec> get)
    {
        _get = get ?? ShortcutDefs.Default;
        Native.UnregisterHotKey(_source.Handle, ToggleId);
        RegisterToggle();

        // Force le ré-enregistrement des raccourcis contextuels
        var active = _contextual;
        var swap = _swapKeys;
        _contextual = !active;
        SetContextual(active, swap);
    }

    private void RegisterToggle()
    {
        var t = _get(ShortcutDefs.Toggle);
        Native.RegisterHotKey(_source.Handle, ToggleId, (uint)t.Mods | Native.MOD_NOREPEAT, (uint)t.Vk);
    }

    private IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _contextual && _hookVk != 0 && wParam == (IntPtr)Native.WM_KEYDOWN)
        {
            int vk = Marshal.ReadInt32(lParam);
            if (vk == _hookVk)
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() => Pressed?.Invoke(InvitesId)));
                return (IntPtr)1; // touche consommée
            }
        }
        return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_HOTKEY)
        {
            Pressed?.Invoke(wParam.ToInt32());
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void Reg(int id, ShortcutSpec s)
    {
        if (s == null || s.Vk == 0) return;
        Native.RegisterHotKey(_source.Handle, id, (uint)s.Mods | Native.MOD_NOREPEAT, (uint)s.Vk);
    }

    /// <summary>Active ou désactive les raccourcis qui ne doivent fonctionner que dans DOFUS.</summary>
    public void SetContextual(bool dofusActive, bool swapEnabled)
    {
        if (dofusActive == _contextual && swapEnabled == _swapKeys) return;
        Unregister();
        _contextual = dofusActive;
        _swapKeys = swapEnabled;
        _hookVk = 0;
        if (!dofusActive) return;

        Reg(MainCharId, _get(ShortcutDefs.Main));

        // « Copier les invitations » : F12 seule passe par le hook clavier, les autres touches par RegisterHotKey
        var inv = _get(ShortcutDefs.Invites);
        if (inv.Mods == 0 && inv.Vk == 0x7B) _hookVk = 0x7B;
        else Reg(InvitesId, inv);

        if (swapEnabled)
        {
            var next = _get(ShortcutDefs.Next);
            Reg(NextId, next);
            // Le * existe en deux versions (touche AZERTY et pavé numérique) : on enregistre les deux
            if (next.Mods == 0 && next.Vk == 0xDC) Reg(NextAltId, new ShortcutSpec(0, 0x6A));
            else if (next.Mods == 0 && next.Vk == 0x6A) Reg(NextAltId, new ShortcutSpec(0, 0xDC));
            Reg(PrevId, _get(ShortcutDefs.Prev));
        }
    }

    private void Unregister()
    {
        var h = _source.Handle;
        foreach (var id in new[] { MainCharId, InvitesId, NextId, NextAltId, PrevId })
            Native.UnregisterHotKey(h, id);
    }

    public void Dispose()
    {
        Unregister();
        Native.UnregisterHotKey(_source.Handle, ToggleId);
        if (_hook != IntPtr.Zero) { Native.UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
        _source.Dispose();
    }
}
