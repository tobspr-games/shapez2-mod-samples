using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Very simple modding example. Hooks to game start and modify stars material colors
/// </summary>
public class RainbowStarsMod : IMod
{
    public void Init(string path)
    {
        ShapezCallbackExt.OnPostGameStart += OnGameStart;
    }

    private static void OnGameStart()
    {
        if (!(GameCore.G.Theme is SpaceTheme spaceTheme))
        {
            throw new Exception("Theme is not SpaceTheme!?");
        }
        PatchResources(spaceTheme.ThemeResources.BackgroundStars);
    }

    private static readonly int MainColor = Shader.PropertyToID("_StarColor");
    private const int COLORS = 8;

    public ModMetadata Metadata => new ModMetadata("Rainbow Stars", "lorenzofman", "0.0.1");
   
    private static void PatchResources(SpaceThemeBackgroundStars.ExtraResources resources)
    {
        var sampleMaterial = resources.StarMaterial[0];
        resources.StarMaterial = new Material[COLORS];

        for (var i = 0; i < COLORS; i++)
        {
            resources.StarMaterial[i] = Object.Instantiate(sampleMaterial);
            resources.StarMaterial[i].SetColor(MainColor, Color.HSVToRGB((float)i / COLORS, 1, 1, false));
        }
    }
}
