using System;
using JetBrains.Annotations;
using ShapezShifter;
using UnityEngine;
using ILogger = Core.Logging.ILogger;
using Object = UnityEngine.Object;

/// <summary>
///     Very simple modding example. Hooks to game start and modify stars material colors
/// </summary>
[UsedImplicitly]
public class RainbowStarsMod : IMod
{
    private const int Colors = 8;

    private static readonly int MainColor = Shader.PropertyToID("_StarColor");

    public RainbowStarsMod(ILogger logger)
    {
        // ShapezCallbackExt.OnPostGameStart.Register(PatchStars);
    }

    public void Dispose() { }

    private static void PatchStars()
    {
        GameCore gameCore = GameHelper.Core;

        if (gameCore.Theme is SpaceTheme spaceTheme)
        {
            // Avoid writing to the original Scriptable Object
            SpaceThemeBackgroundStarsResources resourcesCopy = spaceTheme.ThemeResources.BackgroundStars.DeepCopy();
            PatchResources(spaceTheme.ThemeResources.BackgroundStars);
            spaceTheme.ThemeResources.BackgroundStars = resourcesCopy;
        }
        else
        {
            throw new Exception("Theme is not SpaceTheme!?");
        }
    }

    private static void PatchResources(SpaceThemeBackgroundStarsResources resources)
    {
        Material sampleMaterial = resources.StarMaterial[0].GetMaterialInternal();
        resources.StarMaterial = new MaterialReference[Colors];

        for (int i = 0; i < Colors; i++)
        {
            Material materialCopy = Object.Instantiate(sampleMaterial);
            materialCopy.SetColor(MainColor, Color.HSVToRGB((float)i / Colors, 1, 1, false));

            // resources.StarMaterial[i] = new MaterialReference(null) { _Material = materialCopy };
        }
    }
}
