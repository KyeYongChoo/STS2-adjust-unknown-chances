# Adjust Unknown Chances (Slay the Spire 2 mod)

- **Map tooltip:** hover an unvisited **?** node to see its odds of becoming an Event, Monster, Elite, Treasure or Shop room. The Elite chance is only above zero with the **Deadly Events** modifier, or if you set it yourself.
- **Custom odds:** tick **Use custom odds**, then set Monster/Elite/Treasure/Shop. The box underneath shows the Event chance, which is whatever is left over. The settings are in two places, and both change the same settings:
  - **Main Menu → Settings → Mods → Adjust Unknown Chances** (always available)
  - **Main Menu → Mod Configuration → Adjust Unknown Chances** (only when [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) is installed; BaseLib is optional)

## How the odds work (vanilla rules, which the mod follows)

Each non-Event type has a current chance. Event gets whatever is left. When you enter a ? room:
- The type that was rolled resets to its base chance.
- Every other allowed type goes up by its base chance. Deadly Events doubles the increase for Treasure.

The tooltip reads the run's current chances and applies the same rules the game uses:
- A Shop can't roll right after a shop, or when every next node is a shop.
- Relics and quests that force Events (Juzu Bracelet, Golden Compass, Lantern Key) are taken into account.
- So are the fixed rooms in your very first run.

The odds change after every ? room you visit. Nodes further up the map therefore show the odds you'd get if that node were your next ? room.

The values you set are the **base** chances. They are applied when a **new run** starts and replace the defaults, including Deadly Events' 10% elite chance. A run loaded from a save keeps the current chances stored in the save, and only the base values are updated. In multiplayer, every player should use the same settings.

Settings are saved to `%APPDATA%\SlayTheSpire2\AdjustUnknownChances.settings.json`.

## Build / install

Requires the .NET 9 SDK.

```powershell
dotnet build -c Release                  # build only -> bin/Release/AdjustUnknownChances.dll
dotnet build -c Release -p:Install=true  # build and copy to <game>/mods/AdjustUnknownChances
```

If the game is not in the default Steam folder, add `-p:STS2GameDir="D:\...\Slay the Spire 2"`.

The build produces two DLLs, and both must be shipped (e.g. in the Workshop `content` folder) together with `AdjustUnknownChances.json`:
- `AdjustUnknownChances.dll`: the mod itself. It has no dependency on BaseLib.
- `AdjustUnknownChances.BaseLib.dll`: adds the BaseLib *Mod Configuration* entry. It is loaded only when BaseLib is installed. It's a separate file because the game loads every class in a mod's DLL at startup, so a class that uses BaseLib would stop the whole mod loading for players without BaseLib.

Building it requires BaseLib.dll. The default path is the Workshop copy at `steamapps/workshop/content/2868840/3737335127/BaseLib/BaseLib.dll`. Override it with `-p:BaseLibDll="..."`.
