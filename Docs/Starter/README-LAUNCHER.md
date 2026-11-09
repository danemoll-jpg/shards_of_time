# Shards of Time — complete download

This ZIP holds the launcher, the game and the Steam library artwork. The launcher keeps the game up to date. Each time you
open it, it checks for a newer version and installs it before the game starts, so you only install once.

The game in this ZIP may be older than the newest release. If so, the first start downloads and installs the newest
version, so be online for it.

## Windows

1. Extract the ZIP anywhere you can write to (for example `Documents\Games`). You get a `ShardsOfTime` folder.
2. Run `ShardsOfTime\ShardsOfTimeLauncher.exe`.
3. Leave it alone. It checks for updates, installs a newer version if there is one (the window shows the progress), then
   starts the game. Without an internet connection, or if the check fails, it starts the installed game.

B / Esc skips the update and plays the installed game at once. Any other button or key opens the launcher's menu:
**Check for updates**, **Update game and play**, **Update soundtrack**, **Restore previous version**. D-pad / arrows
choose, A / Enter confirms, B / Esc cancels.

## Steam Deck

1. Switch to **Desktop Mode**.
2. Download the ZIP and extract it anywhere in your home folder (for example `/home/deck/Games`) or on the SD card. You
   get a `ShardsOfTime` folder.
3. In Steam: **Games → Add a Non-Steam Game to My Library → Browse**, pick `ShardsOfTime/ShardsOfTimeLauncher.exe`, then
   **Add Selected Programs**.
4. Right-click the new entry → **Properties → Compatibility** → tick **Force the use of a specific Steam Play
   compatibility tool** → **Proton Experimental**.
5. Optional: the library artwork (below).
6. Back in Gaming Mode, play it from your library. The launcher updates the game, then starts it. The game opens full
   screen, and on the Deck its **Steam Deck mode** setting (Settings) turns itself on (lighter shadows, for the frame
   rate). The Deck's buttons work as an Xbox controller: the on-screen prompts use those names.

### Steam library artwork

The artwork files are in the top `ShardsOfTime` folder, beside the launcher. They're placeholders made from a game
screenshot until the real key art exists. Set them in **Desktop Mode**, in Steam's Library:

| File | What Steam calls it | Where to set it |
|---|---|---|
| `ShardsOfTime-Portrait.png` (600 x 900) | library capsule / grid | right-click the game → **Manage → Set custom artwork** |
| `ShardsOfTime-Wide.png` (920 x 430) | wide capsule (the "Recent games" shelf) | right-click the game's tile in the **Recent games** shelf → **Manage → Set custom artwork** |
| `ShardsOfTime-Hero.png` (1920 x 620) | hero / background banner | open the game's page, right-click the empty banner → **Set custom background** |
| `ShardsOfTime-Logo.png` (transparent) | logo over the hero | same page, right-click the banner → **Set custom logo** |
| `ShardsOfTime-Icon.png` | shortcut icon | right-click the game → **Properties**, click the icon square |

Keep using this same Steam shortcut from now on. Your saves and settings live in that shortcut's Proton prefix; a new
shortcut starts with a new, empty one.

## Your files

- `versions`: the installed game, plus the previous one so you can go back. Saves aren't stored here, and an update or
  a restore never removes them. A backup copy of the saves is made before each update, in `save-backups`.
- `shared`: the soundtrack, once the game has one.
- `recovery.log`: what the launcher did, if something goes wrong.
- `Launcher-Licenses`: the licences of the launcher's third-party code (JSON for Modern C++, miniz; both MIT).

Your save (`save.json`) and settings are in `%USERPROFILE%\AppData\LocalLow\Shards of Time\Shards of Time\` on
Windows (inside the Proton prefix on the Deck). **New game** on the title screen starts over and asks first.

Close the game before you install an update.
