# NORMAX

**v1.1.0 changes:** fixed video panel centering/proportion on scaled monitors, added
position (X/Y) fine-tune sliders in Settings, hardened Settings against crashes,
made the video background immune to Win+D and Alt+F4, and locked the main window
to a fixed size.


Turn a folder of videos into a continuously playing, cinematic desktop background —
a centered video panel (default 70% of the screen) surrounded by pure black,
sitting behind your icons and windows like a real desktop wallpaper.

Built with **.NET 8 / WPF**, **LibVLCSharp** (hardware-accelerated playback, broad
codec support) and the standard **WorkerW** technique for desktop-layer integration.
100% offline, no account required.

---

## ⚠️ Important — read this first

This project's source code was written and organized in full, but it was **not
compiled or tested on an actual Windows machine** by the assistant that wrote it —
that assistant runs in a Linux-only sandbox with no Windows/.NET build tools
available. To get you a real, working `.exe`, this repo includes a **GitHub Actions
workflow** that compiles the app on a genuine Windows machine (a GitHub-hosted
runner) every time you push it. You don't need to install anything yourself.

**Because of that, treat the first build as a beta**: go through the checklist in
[Known risk areas](#known-risk-areas-please-test-these) below once you have it
installed, and report back anything that misbehaves — most of it will be small
fixes (a missing using-statement, a NuGet version bump, etc.), not a redesign.

---

## Getting your NORMAX_Setup.exe (no dev tools needed)

1. Create a new **empty** repository on [github.com](https://github.com) (any name, e.g. `normax`). Public or private both work.
2. On the new repo's page, choose **"uploading an existing file"** and drag in every file/folder from this project (keep the folder structure: `src/`, `installer/`, `.github/`, etc.), then commit.
3. Go to the **Actions** tab of your repo. A workflow called **"Build NORMAX"** will already be running (it triggers automatically on push). Wait ~3–5 minutes.
4. Click the finished run → scroll to **Artifacts** → download **`NORMAX-installer`**. Unzip it — inside is `NORMAX_Setup.exe`.
5. Copy `NORMAX_Setup.exe` to your Windows 11 PC and run it.

If you'd rather have a **portable, no-install** version, download the
**`NORMAX-portable`** artifact instead and run `NORMAX.exe` directly from the
unzipped folder.

---

## Installing

Run `NORMAX_Setup.exe` and follow the wizard:

- Installs to your user folder (no admin rights required).
- Adds a Start Menu entry.
- Optional desktop shortcut (checkbox in the installer).
- Standard Windows uninstall via **Settings → Apps → NORMAX → Uninstall**, or the
  Start Menu's "Uninstall NORMAX" shortcut.

## Using NORMAX

1. Launch **NORMAX** from the Start Menu.
2. Click **Select Video Folder** and pick a folder with `.mp4`, `.mkv`, `.avi`,
   `.mov`, `.wmv`, or `.webm` files. Everything else in the folder is ignored.
3. Click **Start Background**. Videos play in filename order, looping forever,
   with audio, as a centered 70%-sized panel on a black desktop layer.
4. Use the transport buttons (◀ ▶ ⏭ ↻), the volume slider, and the tray icon
   (double-click to reopen the window; right-click for quick controls) to
   control playback without covering the desktop with a visible UI.
5. Click the ⚙ icon to open **Settings**: video panel size (60–80%), which
   monitor to use, start-with-Windows, auto-start playback, loop/shuffle, etc.
   Settings are saved automatically and take effect immediately.
6. Closing the main window just hides it — playback keeps running. Use
   **Exit NORMAX** in the tray menu to fully quit.

Everything (last folder, volume, panel size, monitor, startup preference) is
remembered locally in `%AppData%\NORMAX\settings.json` — no cloud, no account.

## Uninstalling

**Settings → Apps → installed apps → NORMAX → Uninstall**, or the Start Menu
shortcut. This also removes the Windows startup entry if you had enabled it.

---

## Known risk areas (please test these)

These are the parts most likely to need a small tweak on a real machine, roughly
in order of likelihood:

1. **NuGet package versions** — `LibVLCSharp`, `VideoLAN.LibVLC.Windows`, and
   `Hardcodet.NotifyIcon.Wpf` versions pinned in `NORMAX.csproj` may have moved on;
   if the Actions build fails on restore, bump the version number it complains
   about.
2. **Desktop-layer attachment (WorkerW)** — the technique in
   `Wallpaper/DesktopWallpaperInterop.cs` is the same one used by popular
   wallpaper utilities, but Windows shell internals aren't officially documented
   and can shift between builds. If NORMAX ends up merely "always behind other
   windows" instead of "behind the icons too," it has silently fallen back to the
   safe mode in `WallpaperWindow.StartFallbackKeepBehind` — still fully usable,
   just not pixel-perfect wallpaper-layer placement.
3. **Multi-monitor coordinates on mixed-DPI setups** — positioning uses raw pixel
   rectangles from `MonitorHelper`; unusual scaling combinations across monitors
   are the main edge case worth checking.
4. **Codec coverage** — LibVLC covers the vast majority of real-world files, but
   an exotic codec inside an otherwise-supported container should be skipped
   gracefully (`PlaybackEngine` catches this) rather than crash — worth
   confirming with an oddball file if you have one.

## Project structure

```
src/NORMAX/
  App.xaml(.cs)            composition root: settings, engine, tray, wallpaper
  MainWindow.xaml(.cs)      main control panel UI
  Views/SettingsWindow      settings dialog
  Playback/                 VideoFileScanner, PlaylistManager, PlaybackEngine (LibVLC)
  Wallpaper/                WallpaperWindow, WorkerW interop, monitor enumeration
  Settings/                 AppSettings (POCO) + JSON persistence
  Tray/                     system tray icon + menu
  Startup/                  "start with Windows" registry helper
installer/NORMAX.iss         Inno Setup installer script
.github/workflows/build.yml  builds the .exe + installer on every push
```

## Building locally instead (optional)

If you'd rather build on your own Windows PC:

1. Install the free [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. `dotnet publish src\NORMAX\NORMAX.csproj -c Release -r win-x64 --self-contained true -o publish`
3. Run `publish\NORMAX.exe` directly, or install [Inno Setup](https://jrsoftware.org/isinfo.php)
   and compile `installer\NORMAX.iss` to get `NORMAX_Setup.exe`.
