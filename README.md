### Requirements

- Shapez2 Alpha 7.5+
- [ShapezShifter](https://github.com/tobspr-games/shapez2-modding-api) - MonoMod based Shapez2 base API
- Visual Studio (recommended) or Rider or VSCode

> For MacOS users, patching with either MonoMod, HarmonyX, MelonLoader, tModLoader, BepInEx, require running the game with Rosetta

### Installation

The projects is configured for very easy installation with Visual Studio and fairly straightforward with other IDEs. It only requires two environment variables:

1. SPZ2_PATH: Pointing to the game folder containing the managed assemblies
2. SPZ2_PERSISTENT: Should point to Unity's `Application.persistentDataPath`

On Windows, these are automatically set when you play the game. You can also add them manually

On Unix, these must be set somehow. My recommendation for MacOS is using the `.zprofile` to export the variables and then opening Visual Studio from the console.

After these variables are set, it is as easy as building the solution and the mods should already be available in the game. The project will automatically link the game references, the ShapezShifter API reference and the MonoMod refs.

### Disclaimer

These mods are only a proof of concept. We plan on creating an extended API and 99% of these will probably change. They lack dependency handling, unloading, packing and much more. These are not guidelines for how mods should be implemented, but rather a proof of concept for cross-platform modding



### Mods

- BusyDev: Skips preload for faster iterations
- InfiniteIslands: Increase the maximum number of Islands (using reflection)
- InfiniteLayers: Increase the number of layers from 3 to 100 (by patching)
- PortalBuilding: Demonstrates how to add a new building to the game. The portal itself is very glitchy, but the core concepts are here. Uses Unity's asset bundles (from a brand new project) to add models, textures, shaders and materials to the game.
- RainbowStars: Changes stars variety (fast check to determine if the mods were loaded)