The Shapez 2 Example Mods solution contain small sample projects that cover the 101 of modding shapez 2

### Requirements

- Shapez2 1.0.0
- [ShapezShifter](https://github.com/tobspr-games/shapez2-modding-api) - MonoMod based Shapez2 base API
- Visual Studio (recommended) or Rider or VSCode

> For MacOS users, patching with either MonoMod, HarmonyX, MelonLoader, tModLoader, BepInEx, require running the game with Rosetta

### Installation

The projects is configured for very easy installation with Visual Studio and fairly straightforward with other IDEs. It only requires two environment variables:

1. SPZ2_PATH: Pointing to the game folder containing the managed assemblies
2. SPZ2_PERSISTENT: Should point to Unity's `Application.persistentDataPath`

On Windows, these can be set automatically by the game by running the game with the command line argument `--set-modding-env-vars`. You can also add them manually to your environment variables

On Unix, these must be set somehow. My recommendation for MacOS is using the `.zprofile` to export the variables and then opening Visual Studio from the console.

After these variables are set, it is as easy as building the solution and the mods should already be available in the game. The project will automatically link the game references, the ShapezShifter API reference and the MonoMod refs.



### Diagonal Cutter

The most complete official mod example to date. It adds a new building to the game, a diagonal cutter that destroys the odd parts of a shape. The project uses the ShapezShifter mod and highlights how to use its fluent API to create an [atomic building](https://www.notion.so/tobspr-games/Shapez-2-Modding-Documentation-2543c9e752e080a1a772c6b9ada7e462?source=copy_link#2933c9e752e0800f9a49dc6af5fa4821) while covering how to:

- Add a new building and building group to the current scenario game data
- Add a new specialized stateful simulation that can cutter the diagonals of a shape
- Add a new simulation system to the simulation loop that pattern matches for the building and creates the simulation
- Add a new placer for the building
- Add a new toolbar entry for the placer
- Add a new set of modules to be displayed by the HUD when the building is selected
- Load a custom .FBX model and using it for the static rendering
- Add new translation entries for the building
- Add a new dynamic renderer for rendering the building current state
- Add progression requirements to unlock the building



### Sandbox Islands

This mod is very similar to the Diagonal Cutter in the sense that it highlights how to add an entity (in this case an island) with simulation. The mod adds a new island to the game for discarding paint conveniently. Similarly to the diagonal cutter it also uses the ShapezShifter mod and demonstrate how to extend the game data, research, simulation systems, placement system, toolbar, localization and rendering



### Bigger Foundations

This mod example also shows how to add an island, but focusing in the data requirements for creating new foundation platforms  