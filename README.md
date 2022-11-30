![alt text](https://github.com/ConfuzzedCat/TerrariaInjector/blob/master/TerrariaInjector_Long.png?raw=true)
# TerrariaInjector
#### What is it?
TerrariaInjector is a program to inject harmony patches in to Terraria.

## How to install?
Simply put **TerrariaInjector.exe** and download the latest 0harmney.dll from [here](https://github.com/pardeike/Harmony/releases/latest) (choose the one in the `net35` folder) into your terraria installation folder and run it. 

Because of how it works, you will need to run the injector everytime you want to play with mods/patches.

## How to uninstall?
Simply rename the injector to either "uninstall" or "remove". This will delete the  Mods folder, the extracted resources(see more at how it works), the Harmony dll and the exe itself.

## How does it work?
TerrariaInjector first checks for Terraria.exe in the current dir, and then loads it into itself.
It then loads the embedded dependencies from the game. After it will load the libraries dll in the mod folder.
The default Terraria save folder is then checked, and injects the path into a field. This is a workaround for the issue with the main class now being static. Which also means that TerrariaInjector doesn't support any other save paths.
Then mods are loaded, patched and checked for Init methods which are invoked if found (if multiple are found each are invoked).
The patched game assenbly is then invoked, starting the game.


## How to create a patch/mod
Basic knowledge of HarmonyLib is required. You also need to reference both Harmony(0Harmony.dll) and Terraria(Terraria.exe). In the ExampleMod folder there is a example of how you can write a mod. This one is also in the release.
#### (version 1.1.0 and up) Init method
If you have some stuff to setup before the game starts, or just when the mod is loaded, then make one (or more) `public static void Init()` methods. They can't have any parameters.'
