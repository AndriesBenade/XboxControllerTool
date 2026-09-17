# XboxControllerTool

A Windows console application that turns an Xbox / XInput-compatible controller into a wireless mouse and lightweight keyboard input device. It's built for the "laptop connected to a TV, no mouse or keyboard nearby" scenario: pick up the controller from the couch and drive the whole desktop with it.

## Download

**[⬇ Download XboxControllerTool 1.0.0 (Windows Installer)](https://github.com/AndriesBenade/XboxControllerTool/raw/master/Releases/XboxControllerTool-1.0.0.msi)**

Run the `.msi` and you're done — no Visual Studio, no .NET SDK, no source code, nothing to copy by hand. The .NET runtime is bundled inside the installer. See [Installing](#installing-end-users).

The console window itself **is** the application UI — there is no separate configuration app, and no file ever needs to be hand-edited. It's styled as a dark, controller-first dashboard rather than a plain command-line tool: a persistent header, a live status dashboard, controller-glyph button prompts, and on-screen sliders/toggles for settings. Everything (settings, controller selection, live status) is reachable with the controller alone. See [Console UI](#console-ui) for the visual design.

## Features

- Left stick moves the mouse cursor with dead-zone filtering, an acceleration curve, and a configurable maximum speed — tuned to feel controllable from a couch rather than like a raw analog-to-pixel mapping.
- Right stick scrolls the active window vertically, tuned for a snappy, immediate response and smooth sub-notch increments rather than choppy, infrequent jumps — especially noticeable in Precision Mode.
- `A` / `X` map to left/right mouse click with correct press/release edge handling (holding the button does not spam clicks).
- **Spare controller buttons (XInput and raw HID) can be mapped to any keyboard shortcut** (`Win+D`, `Alt+Tab`, `Ctrl+Shift+Esc`, `Alt+F4`, media keys…) entirely from the controller — and those mappings **keep working while a game is focused**. See [Custom Button Mappings](#custom-button-mappings).
- `B` sends Backspace, held down for natural OS key-repeat, just like a physical held Backspace key.
- `BACK` sends Alt+Left, the standard Windows/browser "navigate back" shortcut.
- `START` is context-aware browser control rather than a fixed shortcut: opens/focuses your default browser, or sends Alt+Right (forward navigation) if it's already focused. See [Browser Control](#browser-control).
- `LEFT BUMPER` / `RIGHT BUMPER` send Win+D (Show Desktop) / Escape.
- `D-PAD LEFT` / `D-PAD RIGHT` move the text caret in whatever field currently has focus, also with natural key-repeat while held.
- Holding `LEFT TRIGGER` engages a temporary, non-persistent Precision Mode that slows the cursor (and moderately slows scrolling — enough to feel deliberate without ever feeling stuck) for fine work. `RIGHT TRIGGER` sends Enter/Return.
- **Automatically pauses itself while a game is focused** so the controller belongs entirely to the game, and resumes the moment you Alt-Tab away — with no game list to maintain. See [Automatic Game Detection](#automatic-game-detection).
- Installs as a normal Windows application via an MSI, can start with Windows, and ships seven UI themes plus adjustable console font size.
- `Y` toggles the app between hidden (minimized, controller drives the desktop) and shown (restored, centred, always-on-top, controller drives the menus), restoring focus to whatever window was in front before it was shown. Minimizing or restoring the window from the taskbar does exactly the same thing, so the two stay in sync — see [Console Window Behavior](#console-window-behavior).
- `D-PAD UP` opens the built-in Windows On-Screen Keyboard (`osk.exe`).
- `D-PAD DOWN` toggles Windows voice typing (simulates the `Win+H` shortcut to start/stop, plus `Escape` on stop so the flyout actually closes instead of just pausing).
- Small, always-on-top, non-activating notification toasts confirm state changes (precision mode, game focus pause/resume, app visibility, voice input, controller connect/disconnect/selection, browser, show desktop, on-screen keyboard).
- Supports multiple simultaneously connected controllers, with a mode to restrict control to one specific pad (useful when a second controller is being used to play a game).
- Settings and controller selection persist between runs in `%AppData%\XboxControllerTool`.
- The console starts minimized so it stays out of the way; desktop mouse/keyboard control is active immediately, and `Y` brings the menu up whenever it's wanted.

## Controller Mapping

| Input | Action |
|---|---|
| Left Stick | Mouse movement |
| Right Stick | Scroll (vertical) |
| A | Left click |
| X | Right click |
| B | Backspace (hold to repeat) |
| Left Stick Click | *Nothing — intentionally unassigned* |
| Back | Browser back (Alt+Left) |
| Start | Open/focus default browser, or forward nav (Alt+Right) if already focused |
| Y | Toggle console on top (centered) / background |
| Left Trigger (hold) | Precision mode (slower cursor and scrolling) |
| Right Trigger | Enter / Return |
| Left Bumper | Show desktop (Win+D) |
| Right Bumper | Escape |
| D-Pad Up | Open On-Screen Keyboard |
| D-Pad Down | Toggle voice input (dictation) |
| D-Pad Left / Right | Move text caret left/right (hold to repeat) |
| Right Stick Click, Guide, other spare buttons | Whatever you map them to — see [Custom Button Mappings](#custom-button-mappings) |

**Left Stick Click does nothing at all** — no key, no click, no notification. Left stick *movement* still drives the mouse as normal; only the click is unassigned, deliberately, so it can't fire by accident when you push the stick.

Holding both triggers at once favors Precision Mode. Controller mode is changed only from the **Controller Selection** menu — pressing `BACK` no longer has a global side effect, specifically so it can't be pressed accidentally (e.g. while playing a game with the same pad) and silently drop you back to All Controllers.

While the console is focused (after pressing `Y`), the same controller switches into **menu navigation mode**:

| Input | Action |
|---|---|
| D-Pad / Left Stick | Move selection |
| A | Select / confirm |
| B | Back / cancel |
| Left / Right | Adjust the selected setting |

`B` is intentionally dual-purpose: Backspace while controlling the desktop, Back/Cancel while navigating the menu — never both at once, since only one context is active at a time.

This mode switch is deliberate and important: while navigating the menu, the controller **only** drives the menu. Pressing `A` to confirm a menu item never also left-clicks whatever window happens to be underneath the console. See [Architecture](#architecture) for how that boundary is enforced.

## Requirements

- Windows 10 or Windows 11 (x64).
- .NET 10 SDK (or the .NET 10 Desktop Runtime to only run the built binary).
- An Xbox controller or other XInput-compatible controller (wired or wireless with its receiver/Bluetooth already paired).
- Administrator rights (the app requests elevation automatically on launch).

## Installing (end users)

Run **`XboxControllerTool-<version>.msi`** and follow the prompts. Nothing else is required — no Visual Studio, no .NET SDK, no runtime install, no cloning this repository, no copying DLLs. The installer is self-contained: the .NET runtime is bundled inside it.

The installer:

- installs to `C:\Program Files\XboxControllerTool`,
- creates Start Menu and Desktop shortcuts,
- registers the app in *Apps & features* so it uninstalls normally,
- **upgrades in place** — installing a newer version detects the existing installation via a stable `UpgradeCode` and replaces it rather than adding a second entry.

Your settings live in `%AppData%\XboxControllerTool\settings.json`, which is outside the installation directory, so **upgrading and uninstalling both leave your configuration intact**. Delete that folder yourself if you want a clean slate.

Uninstalling also removes the Windows startup task, so nothing is left behind trying to launch a deleted program.

## Running

Launch **XboxControllerTool** from the Start Menu or desktop shortcut. Windows shows a UAC prompt because the manifest requests `requireAdministrator` — accept it. If UAC is cancelled, Windows never starts the process; there is no elevation retry loop.

Elevation is required because simulated mouse/keyboard input and window-focus changes need to reach applications system-wide, some of which are themselves elevated.

The app starts **minimized**: desktop mouse/keyboard control is live immediately, and pressing `Y` on the controller brings the UI up whenever you want it.

## Start With Windows

**Settings → Start With Windows** toggles automatic startup at logon. No registry editing, no Startup-folder shortcuts, no config files.

Because the app requires administrator rights, a plain `HKCU\...\Run` entry is the wrong mechanism — Windows cannot show a UAC prompt at logon, so an elevated app registered that way is simply skipped. Instead this registers a **Scheduled Task** (`XboxControllerTool`) that triggers *at logon* for the current user with *highest privileges*, which is the supported way to auto-start an elevated application without a logon-time prompt.

The task name is fixed, and enabling uses `/F` (overwrite), so re-enabling can never create duplicate entries. The setting is stored in Windows itself — the task's existence *is* the setting — so it survives app restarts and upgrades without a config value that could drift out of sync with reality.

## Building from source / creating a release

Developers only. Requires the .NET 10 SDK.

```powershell
# build + test
dotnet build XboxControllerTool.slnx -c Release
dotnet test  XboxControllerTool.Tests -c Release

# publish self-contained and package into an installer
pwsh build\build-installer.ps1
```

`build\build-installer.ps1` does the whole release in one step:

1. reads `<Version>` from `XboxControllerTool.csproj` (the single source of version truth — the app header, the MSI's `ProductVersion` and the output filename all come from it),
2. `dotnet publish -c Release -r win-x64 --self-contained` into `artifacts\publish`,
3. installs the WiX build tool if it is missing (`dotnet tool install --global wix`),
4. builds `installer\XboxControllerTool.wxs` into `artifacts\XboxControllerTool-<version>.msi`.

To cut a new release, bump `<Version>` in the csproj and re-run the script. Ship the single `.msi`.

## Controller Selection

Two modes are available from the **Controller Selection** menu:

- **All Controllers** (default) — any connected XInput controller drives the mouse.
- **Specific Controller** — only one physical controller drives the mouse. Choose "Select Specific Controller", then press `START` on the desired pad. Useful when a second controller is being used to play a game and should not also move the desktop cursor.

To go back to All Controllers, open the Controller Selection menu and choose it explicitly. `BACK` intentionally does **not** do this automatically — an earlier version reset to All Controllers on any `BACK` press from any pad, which meant an accidental `BACK` press during normal use (e.g. in a game using the same controller) would silently change the desktop-control mode. Selection now only ever changes through a deliberate menu action.

### How a specific controller is identified (and its limitation)

**XInput does not expose a persistent hardware identifier for a controller.** The only public identity XInput gives you is a *slot index* (0–3, assigned by Windows) plus `XInputGetCapabilities` (device type/subtype/flags). There is no supported API to read a serial number or a stable GUID per physical pad through XInput.

This app identifies a selected controller by its **slot index + capability fingerprint** and persists both. On restart, or when a controller disconnects and reconnects while the app keeps running, if a controller is present in that same slot with a matching capability fingerprint, it's treated as the same controller. If the slot is empty or the capabilities don't match, the controller is reported **Unavailable** — the app never silently falls back to a different physical pad. If Windows reassigns slot numbers after a full disconnect/re-enumeration (which XInput does not prevent and does not expose a way to detect), the previously selected controller may show as unavailable even though a controller is physically present; this is a genuine Windows/XInput limitation, not a bug in the selection logic.

## Configuration

Settings are stored as JSON at:

```
%AppData%\XboxControllerTool\settings.json
```

- The file is created automatically on first run with sensible defaults.
- Every setting change from the Settings menu is saved immediately (atomic write via a temp file + rename).
- A missing, corrupt, or unreadable file is quarantined (renamed to `settings.json.corrupt-<timestamp>`) and replaced with a fresh default file rather than crashing the app.
- Values outside their valid ranges (e.g. from manual editing or a future downgrade) are clamped on load.
- An unrecognized settings version resets to defaults, so the app never crashes on an old/incompatible config format.

Persisted values include mouse/scroll/precision sensitivity, dead zones, mouse acceleration, notification duration/position, audio feedback on/off, theme, font size, pause-in-games, controller mode and selected controller identity, and console window size. Transient state — whether precision mode or voice input is *currently* active, whether desktop input is paused for a game, or which menu is open — is intentionally never persisted. (Start With Windows is deliberately *not* stored here: the scheduled task in Windows is the single source of truth.) The console window position is no longer persisted either: it is always centered on screen (see [Console Window Behavior](#console-window-behavior) below), so there is nothing meaningful to remember.

## Settings

All of the following are adjustable from the in-app **Settings** screen with `◀▶`, and save automatically. Each is shown as an actual control rather than raw text — a filled/empty block slider (`■■■■■□□□□□`) with its live value for numeric ranges, a filled/hollow dot for on/off toggles (`● ON` / `○ OFF`), or a `◀ value ▶` cycle for multi-choice settings — never a bare number or a config-file key:

- Custom Buttons (opens the [mapping screen](#custom-button-mappings))
- Theme, Font Size (cycles), Start With Windows, Pause In Games (toggles)
- Mouse Sensitivity, Scroll Sensitivity, Precision Sensitivity, Precision Scroll (sliders)
- Stick Dead Zone, Scroll Dead Zone (sliders)
- Mouse Acceleration (toggle)
- Notification Time (slider), Notification Spot (cycle)
- Audio Feedback (toggle)
- Restore Defaults (action, confirmed with `Ⓐ`)

## Voice Input

Windows does not provide a public, supported API to directly start/stop dictation or query its listening state. The documented, Windows-native way to invoke voice typing is the built-in keyboard shortcut **Win+H**, which opens the dictation flyout in the currently focused text field and starts listening.

`D-PAD DOWN` toggles it: the first press sends Win+H via `SendInput`. The second press sends Win+H again to stop listening, then **Escape** to fully dismiss the flyout — otherwise it can be left open in a paused state rather than actually closed. The app tracks its own ACTIVE/OFF state for the notification and Status screen; this reflects the app's last action, not a guaranteed read of whether Windows is actually listening, since there is no supported API to query that.

An earlier version of this feature tried to infer real state by watching for Windows' own voice-typing flyout window (via `EnumWindows`, looking for a window owned by the `TextInputHost.exe` process). In practice that heuristic never reliably detected the flyout and produced no notifications at all, which is worse than a simple toggle that's occasionally out of sync, so it was removed in favor of the direct toggle described above. Voice typing only does anything meaningful if a text field is focused when it's triggered. A short two-tone chime marks start/stop when Audio Feedback is enabled.

## On-Screen Keyboard

`D-PAD UP` launches `%SystemRoot%\System32\osk.exe`, the built-in Windows On-Screen Keyboard, via `Process.Start`. This is the standard, Microsoft-provided way to invoke it — there is no other supported API to open it programmatically, and no custom virtual keyboard is implemented. On some managed/locked-down systems `osk.exe` can be disabled by Group Policy, in which case the app reports "Unable to open On-Screen Keyboard" rather than failing silently.

## Automatic Game Detection

The app pauses its own desktop mouse/keyboard injection while a game is focused, so the controller belongs entirely to the game — then resumes automatically. There is **no game list to maintain**: you never add Minecraft, GTA V, Steam titles or anything else.

The rule is strictly about *focus*, never about what is merely running:

| Situation | Desktop controls |
|---|---|
| Minecraft focused | **Paused** |
| Minecraft running, you Alt-Tab to Chrome | **Active** |
| GTA V running in the background while you browse | **Active** |
| Alt-Tab back into the game | **Paused** again |
| Game closed | **Active** |

While paused, the app injects nothing at all — no cursor movement, clicks, keystrokes or shortcuts — and it stops acting on `Y` too, so pressing Y in-game (jump, reload, …) does not yank you out to the desktop. Alt-Tab out and the UI is available again. **The physical controller is never disabled**; the game keeps receiving it normally, and controller polling, connection and reconnection handling all keep running.

Any buttons being held when a game takes focus are released first, so a held `A` can't leave the left mouse button stuck down in the game.

### How the detection actually works

Windows has **no universal "is this a game" API**, so this uses focus-driven heuristics rather than pretending otherwise. On a foreground change the app resolves the foreground window's owning process (`GetForegroundWindow` → `GetWindowThreadProcessId` → `QueryFullProcessImageName`) and classifies it by two independent signals:

1. **Install location** — the executable lives under a game-distribution root (`steamapps\common`, `Epic Games`, `XboxGames`, GOG, Ubisoft, EA/Origin, Riot, or a generic `\Games\` folder). This is launcher-directory detection, not a game database.
2. **Controller runtime in use** — the process has an XInput / GameInput / DirectInput DLL loaded, i.e. it is itself consuming a gamepad. This is the signal that catches everything outside a launcher folder: Minecraft, emulators, itch.io builds, Store games.

Either signal alone marks the process as a game. Processes under `%WINDIR%` and the app's own process are never classified as games.

**Deliberate limitations, stated honestly:**

- Full-screen alone is *not* treated as a game signal. Browsers in F11 full-screen load Direct3D and lose their title bar, so using that as a signal would pause your controls every time you watch a video full-screen — the exact opposite of what a couch utility should do.
- A game that is both outside every known launcher folder *and* never touches a controller API (a keyboard-only indie title) will not be detected. In practice that matters little, since such a game ignores the controller anyway.
- Anti-cheat–protected processes may refuse module enumeration; those titles are almost always in a launcher directory, so signal 1 covers them.
- Classification is cached per process. A *positive* result is cached for the process's lifetime; a *negative* is re-checked every 5 seconds, so a game that was still loading its controller runtime when you first focused it is picked up shortly after rather than being misjudged forever.

The foreground check runs roughly every 32 ms and is just two cheap Win32 calls; the expensive part (process path and module scan) only runs when focus changes to a process not already classified. The detection logic lives in dedicated services (`ForegroundWindowWatcher`, `GameDetector`, `GameFocusMonitor`) behind interfaces — not inside the controller polling loop — so additional signals can be added later without touching input handling.

**Pause In Games** in Settings turns the whole behaviour off if you ever want the controls to stay live regardless.

## Custom Button Mappings

**Settings → Custom Buttons** lets you bind any spare controller button to a keyboard key or key combination, configured entirely with the controller — no keyboard, no mouse, no config file.

```
┌─ CUSTOM BUTTONS ───────────────────────────────────────────────────────────┐
│  ██ Detect Button   Press a spare button to add it                [ A ]    │
├─ DETECTED BUTTONS ─────────────────────────────────────────────────────────┤
│     [ R3 ]          Win + D                                                │
│     [ GUIDE ]       Alt + Tab                                              │
└────────────────────────────────────────────────────────────────────────────┘
```

### Nothing is mappable until you have pressed it

No button is offered from a hard-coded list of buttons a controller *might* have. **Detect Button** waits for a real press and shows you what Windows actually reported:

```
┌─ DETECT A BUTTON ──────────────────────────────────────────────────────────┐
│  Press and release the button you want to map.                             │
├─ RESULT ───────────────────────────────────────────────────────────────────┤
│  DETECTED        [ R3 ]                                                    │
│  ██ Map Button      Choose a key combination                      [ A ]    │
├─ WHAT WINDOWS CAN SEE ─────────────────────────────────────────────────────┤
│  045E-02FF       declares 16 buttons to Windows                            │
└────────────────────────────────────────────────────────────────────────────┘
```

If a press does not appear here, no software on the machine can map that button — see [Why AGL/AGR/M1/M2/MX may not appear](#why-aglagrm1m2mx-may-not-appear). The **Save Mapping** action stays locked until the button has been pressed in the current session, so a mapping can never be written for a button this machine cannot see. A button carrying a mapping from a previous run stays listed so you can review or clear it, but must be pressed again before its mapping can be changed.

Pick a button, then build the combination with toggles and pickers: `Ctrl`, `Alt`, `Shift`, `Win`, a key group (Letters / Digits / Function / Navigation / Editing / Media) and the key itself. `A` saves, `X` clears a mapping from the list. Examples that work today: `Win+D`, `Alt+Tab`, `Ctrl+Shift+Esc`, `Ctrl+C`, `Alt+F4`, `F5`, `Enter`, or media keys like Play/Pause and Volume Up.

Combinations are injected as one atomic `SendInput` batch — modifiers down, key down, key up, modifiers up in reverse — so a modifier cannot be left stuck. Mappings fire **once per press** (released → pressed), never repeatedly while held, so holding a button can't spam `Alt+Tab`. Any injected modifiers are also released on shutdown.

### Custom mappings keep working while a game is focused

This is the point of the feature. Normal desktop injection pauses while a game is focused, but custom mappings sit on a separate path and are **not** suppressed:

| While a game is focused | |
|---|---|
| Left stick → mouse movement | paused |
| `A` / `X` → clicks | paused |
| `R3` → `Win+D` | **still works** |
| `GUIDE` → `Alt+Tab` | **still works** |

So you can drop to the desktop or switch windows from the couch without closing the game or reaching for a keyboard. (They are suppressed in one place only: while XboxControllerTool's own UI is in front, so pressing a button to configure it can't also trigger it.)

### Which buttons can be mapped

Any button that reaches Windows and has no built-in action. Two independent sources are watched:

- **XInput** — `R3` (right stick click) and `GUIDE`. XInput's public `XInputGetState` masks the Guide button out, so the app resolves the undocumented ordinal-100 export (`XInputGetStateEx`) at runtime to see it, falling back to the public function if that ever fails. XInput's button mask is a fixed 16 bits and can never carry anything more.
- **Raw Input / HID** — the app registers a message-only window for HID usages *Joystick*, *Gamepad* and *Multi-axis Controller* with `RIDEV_INPUTSINK` (so presses arrive even while it is minimized) and parses pressed button usages with `HidP_GetUsages`. This is the only route to a button XInput has no bit for.

Windows also delivers HID reports for XInput pads, so every ordinary face button produces **both** an XInput edge and a HID edge. A HID press is therefore held for 60 ms and discarded if an XInput press arrived at the same moment — what survives is a button XInput never reported at all. Once a button is known to be HID-only it skips the wait and responds immediately.

Buttons are identified as `xinput:0080` or `hid:<VID>-<PID>:<usage>`, so a mapping survives a reconnect or a change of controller slot. Settings written before HID support are migrated automatically.

Buttons that already have built-in actions can never be remapped, and neither can Left Stick Click (it is meant to do nothing).

### Why AGL/AGR/M1/M2/MX may not appear

**Because on most pads those buttons never reach Windows at all.** A controller declares its buttons in its HID report descriptor, and that declaration is a hard ceiling on what any application can detect. The **WHAT WINDOWS CAN SEE** panel on the detection screen shows the count for each attached pad, read straight from the descriptor via `HidP_MaxUsageListLength`.

Measured on the controller attached during development (`\\?\HID#VID_045E&PID_02FF&IG_00`): **16 buttons** — exactly the 16 XInput already exposes, with nothing spare. On Xbox Elite pads the paddles are likewise remapped *by the controller firmware / Xbox Accessories app* onto ordinary buttons before anything reaches the PC, so a paddle arrives as (say) a plain `A` press, indistinguishable from the face button, or as nothing at all if left unassigned. The same applies to the Share button and to the M1/M2/MX buttons on many third-party pads, which are mode/profile switches handled entirely inside the controller.

If your pad declares more buttons than XInput uses, they appear the moment you press one. If it declares 16, the fix is not in this app: give those buttons an output in the controller's own configuration software (Xbox Accessories, or the vendor's app), and whatever they are mapped to will then be visible here.

Mappings are stored in `settings.json`, validated on load — an unknown key name or a reserved button is discarded rather than crashing — and preserved across upgrades.

## Themes

**Settings → Theme** cycles the UI theme with `◀ ▶`; the choice is saved immediately and restored on the next launch. Seven themes ship built in:

| Theme | Look |
|---|---|
| **Dark** | Default. Black background, cyan accent, neutral greys. |
| **Light** | White background with dark text for bright rooms. |
| **Xbox** | Black with restrained Xbox-green accents (accents only — frames stay neutral). |
| **Girly** | Pink/magenta accents on black, still high-contrast and readable. |
| **Matrix** | Green-on-black terminal aesthetic. |
| **Ocean** | Blue/cyan accents on black. |
| **Amber** | Amber CRT-inspired monochrome. |

Themes are data, not separate UI code: each is a `Theme` record of named colour slots (title, panel frame, label, value, focus, positive/active/negative, plus notification colours), and every component resolves colours through one accessor. A theme therefore restyles the entire interface at once — header, panels, menus, buttons, focus state, status indicators, controller mapping, hint bar **and the notification toasts** — with no per-screen theming code.

## Font Size

**Settings → Font Size** cycles Small / Medium / Large / Extra Large for TV viewing distance, applied via the documented `SetCurrentConsoleFontEx` console API (a real font change, not text-formatting tricks) and persisted.

**The console window resizes itself to match.** Changing font size runs the full sequence automatically — save the setting, apply the font, recalculate the window, resize, re-render — so you never drag a console edge yourself. The target size is *derived from the UI*, not a hardcoded guess: `UiDimensions` computes the required columns from the panel grid width plus margins, and the required rows from the tallest screen, so changing the layout changes the window that gets requested. That keeps the window from being needlessly wide at Small (it asks for exactly the columns the panels need) or too narrow to hold the borders at Extra Large.

Resizing is defensive about Windows' console rules: the requested size is clamped to `LargestWindowWidth`/`LargestWindowHeight` for the current font, and window/buffer are resized in the correct order for both growing and shrinking (buffer first when growing, window first when shrinking), with every call individually guarded so a rejected resize can never crash the app. A font size is also **rejected and rolled back** if the resulting console could not fit the interface at all, so a large font can't leave you with a cut-off, unusable screen.

The layout adapts too: the UI measures available rows each frame and switches to a compact density — dropping internal panel padding and the stick-mapping row — when the comfortable layout no longer fits.

**Known limitation:** console font APIs are honoured by the classic **Windows Console Host** only. **Windows Terminal** renders with its own profile font and silently ignores them. The app detects this (via `WT_SESSION`) and labels the setting *console host only* when it will have no effect. To use font sizing, set *Settings → System → For developers → Terminal* (or Windows Terminal's *Default terminal application*) to **Windows Console Host**, or just set the font size in your Windows Terminal profile instead.

## Browser Control

`START` is deliberately context-aware rather than a single fixed shortcut, so one button covers "open the browser," "switch to the browser," and "go forward" depending on what's already true:

1. The app resolves the current default browser the same way Windows itself does: it reads `HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice` for the registered `ProgId`, then resolves that `ProgId`'s `shell\open\command` in the registry to get the actual executable path. This is the standard, documented mechanism Windows uses internally for protocol/file associations — the same setting shown at Settings → Apps → Default apps → Web browser.
2. It looks for a running process with that executable's name that has a non-zero main window handle.
   - **No such window** → launches the resolved browser executable (or falls back to shell-executing `http://` if the registry lookup fails) on a background task, then polls briefly for its window to appear and brings it to the foreground once it does. This happens off the input-polling thread so the mouse/controller stay responsive while the browser starts.
   - **A window exists but isn't focused** → restores it first if it's minimized, then brings it to the foreground using the same `AttachThreadInput`-based activation the console window uses. This restore-if-minimized step lives in the shared `ForegroundWindowActivator` helper, so it applies uniformly rather than being special-cased per caller.
   - **Its window is already focused** → sends Alt+Right (forward navigation) instead, since "activate the browser" would otherwise do nothing. A minimized window is never reported as focused by Windows, so this branch naturally only triggers on a visible, active browser window.

This means START behaves like a dedicated "go to my browser" button that also becomes a forward-navigation button once you're already there, without needing a mode switch.

## Notification System

Important state transitions (not every polling tick) raise a small, borderless, always-on-top toast in the corner of the active display:

- `Precision Mode: ON` / `OFF`
- `Game Focused` / `Game Unfocused` (desktop controls paused / resumed)
- `App Shown` / `App Hidden`
- `Voice Input: ACTIVE` / `OFF`
- `Controller Connected` / `Controller Disconnected`
- `Controller Selected` / `All Controllers`
- `Browser Opened` / `Browser Focused`
- `Show Desktop`
- `OSK Opened` / `OSK Failed`

Implementation notes:
- Built with a `System.Windows.Forms.Form` running on its own dedicated STA thread with its own message loop, kept fully independent of controller/input code — input-processing classes only depend on an `INotificationService` interface and never touch a Win32 window directly.
- The window opts out of activation via `Form.ShowWithoutActivation` plus the `WS_EX_NOACTIVATE`/`WS_EX_TOOLWINDOW` extended styles, so it never steals keyboard focus or interferes with mouse input, and never appears in the taskbar or Alt+Tab.
- It appears on whichever monitor currently has the cursor, so it behaves reasonably on multi-monitor setups.
- Because callers only raise a notification on an actual state *transition* (edge-detected), holding a button or a trigger never re-triggers the same toast on every 8 ms poll.

## Console Window Behavior

The app has exactly two visible states, and **"hidden" means minimized**:

| State | Window | Controller drives |
|---|---|---|
| Hidden | minimized to the taskbar | the Windows desktop |
| Shown | restored, centered, topmost | the app's own menus |

The console is minimized right after startup (after being centered and sized once while still in its normal state, since a minimized window's rect isn't the one to centre). Desktop control is fully active while hidden.

Showing it — by pressing `Y` — centres the window on the screen containing the mouse cursor, brings it to the foreground and makes it topmost, using the standard `AttachThreadInput` technique to work around Windows' foreground-lock restrictions for background processes. It is re-centred every time, so it never ends up off-screen or wherever it was last dragged.

Hiding it — by pressing `Y` again — removes the topmost style, explicitly restores focus to whatever window was in the foreground before the app was shown (rather than leaving focus wherever it landed), and minimizes the console.

### Window state is kept in sync with the taskbar

The two states are driven by the *actual* window state, not only by `Y`, so using the taskbar behaves identically to using the controller:

- Restoring the window from the taskbar is treated exactly like pressing `Y` to show it — it becomes topmost and centred, the controller switches to menu navigation, and the `App Shown` notification fires.
- Minimizing the window yourself is treated exactly like pressing `Y` to hide it — the controller returns to driving the desktop and `App Hidden` fires.

This is implemented by observing `IsIconic` each poll and reacting only to *transitions* that disagree with the current input context, which keeps it immune to the fact that `ShowWindow` against the console host's window is asynchronous — a programmatic show/hide simply records the state change when it lands, without ever fighting itself. The one deliberate asymmetry: a user-initiated minimize does **not** force focus back to the previously-remembered window, since you probably minimized in order to go somewhere else.

## Console UI

The console is laid out as a designed application shell on a fixed 78-column grid, not as formatted terminal output. Every screen is built from the same components: a title band, bordered panels, button components, focus rows, and a contextual hint bar.

```
╔════════════════════════════════════════════════════════════════════════════╗
║  X B O X   C O N T R O L L E R   T O O L                           v1.0.0  ║
║  by Andries Benade                                                         ║
╚════════════════════════════════════════════════════════════════════════════╝

┌─ STATUS ───────────────────────────────────────────────────────────────────┐
│                                                                            │
│  CONTROLLER  CONNECTED (2)           MODE        ALL CONTROLLERS           │
│  SELECTED    ANY PAD                 SPEED       PRECISION                 │
│  VOICE       OFF                     WINDOW      SHOWN                     │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

┌─ CONTROLS ─────────────────────────────────────────────────────────────────┐
│                                                                            │
│  [ L-STICK ] Move Cursor             [ R-STICK ] Scroll                    │
│                                                                            │
│  [ A ]     Left Click    [ START ] Browser       [ LT ]    Precision       │
│  [ X ]     Right Click   [ BACK ]  Browser Back  [ RT ]    Fast Speed      │
│  [ B ]     Backspace     [ LB ]    Show Desktop  [ LS ]    Enter           │
│  [ UP ]    Keyboard      [ DOWN ]  Voice Input   [ RB ]    Escape          │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

┌─ MENU ─────────────────────────────────────────────────────────────────────┐
│                                                                            │
│  ██ SETTINGS      Speed, dead zones and alerts                    [ A ]    │
│     CONTROLLER    Choose which pad drives the desktop                      │
│     STATUS        Full input and configuration detail                      │
│     EXIT          Close XboxControllerTool                                 │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

   [ A ] Select     [ UP/DN ] Navigate     [ Y ] Hide App
```

- **Title band** — a double-ruled band (`╔═╗`) with letter-spaced title text, version right-aligned and the author line beneath. Double rules are used *only* for the app band; panels use single rules, which gives a two-level frame hierarchy rather than decoration for its own sake.
- **Panels with titled borders** — section names sit inside the top border (`┌─ STATUS ─…─┐`) and sub-sections inside a tee divider (`├─ SENSITIVITY ─…─┤`) within the same panel, so related fields stay in one visual container instead of each value getting its own box.
- **Controller mapping is on the home screen** — the full desktop mapping is a permanent panel on the dashboard, laid out as a 3-column grid of button components. Nothing needs to be opened to find out what a button does.
- **Button components, not letters in a sentence** — every controller reference anywhere in the app renders through one component as `[ A ]`, `[ LB ]`, `[ START ]`, `[ UP/DN ]`, with dim brackets and a bright label, so it reads as a physical button and is visually distinct from body text. There is exactly one visual language for this: no `(A)`, `Press A` or bare `A` anywhere.
- **Focus state that doesn't depend on colour** — the focused row is marked with a solid `██` bar in the accent colour *and* its label switches to the accent colour *and* it shows the `[ A ]` (or `[ L/R ]`) badge that activates it. The bar column is always reserved, so rows never shift horizontally as focus moves.
- **Real settings controls** — numeric settings render as a block gauge (`████░░░░░░░░  14.0`) with the live value, booleans as `[ ON  ]` / `[ OFF ]`, and multi-choice values as `<  TOP RIGHT  >`. No raw numbers-only rows and no config-file syntax.
- **Restrained palette** — black background, one accent (cyan) for title/focus, gray body text, dark-gray labels and frames, and semantic colour only for state (green connected, red disconnected, yellow active/fast). Most of the screen is deliberately calm.
- **Spacing as a component** — every panel has a blank padding row top and bottom, panels are separated by a blank line, and the hint bar is offset from the last panel, so the layout breathes at TV distance.

### Character safety (why there are no Xbox glyphs)

An earlier pass used circled letters (`Ⓐ Ⓑ Ⓧ Ⓨ`) and geometric shapes (`▲▼◀▶`, `●○`, `❯`, `☰`); they rendered as replacement boxes. The cause was confirmed by inspecting the actual `cmap` tables of the fonts a Windows console can use:

| Codepoint | Cascadia Mono | Consolas | Lucida Console | Segoe UI Symbol |
|---|---|---|---|---|
| `┌` U+250C (box) | yes | yes | yes | yes |
| `█` U+2588 (block) | yes | yes | yes | yes |
| `Ⓐ` U+24B6 (circled A) | **no** | **no** | **no** | yes |
| U+E001 (private-use icon) | **no** | **no** | **no** | yes* |

`Ⓐ` simply does not exist in *any* font the console renders with — it only exists in proportional UI fonts like Segoe UI Symbol. This is also why it can't be "fixed" by changing encoding: there is no glyph to draw.

**Could real Xbox button glyphs be used?** Practically, no:

- Windows ships icon fonts (`Segoe MDL2 Assets`, `Segoe Fluent Icons`) containing gamepad artwork, but those glyphs live in the Unicode *private use area*. PUA codepoints carry no Unicode identity, so DirectWrite font fallback cannot resolve them — the terminal would have to be explicitly set to the icon font, which would then break every box-drawing character and all body text.
- A bundled custom font would need the user to install it and change their terminal font manually, and would still fail in legacy `conhost` with a raster font.

So the UI uses **only** ASCII plus box-drawing and block-shading characters, all of which are both CP437-native and present in every console font checked. Controller buttons are ASCII `[ A ]` components by design rather than as a fallback. (Even `↑`/`↓` are outside CP437, which is why navigation hints read `[ UP/DN ]`.) `ConsoleUiRenderingTests` renders every screen and fails the build if any character outside the verified set appears, so broken glyphs cannot be reintroduced.

This structure replaces the previous Controls/Help/About screens — identity/version moved into the permanent title band, and the controls reference became the dashboard panel plus the per-screen hint bar.

## UI Performance

Rendering is fully decoupled from controller polling and input simulation: `DesktopInputController` (mouse/keyboard/OSK/voice/browser) and the `ScreenNavigator`/`ConsoleFrameRenderer` never run in the same branch of the polling loop (see [Architecture](#architecture)), so menu rendering is skipped entirely while the console is minimized and controlling the desktop, and desktop input simulation never waits on a console write.

Within the menu, there are three layers of throttling:

1. **Input is polled at 125 Hz but the UI is not rebuilt at 125 Hz.** A frame is only rebuilt when a menu action actually occurred, or when a 100 ms refresh interval elapses to pick up live status changes. Navigation therefore responds on the very next tick (≈8 ms) while idle dashboard redraws cost ~10/second instead of 125.
2. **The renderer diffs against the previous frame** and rewrites only rows whose content or colour actually changed, so moving the selection touches two rows, not the whole screen — this is what keeps it flicker-free.
3. **Row equality is structural** (`ConsoleLine` compares its segments by value), so an identical rebuilt frame produces zero console writes.

## Architecture

```
Core/           Controller state, button flags, edge detection, multi-controller aggregation — no OS dependency
Input/          XInput P/Invoke, polling, controller selection/identity
Simulation/     SendInput-based mouse/keyboard simulation (behind IMouseInput / IKeyboardInput)
Processing/     Dead zone, sensitivity curves, precision mode — pure, hardware-independent math
Windows/        Console window focus/topmost + font size, window placement, on-screen keyboard launch,
                default browser control, Windows startup task, foreground window + game detection
Notifications/  INotificationService + the WinForms overlay implementation
Audio/          Short tone feedback (System.Console.Beep)
Configuration/  Settings model + JSON persistence
ConsoleUi/      Design system (theme, CP437-safe glyphs, panels, button components, gauges/toggles,
                focus rows, hint bar), diff renderer, navigation stack, screens
Application/    Composition of the above into the running app (AppLoop, DesktopInputController, AppState)
```

Key boundaries:
- **Controller polling is separate from Windows input simulation.** `ControllerManager` only reads XInput state; `MouseSimulator`/`KeyboardSimulator` only call `SendInput`. Nothing in `Processing/` touches a Windows API, which is what makes it unit-testable.
- **Desktop control vs. menu navigation is an explicit, single switch** (`InputContext`, flipped by `Y`). `DesktopInputController` (mouse/keyboard/OSK/voice) only runs while the context is `DesktopControl`; the `ScreenNavigator` only receives input while the context is `MenuNavigation`. This is what guarantees `A` never both confirms a menu item and left-clicks the desktop.
- **Controller selection only changes through an explicit call** (`ControllerSelectionService.ResetToAllControllers()` / a completed specific-controller selection) — there is no implicit button-driven side effect baked into per-tick polling, which is what makes the mode immune to an accidental button press during normal use.
- **Foreground-activation logic is shared, not duplicated.** `ForegroundWindowActivator` holds the one `AttachThreadInput`-based implementation, used by both `ConsoleWindowController` (toggling the console) and `DefaultBrowserController` (focusing the browser).
- **Rendering never lives in input code.** Screens build `ConsoleLine`/`ConsoleSegment` data; `ConsoleFrameRenderer` is the only thing that writes to the console. `DesktopInputController` and `ControllerManager` have no reference to it at all, and `AppLoop` only calls it while the app is in menu-navigation context.
- **Game detection is a service, not loop code.** `ForegroundWindowWatcher` (which window/process is in front) and `GameDetector` (is this process a game) sit behind `IForegroundWindowSource` / `IGameClassifier`, and `GameFocusMonitor` turns them into a debounced state with explicit transitions. `AppLoop` only ticks the monitor and reacts to transitions, which is what makes the whole behaviour unit-testable without a game installed.
- **Theme and font are preferences, not rendering concerns.** `UiPreferences` owns applying/persisting theme, font size and the startup task, and raises one `SurfaceInvalidated` event; nothing in the controller or simulation layers knows a theme exists.
- **Custom mappings are a separate input path.** `CustomButtonService` (detection, storage, validation, dispatch) is invoked by `AppLoop` *before* the game-focus pause check, while `DesktopInputController` is invoked after it. That ordering is the whole reason custom shortcuts keep working inside games, and it keeps the two concerns from tangling.
- Interfaces (`IMouseInput`, `IKeyboardInput`, `IOnScreenKeyboardLauncher`, `INotificationService`, `IAudioFeedbackPlayer`) exist specifically around the pieces that talk to the OS, so the input-mapping logic that drives them can be unit tested with simple fakes instead of real hardware/Windows APIs.

## Testing

```
dotnet test XboxControllerTool.Tests
```

Covers, without requiring any controller hardware: UI rendering guarantees (every panel row is exactly the grid width, no character outside the verified CP437-safe set appears on any screen, every screen ends in a hint bar, the home screen contains the controller mapping and app identity, exactly one focus bar is rendered), button edge detection (including "holding a button never repeats a press"), controller selection in both modes plus confirmation that `BACK` no longer has an implicit reset side effect, game-focus state transitions (focusing a game pauses desktop input, Alt-Tabbing away resumes it, a game merely running in the background never pauses it, staying inside a game never repeats the transition, positive classifications are cached while negative ones are retried, and the app's own window is never treated as a game), custom button mappings (every built-in button and Left Stick Click are rejected as mapping targets, spare buttons are detected on first press, a mapping fires exactly once per press and never while held, unmapped presses send nothing, detection still works while dispatch is suppressed, mappings survive a restart, reassigning replaces rather than duplicates, and invalid keys/reserved buttons are discarded on load), theme/font-size/pause-in-games persistence across a restart plus settings files written by an older version still loading, precision-trigger transitions, analog stick dead zone/acceleration math, mouse movement accumulation (including the precision multiplier and small-stick sustained movement), scroll accumulation and its dead zone/speed scaling (including that output is emitted immediately in smooth sub-notch increments rather than lagging until a full 120-unit notch accumulates), multi-controller state aggregation, settings JSON round-tripping plus corrupt/out-of-range/unknown-version handling, and menu navigation (selection movement, wraparound, push/pop, cancel-at-root).

Everything that talks to XInput, `SendInput`, or Win32 windowing is intentionally kept out of the test project and only exercised by running the real app, since it has no meaningful way to run headless.

## Troubleshooting

- **No controller detected** — reconnect it and confirm Windows itself sees it (Settings → Bluetooth & devices → Controllers, or `joy.cpl`).
- **Cursor doesn't move** — check Controller Selection: if it's set to a specific controller, confirm that's the one you're holding. Note `BACK` will not switch this back to All Controllers automatically anymore — use the menu.
- **On-Screen Keyboard doesn't open** — `osk.exe` can be blocked by Group Policy on some managed machines; the app will report the failure rather than pretending it worked.
- **Voice input does nothing** — click/select a text field first; Win+H only opens dictation for the currently focused input.
- **The console won't come to the foreground** — Windows' foreground-lock behavior can, in some configurations, refuse focus to background processes; the app works around this with the standard `AttachThreadInput` technique, but a fully locked-down desktop policy can still block it. The window is still made topmost even if focus itself is denied.
- **Previously selected controller shows Unavailable after a reboot** — see the [Controller Selection](#controller-selection) limitation above; this is an XInput identity limitation, not silently switching controllers.

## Known Windows Limitations

- XInput exposes no persistent per-controller hardware ID; specific-controller selection is best-effort via slot + capability fingerprint (see above).
- Windows exposes no public API to query real dictation/listening state; voice input ACTIVE/OFF reflects the app's own toggle of the Win+H/Escape sequence, not confirmed microphone state. An attempt to infer real state from Windows' voice-typing flyout window was tried and abandoned — it never reliably detected the flyout in practice.
- `SetForegroundWindow` is subject to Windows' foreground-lock restrictions for background processes; the app uses the documented `AttachThreadInput` workaround, which is reliable in normal desktop sessions but not guaranteed on every system policy.
- `osk.exe` can be disabled by Group Policy on managed/locked-down machines, in which case the app reports the failure rather than working around it with a custom keyboard.
- Default browser resolution depends on the `UserChoice` registry association Windows itself uses; if no default browser is set, or a browser overrides that association in an unusual way, `START` falls back to shell-executing a bare `http://` URL rather than a specific executable, and the "already focused" check (which relies on matching that resolved executable's process) won't apply.
- **Windows exposes no universal "this process is a game" API.** Detection is heuristic (launcher install directory, or the process having a controller-input runtime loaded) and deliberately conservative about full-screen windows. See [Automatic Game Detection](#automatic-game-detection) for exactly what is and isn't detected.
- **Console font size only applies under the classic Windows Console Host.** Windows Terminal ignores `SetCurrentConsoleFontEx` and uses its own profile font; the app detects this and labels the setting accordingly.
- Auto-start uses a Scheduled Task rather than a `Run` registry entry, because Windows cannot prompt for UAC at logon and would otherwise skip an elevated app. Creating or removing that task requires the app to be running elevated, which it always is.
- **A controller's HID report descriptor is a hard ceiling on detectable buttons.** XInput cannot see Elite paddles (AGL/AGR), the Share button or M1/M2/MX, and on most pads neither can Raw Input/HID, because those buttons are handled inside the controller and never reach the PC as buttons at all. The app reads each pad's declared button count and shows it, rather than offering buttons it cannot detect — see [Why AGL/AGR/M1/M2/MX may not appear](#why-aglagrm1m2mx-may-not-appear).
- The Guide button is only readable through XInput's **undocumented** ordinal-100 export. It is resolved at runtime with a fallback to the public API, so if a future XInput version drops it the app keeps working — Guide simply stops being listed.

## Future Improvements

- Rumble/vibration feedback for state changes, for controllers that support it.
- Per-application sensitivity profiles.
- A secondary, fully custom on-screen keyboard for systems where `osk.exe` is policy-disabled.
- Additional game-detection signals (the classifier is structured to accept them without touching input handling).

## Hardware Verification

This was developed and unit-tested in an environment without a physical Xbox controller attached, so changes are implemented and reasoned through against the documented Win32/XInput APIs and the automated test suite, but not run end-to-end here. Real-hardware testing by an actual user has already caught and driven several rounds of fixes for issues invisible to unit tests alone — scroll feel and smoothness (both normal and precision-mode speed), focus-restore behavior on the console toggle, the console being blank on first launch until Y was pressed, the voice-typing flyout detection heuristic turning out to be unreliable in practice and being replaced with a simpler deterministic toggle, the full X/B/Back/Start/bumper remapping, and minimized-on-launch behavior plus restoring a minimized browser window before focusing it.

The first UI pass shipped Unicode glyphs that rendered as replacement characters on the real machine; the redesign replaced them with a CP437-verified character set (see [Character safety](#character-safety-why-there-are-no-xbox-glyphs)) and added a test that fails the build if an unverified character reappears. Every screen's exact rendered output was inspected line-by-line during development, so grid alignment and content are confirmed.

**Verified here (automated / inspectable):** the solution builds clean in Release with no warnings; the full test suite passes; the release script runs end to end and produces a ~40 MB self-contained MSI (271 files, .NET runtime bundled) whose `ProductName`/`ProductVersion`/`UpgradeCode` were read back out of the built package; every screen — including the two new custom-mapping screens — renders with every panel row exactly the grid width, using only characters verified present in real console fonts; the custom-mapping engine, game-focus state machine and settings persistence are covered by unit tests using fakes.

Raw Input is not only reasoned about: an automated test performs the real `RegisterRawInputDevices` call for HID gamepads on the build machine and fails if registration is refused, and the device inventory was run against the attached controller — `\\?\HID#VID_045E&PID_02FF&IG_00`, usage page `0x01`, usage `0x05`, **16 declared buttons**, which is what proves that pad has no spare buttons to offer rather than the app failing to look.

**Not verified here, because it needs a real desktop session, a controller and games:** installing/upgrading/uninstalling the MSI; the logon scheduled task actually launching the app; whether a *different* controller declares spare buttons beyond the 16 (the detection path itself is covered by unit tests with a fake HID source); custom mappings firing inside a real game; actual console font/window resizing at each size on a TV; and game detection against real titles. The automated tests prove the *logic* is right; only a real run proves the *Windows integration* is.
