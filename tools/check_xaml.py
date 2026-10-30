#!/usr/bin/env python3
"""Contrôle rapide des fichiers XAML avant la compilation.

Détecte ce que le compilateur ne voit pas et qui fait planter l'appli au lancement :
  1. une ressource utilisée avec {StaticResource Nom} qui n'est définie nulle part ;
  2. un gestionnaire d'évènement (Click="...", Drop="...") absent du fichier .xaml.cs ;
  3. un fichier XAML mal formé.
Code de sortie 1 s'il y a un problème.
"""
import glob, os, re, sys
import xml.dom.minidom as minidom

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EVENTS = {
    "Click", "Checked", "Unchecked", "Loaded", "Unloaded", "TextChanged", "SelectionChanged", "MouseLeftButtonDown",
    "MouseLeftButtonUp", "MouseDown", "MouseUp", "MouseMove", "PreviewMouseLeftButtonDown", "PreviewMouseLeftButtonUp",
    "PreviewMouseMove", "PreviewMouseDown", "PreviewKeyDown", "KeyDown", "KeyUp", "DragOver", "DragEnter", "DragLeave",
    "Drop", "SizeChanged", "IsVisibleChanged", "GotFocus", "LostFocus", "ValueChanged", "MouseWheel", "PreviewMouseWheel",
    "MouseDoubleClick", "Closing", "Closed", "StateChanged", "ContextMenuOpening", "MouseEnter", "MouseLeave",
}

def read(p):
    with open(p, encoding="utf-8") as f:
        return f.read()

files = sorted(glob.glob(os.path.join(ROOT, "App.xaml")) + glob.glob(os.path.join(ROOT, "Views", "*.xaml")))
errors = []
texts = {}
for p in files:
    t = read(p)
    texts[p] = t
    try:
        minidom.parseString(t.encode("utf-8"))
    except Exception as e:
        errors.append(f"{os.path.relpath(p, ROOT)} : XAML mal formé ({e})")

key_re = re.compile(r'x:Key\s*=\s*"([^"]+)"')
use_re = re.compile(r'\{StaticResource\s+([^\s}{]+)\s*\}')

# Ressources globales : App.xaml + dictionnaires qu'il charge
global_keys = set()
app = texts.get(os.path.join(ROOT, "App.xaml"), "")
global_keys |= set(key_re.findall(app))
for src in re.findall(r'Source\s*=\s*"([^"]+\.xaml)"', app):
    dp = os.path.join(ROOT, src.replace("/", os.sep).lstrip("\\/"))
    if os.path.exists(dp):
        global_keys |= set(key_re.findall(read(dp)))
for p in glob.glob(os.path.join(ROOT, "**", "*.xaml"), recursive=True):
    if os.path.dirname(p) != os.path.join(ROOT, "Views") and os.path.basename(p) != "App.xaml":
        if os.path.relpath(p, ROOT).split(os.sep)[0] in ("obj", "bin", "publish"):
            continue
        global_keys |= set(key_re.findall(read(p)))

for p, t in texts.items():
    rel = os.path.relpath(p, ROOT)
    local = set(key_re.findall(t))
    known = global_keys | local
    for m in use_re.finditer(t):
        name = m.group(1)
        if name.startswith("x:") or name in known:
            continue
        line = t.count("\n", 0, m.start()) + 1
        errors.append(f"{rel}:{line} : ressource introuvable « {name} » (à définir dans App.xaml ou dans la page)")

    cs = p + ".cs"
    if os.path.exists(cs):
        code = read(cs)
        for m in re.finditer(r'\b(\w+)\s*=\s*"([A-Za-z_]\w*)"', t):
            attr, val = m.group(1), m.group(2)
            if attr in EVENTS and not re.search(r'\b' + re.escape(val) + r'\s*\(', code):
                line = t.count("\n", 0, m.start()) + 1
                errors.append(f"{rel}:{line} : gestionnaire « {val} » ({attr}) absent de {os.path.basename(cs)}")

if errors:
    print("Contrôle XAML : %d problème(s)" % len(errors))
    for e in errors:
        print("  -", e)
    sys.exit(1)
print("Contrôle XAML : OK (%d fichiers)" % len(files))
