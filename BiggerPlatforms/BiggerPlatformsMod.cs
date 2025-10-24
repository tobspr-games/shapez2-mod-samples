using System;
using System.Collections.Generic;
using System.Linq;
using Core.Collections.Scoped;
using Core.Localization;
using Game.Core.Coordinates;
using Game.Core.Research;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using Unity.Mathematics;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class BiggerPlatformsMod : IMod
{
    public BiggerPlatformsMod(ILogger logger)
    {
        AddFoundation(5, 1);
        AddFoundation(6, 1);

        AddFoundation(4, 4);
        AddFoundation(5, 5);
        AddFoundation(6, 6);

        // Platforms bigger than 6x6 cause issues because the foundation mesh cannot be baked into a single mesh (it
        // exceeds 65536 vertices)
        // AddFoundation(7, 7);
    }

    public void Dispose() { }

    private void AddFoundation(int width, int height)
    {
        string suffix = $"{width}x{height}";
        IslandDefinitionGroupId groupId = new($"FoundationGroup_{suffix}");
        IslandDefinitionId definitionId = new($"Foundation_{suffix}");

        string titleId = $"Foundation{suffix}.title";
        string descriptionId = "island-layout.Layout_GenericPlatform.description";

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<BiggerPlatformsMod>().SubLocator("Resources");

        string icon = modResourcesLocator.SubPath($"Foundation_{suffix}.png");

        IIslandGroupBuilder islandGroupBuilder = IslandGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(descriptionId.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(icon, out _))
           .AsNonTransportableIsland()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.Area);

        ChunkLayoutLookup<ChunkVector, IslandChunkData> layout = FoundationLayout(width, height);

        IIslandBuilder islandBuilder = Island.Create(definitionId)
           .WithLayout(layout)
           .WithConnectorData(FoundationConnectors(layout))
           .WithInteraction(flippable: false, canHoldBuildings: true)
           .WithDefaultChunkCost()
           .WithRenderingOptions(ChunkDrawingOptions(), drawPlayingField: true);

        IToolbarElementLocator platformsGroup = ToolbarElementLocator.Root().ChildAt(5);
        IToolbarElementLocator lineFoundations = platformsGroup.ChildAt(4);
        IToolbarElementLocator rectFoundations = platformsGroup.ChildAt(5);

        IToolbarEntryInsertLocation toolbarEntryLocation = width == 1 || height == 1
            ? lineFoundations.ChildAt(^1).InsertAfter()
            : rectFoundations.ChildAt(^1).InsertAfter();

        AtomicIslands.Extend()
           .AllScenarios()
           .WithIsland(islandBuilder, islandGroupBuilder)
           .UnlockedAtMilestone(new ByIdMilestoneSelector(new ResearchUpgradeId("RNIslandBuilding")))
           .WithDefaultPlacement()
           .InToolbar(toolbarEntryLocation)
           .WithoutSimulation()
           .WithoutModules()
           .Build();
    }

    private IChunkDrawingContextProvider ChunkDrawingOptions()
    {
        return new HomogeneousChunkDrawing(ChunkPlatformDrawingContext.DrawAll());
    }

    // TODO: Create fluent API for this
    private ChunkLayoutLookup<ChunkVector, IslandChunkData> FoundationLayout(int width, int height)
    {
        return new ChunkLayoutLookup<ChunkVector, IslandChunkData>(Chunks(width, height));
    }

    private IEnumerable<KeyValuePair<ChunkVector, IslandChunkData>> Chunks(int width, int height)
    {
        KeyValuePair<ChunkVector, ChunkDirection[]>[] allChunks = ChunksData(width, height).ToArray();

        foreach (KeyValuePair<ChunkVector, ChunkDirection[]> kv in allChunks)
        {
            yield return new KeyValuePair<ChunkVector, IslandChunkData>(
                kv.Key,
                IslandLayoutFactory.CreateIslandChunkData(
                    kv.Key,
                    kv.Value,
                    allChunks.Select(x => x.Key).ToArray(),
                    true,
                    false,
                    out _));
        }
    }

    private static IEnumerable<KeyValuePair<ChunkVector, ChunkDirection[]>> ChunksData(int width, int height)
    {
        var start = new int2((width - 1) / -2, (height - 1) / -2);
        var end = new int2(width / 2, height / 2);

        using ScopedHashSet<ChunkVector> chunks = ScopedHashSet<ChunkVector>.Get();

        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                chunks.Add(new ChunkVector(x, y, 0));
            }
        }

        foreach (ChunkVector chunk in chunks)
        {
            using ScopedList<ChunkDirection> notchDirections = ScopedList<ChunkDirection>.Get();
            ComputeExternalNotches(chunk, chunks, notchDirections);
            yield return new KeyValuePair<ChunkVector, ChunkDirection[]>(chunk, notchDirections.ToArray());
        }
    }

    private static void ComputeExternalNotches(
        ChunkVector chunk,
        ISet<ChunkVector> chunks,
        ICollection<ChunkDirection> notchDirections)
    {
        foreach (GridRotation rotation in GridRotation.RotationsInClockwiseOrder)
        {
            var dir = rotation.ToChunkDirection();
            ChunkVector neighbor = chunk + dir;
            if (!chunks.Contains(neighbor))
            {
                notchDirections.Add(dir);
            }
        }
    }

    // TODO: Create fluent API for this
    private IIslandConnectorData FoundationConnectors(ChunkLayoutLookup<ChunkVector, IslandChunkData> chunkLayout)
    {
        return new IslandConnectorData(
            Array.Empty<EntityIO<LocalChunkPivot, IIslandConnector>>(),
            chunkLayout.ChunkPositions);
    }
}
