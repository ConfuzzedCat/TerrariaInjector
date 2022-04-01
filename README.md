![alt text](https://github.com/ConfuzzedCat/TerrariaInjector/blob/master/TerrariaInjector_Long.png?raw=true)
## TerrariaInjector
#### What is it?
TerrariaInjector is a program to inject harmony patches in to Terraria.

#### How to install?
Simply put **TerrariaInjector.exe** into your terraria installation folder and run it. 
Because of how it works, you will need to run the injector everytime you want to play with mods/patches.

#### How to uninstall?
Simply rename the injector to either "uninstall" or "remove". This will delete the  Mods folder, the extracted resources(see more at how it works), the Harmony dll and the exe itself.

#### How does it work?
The injecting can be put into x steps.
1. Finding and loading the Terraria.exe
This is not done in an advance method, but it does support drag and drop. 
2. Checking for game dependencies.
   This is need, because when the injector runs the game, it is running the method "Program.LaunchGame" which isn't the first method that will run normally, but because I couldn't get to run that method, I'm doing this way instead.
3. Extracting dependencies.
In this step, it will simply look through "Terraria.exe" for any embeded dll files, and rename them(only the ones I know about, so this method isn't futureproof). This step doesn't remove and embeded files from the .exe, only copying them out. This step is skipped over if all dependencies is found. It also only extracts the ones missing.
4. Patching and launching.
   in this last step, the injector will load all assemblies and try to patch them with Harmony to the loaded Terraria.exe (from step 1) and call the LaunchGame method from the loaded exe.

#### How to create a patch/mod
Basic knowledge of HarmonyLib is required. You also need to reference both Harmony(0Harmony.dll) and Terraria(Terraria.exe). In the ExampleMod folder there is a example of how you can write a mod. This one is also in the release.
