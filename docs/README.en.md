# MuseDashSteamPresence

[简体中文](../README.md) | English

Free, MIT-licensed independent MelonLoader plugin by **樊小胃**. Shows Muse Dash song and artist information in Steam Friends using the real AppID 774171.

**Experimental, Windows x64, pinned Steam client build only.** Unsupported Steam DLL hashes disable publication; see [compatibility](COMPATIBILITY.md). A public-beta full gameplay regression is still pending.

## Install

Before downloading: if MelonLoader 0.7.3 net6 already works with Muse Dash, follow the extraction steps below. Otherwise, install it first using the official installer paragraph. Merely downloading an installer or another mod does not install the loader.

1. Have MelonLoader net6 installed and launch the game once. Tested with MelonLoader 0.7.3 and Muse Dash 6.7.0. No other mod is required.
2. Download `*-win-x64.zip` from the repository's Releases, not GitHub's generated source archive.
3. Close Muse Dash and the settings window. Extract **all ZIP contents directly into the game folder**, beside `musedash.exe`. Merge `Mods`, `Plugins` and `UserData`; do not add another containing folder.
4. **Configure first:** Open `MuseDash-Settings.cmd`, adjust the Chinese settings UI and click `保存并应用` (save and apply). The game does not need to be running. You may close the settings window after saving.
5. **Then play:** Launch Muse Dash through Steam. The plugin reads the saved settings.

No installer, extra Steam login or friend-side plugin is required. MelonLoader is not bundled.

If MelonLoader is not installed, get **MelonLoader.Installer.exe** from the [official downloads](https://github.com/LavaGang/MelonLoader.Installer/releases/latest). With the game closed, select Muse Dash (or **Add Game Manually** and select `musedash.exe`), choose loader version **0.7.3**, and click **Install**. Launch the game once to initialize it, then close it and extract this plugin. The installer version and loader version are different.

## Euterpe Hub single-DLL distribution

After the Hub submission is approved, click download and launch the game normally. The main DLL bundles its own helper, independent settings application, early plugin and documentation; it prepares those files locally without fetching more executables. After the first launch, open `MuseDash-Settings.cmd` to customize all existing options independently. The early component takes effect on subsequent launches; the first launch requests state when the main DLL registers. The full ZIP can still be configured before the very first launch.

Euterpe's own first-time environment setup for Muse Dash can install MelonLoader automatically as part of its player preset. Merely downloading Euterpe does not install the loader; the setup also prepares other components and has its own requirements. It is optional, not a new dependency of this mod.

Hub submission and complete new-build gameplay regression remain pending. See [Hub upload fields and review notes](EUTERPE-HUB.md).

## Settings

On the currently verified client, switching between selecting and playing updates the displayed text without repeatedly triggering new "started playing" popups. Initial game launch can still notify friends normally; future Steam versions require revalidation. See the main README for author-supplied self/friend-view screenshots.

- Non-playing text is disabled by default: idle shows `Muse Dash`, playing shows `Muse Dash : song title`. Custom idle text and artist display can be enabled independently.
- Song title defaults on; artist defaults off. Existing configuration is preserved on upgrade; save "restore defaults" to adopt the new defaults.
- Choose no separator, a space, `_`, or custom text.
- The settings app runs independently; neither the game nor Steam needs to be running. Saved settings load on the next game launch. While the game is running, changes apply in about 0.5–1 second. Closing the settings window keeps the configuration saved.

**Long song titles plus artist names may exceed text or UI limits and appear incomplete. There is currently no guaranteed full-display solution.** You may hide the artist or shorten the separator to reduce truncation. Improvements and contributions are welcome.

With the game closed, `MuseDash-Disable.cmd` disables both plugins; `MuseDash-Enable.cmd` restores them. Back up your own files before updating by extraction. Settings are preserved because release packages do not contain `settings.xml`.

The settings window also has independent enable/disable buttons. No other mod manager needs to be open. It checks loader and file presence, distinguishes disabled/incomplete installs, and keeps configuration editable even when publication cannot run. Turning off only the main mod with an external manager now prevents the early component from starting on the next game launch.

Keep Steam on its latest stable release. Client build 1792618285 was checked on 2026-11-01 with no update reported; the pinned DLL hash remains the final compatibility check, not an assurance for all future latest versions. A short plain-text guide is included as `MuseDashPresence-ReadMe.txt`.

See the [main README](../README.md), [build guide](BUILD.md) and [MIT license](../LICENSE).
