using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Linq;

public class InfiniteLayers : IMod
{
    public ModMetadata Metadata => new ModMetadata("Infinite Layers", "Shapez2 Team", "1.0.0");
    private const int BaseLayers = 2;
    private const int Layers = 100;
    private static bool Patched = false;
    public void Init()
    {
        ShapezCallbackExt.OnPreGameStart += OnPreGameStart;
    }

    private void OnPreGameStart()
    {
        // This is called before each game loading, but we only need to execute one time (yes, it needs a officially supported callback)
        if (Patched)
        {
            return;
        }

        Patched = true;

        // Right now, there is no hook between loading the resources and using them to load the map. 
        // That's a great candidate for a better API, but without changing the target application, we can
        // hijack the layers before they are used by any game loader (this must be done to make sure that
        // the tile map has the approapriate size)
        var layers = Globals.Resources.GameModes.First().LayerUnlocks;
        for (int i = BaseLayers; i < Layers; i++)
        {
            // We add layer3 here as if it was all the other layers, so that the research unlocks first the second layer
            // and then the rest altogether.
            layers.Add(layers[BaseLayers - 1]);
        }
    }
}
