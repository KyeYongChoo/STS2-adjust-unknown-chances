# Project notes for Claude

Slay the Spire 2 (**v0.111.0**) mod: hovering an unknown (?) map node shows its odds of becoming an Event, Monster,
Elite, Treasure or Shop room, and the player can set their own base odds. See README.md for the player's view and the
build steps. Published on the Steam Workshop as item **3814064648**
(https://steamcommunity.com/sharedfiles/filedetails/?id=3814064648).

## Steam Workshop upload workspace

The Workshop copy is uploaded from
`C:\Users\chook\Desktop\sts2-mod-uploader\bin\Debug\net8.0\AdjustUnknownChances\`, **outside this repo and not under
version control**. When the mod changes, update it there as well as here:

- `content\`: `AdjustUnknownChances.dll` and `AdjustUnknownChances.BaseLib.dll` from this repo's `bin\Release\` (after
  `dotnet build -c Release`), plus `AdjustUnknownChances.json` from the repo root. Ship all 3 together. Bump `version`
  in `AdjustUnknownChances.json` for each release.
- `workshop.json`: the Workshop page's title, description (keep it in step with README.md: features, menu paths,
  game version, GitHub link), tags and `changeNote` (describe each update).
- `image.png` (cover) and `previews\` (extra screenshots): each must be under 1 MB. Preview files are matched by
  filename on update, so renaming one replaces it on Steam. The repo's `preview_images\` holds copies of the
  screenshots.
- `mod_id.txt`: holds `3814064648`. Keep it, or the next upload creates a new Workshop item.

The repo's `upload\` folder (gitignored) is a local staging copy of the 3 content files; the workspace's `content\` is
what actually gets uploaded.

Don't upload yourself. The user runs `ModUploader.exe upload -w AdjustUnknownChances` from
`C:\Users\chook\Desktop\sts2-mod-uploader\bin\Debug\net8.0\`. The sibling mods' workspaces are next to it
(`CustomDrawOrder\`, `RngPreview\`).

## Ground rules

- **Never guess game behaviour; read the decompiled code** in `C:\Users\chook\Desktop\sts2-decompiled\` (`sts2\`,
  `BaseLib\`; shared by all STS2 projects, never commit it). Run `bash ~/Desktop/sts2-decompiled/update.sh` first: it
  decompiles again only if the game or BaseLib updated.
- `dotnet build -c Release -p:Install=true` installs into `<game>\mods\AdjustUnknownChances\`, which fails while the
  game runs. Don't kill the game; ask the user to close it.
- The user tests in game; say plainly what hasn't been tried in game.
- Commit only when asked.
- Settings pages: Main Menu → Settings → General → Mod Settings (always), and Main Menu → Mod Configuration (only
  when BaseLib is installed; BaseLib is optional). The BaseLib part is a separate `AdjustUnknownChances.BaseLib.dll`
  because the game loads every type in a mod's DLL at startup.
