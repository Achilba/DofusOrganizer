using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DofusTeamOrganizer.Services;

/// <summary>
/// Raccourcis via RegisterHotKey. Seul Ctrl+Alt+O est global ;
/// les autres ne sont enregistrés que lorsque DOFUS est au premier plan.
/// </summary>
public class HotkeyService : IDisposable
{
    public const int ToggleId = 1, InvitesId = 10, NextId = 11, NextAltId = 12, PrevId = 13, MainCharId = 14;

    private readonly HwndSource _source;
    private bool _contextual;
    private bool _swapKeys;

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
        // Ctrl+Alt+O (global)
        Native.RegisterHotKey(_source.Handle, ToggleId, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, 0x4F);

        _hookProc = KeyboardHook;
        _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _hookProc, Native.GetModuleHandle(null), 0);
    }

    private IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _contextual && wParam == (IntPtr)Native.WM_KEYDOWN)
        {
            int vk = Marshal.ReadInt32(lParam);
            if (vk == 0x7B) // F12
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

    /// <summary>Active ou désactive les raccourcis qui ne doivent fonctionner que dans DOFUS.</summary>
    public void SetContextual(bool dofusActive, bool swapEnabled)
    {
        if (dofusActive == _contextual && swapEnabled == _swapKeys) return;
        Unregister();
        _contextual = dofusActive;
        _swapKeys = swapEnabled;
        if (!dofusActive) return;

        var h = _source.Handle;
        Native.RegisterHotKey(h, MainCharId, Native.MOD_NOREPEAT, 0x70);  // F1
        if (swapEnabled)
        {
            Native.RegisterHotKey(h, NextId, Native.MOD_NOREPEAT, 0x6A);    // * pavé numérique
            Native.RegisterHotKey(h, NextAltId, Native.MOD_NOREPEAT, 0xDC); // touche * (AZERTY)
            Native.RegisterHotKey(h, PrevId, Native.MOD_NOREPEAT, 0x75);    // F6
        }
    }

    private void Unregister()
    {
        var h = _source.Handle;
        foreach (var id in new[] { MainCharId, NextId, NextAltId, PrevId })
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
