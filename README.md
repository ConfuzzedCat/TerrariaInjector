![alt text](https://github.com/ConfuzzedCat/TerrariaInjector/blob/master/TerrariaInjector_Long.png?raw=true)
# TerrariaInjector
#### What is it?
TerrariaInjector is a program to inject harmony patches in to Terraria.
Though it should work with any XNA/.net Applications.

## How to install?
Simply put **TerrariaInjector.exe** and download the latest 0Harmony.dll from [here](https://github.com/pardeike/Harmony/releases/latest) (choose the one in the `net35` folder) into your terraria installation folder and run it. 
Because of how it works, you will need to run the injector every time you want to play with mods/patches.

## How does it work?
TerrariaInjector first checks for Terraria.exe in the current dir, and then loads it into itself.
It then loads the embedded dependencies from the game. After it will load the libraries dll in the mod folder.
The default Terraria save folder is then checked, and injects the path into a field. This is a workaround for the issue with the main class now being static. Which also means that TerrariaInjector doesn't support any other save paths.
The patched game assembly is then invoked, starting the game.

## Folder Structure

By default, TerrariaInjector looks for mods in:
- `Mods/` folder (original structure)

You can also use a custom folder structure via INI config file:
- `TerrariaModder/core/config.ini` or `Mods/config.ini` defines folder paths
- Example config:
```ini
[Paths]
rootFolder=TerrariaModder
coreFolder=core
depsFolder=core/deps
modsFolder=mods
logsFolder=core/logs
```

## Server Support

TerrariaInjector automatically detects `TerrariaServer.exe` and skips UI-related patches.

## Loading Blocked DLLs

Uses `Assembly.UnsafeLoadFrom()` to load DLLs without requiring manual unblocking or `.exe.config` files. Downloaded DLLs from the internet work automatically.

## How to create a patch/mod
Basic knowledge of HarmonyLib is required. You also need to reference both Harmony(0Harmony.dll) and Terraria(Terraria.exe).
Each mod can have a Init(or Initialize) and a PrePatch method, which each get run at initialization and before patching the game.
For and example/template look at the template.cs file in the 'Extra' folder to see an example made by deltaone.
You can also use the files in the 'Extra/ModCompile' to help with compiling, just remember to change the modname in the bat file.

## How to target another game
simple make a target file in the modfolder of the game, with the name of the game with the extension eg. 'Terraria.exe'. For an example check the target file in the 'Extra' folder.

## Credits
Thank you Deltaone for help with the OutOfMemomery bug and over all fixing and cleaning.

Inidar - <TBD>

Deltaone - https://github.com/deltaone

Tiberiumfusion - TT2 & https://github.com/TiberiumFusion/TTPlugins

Dougbenham - https://github.com/dougbenham/TerrariaPatcher
