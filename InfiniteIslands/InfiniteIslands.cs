using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using UnityEngine;

public class InfiniteIslands : IMod
{
    public ModMetadata Metadata => new ModMetadata("Infinite Islands", "lorenzofman", "1.0.0");

    public void Init(string path)
    {
        ShapezCallbackExt.OnPostGameStart += OnGameStart;
        // Note: this is not ideal because we need to rely on string lookups to find the method name
        // One idea would be to have an automated program create public type definitions for every class in the project
        // Similar to a .h which holds all definitions, and then some utility function to actually get the correct reflected type
        var method = typeof(ResearchChunkLimitManager).GetMethod("ComputeChunkLimit", BindingFlags.Instance | BindingFlags.NonPublic);

        if (method == null)
        {
            throw new Exception();
        }
        new Hook(method, typeof(InfiniteIslands).GetMethod(nameof(Nothing)));
    }

    public static void Nothing(ResearchChunkLimitManager researchChunkLimitManager)
    {
        Debug.Log("ok");
    }

    private void OnGameStart()
    {
        var propertyInfo = typeof(ResearchChunkLimitManager).GetProperty("CurrentChunkLimit");
        propertyInfo.SetValue(GameCore.G.Research.ChunkLimitManager, Convert.ChangeType(999999, propertyInfo.PropertyType), null);
    }
}
