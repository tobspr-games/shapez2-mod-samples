using System;
using System.Collections.Generic;
using Core.Collections;
using Core.Localization;
using Game.Content.Features.SpacePaths.IslandIO;
using Game.Core.Coordinates;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class SandboxIslandsMod : IMod
{
    public SandboxIslandsMod(ILogger logger)
    {
        // TODO: create custom island category
        // TODO: change existing sandbox category icon
        AddFluidTrash();
    }

    public void Dispose() { }

    private void AddFluidTrash()
    {
        IslandDefinitionGroupId groupId = new("FluidTrashGroup");
        IslandDefinitionId definitionId = new("FluidTrash");

        string titleId = "FluidTrash.title";
        string descriptionId = "FluidTrash.description";
        string title = "Fluid Trash";
        string description = "Disposes large amounts of fluid";

        // TranslationBatch.Begin()
        //    .AddEntry(titleId, TranslationEntry.WithDefault(title))
        //    .AddEntry(descriptionId, TranslationEntry.WithDefault(description))
        //    .Flush();

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<SandboxIslandsMod>().SubLocator("Resources");

        string iconPath = modResourcesLocator.SubPath("FluidTrash.png");

        IIslandGroupBuilder islandGroupBuilder = IslandGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(descriptionId.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(iconPath, out _))
           .AsNonTransportableIsland()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.Area);

        ChunkLayoutLookup<ChunkVector, IslandChunkData> layout = FoundationLayout();

        IIslandBuilder islandBuilder = Island.Create(definitionId)
           .WithLayout(layout)
           .WithConnectorData(FoundationConnectors(layout))
           .WithInteraction(flippable: false, canHoldBuildings: false)
           .WithDefaultChunkCost()
           .WithRenderingOptions(ChunkDrawingOptions(), drawPlayingField: true);

        AtomicIslands.Extend()
           .AllScenarios()
           .WithIsland(islandBuilder, islandGroupBuilder)
           .UnlockedAtMilestone(new ByIndexMilestoneSelector(^1))
           .WithDefaultPlacement()
           .InToolbar(ToolbarElementLocator.Root().ChildAt(5).ChildAt(4).ChildAt(^1).InsertAfter())
           .WithSimulation(new FluidTrashFactoryBuilder())
           .WithoutModules()
           .Build();
    }

    private IChunkDrawingContextProvider ChunkDrawingOptions()
    {
        return new HomogeneousChunkDrawing(ChunkPlatformDrawingContext.DrawAll());
    }

    // TODO: Create fluent API for this
    private ChunkLayoutLookup<ChunkVector, IslandChunkData> FoundationLayout()
    {
        return new ChunkLayoutLookup<ChunkVector, IslandChunkData>(ChunkData());
    }

    private IEnumerable<KeyValuePair<ChunkVector, IslandChunkData>> ChunkData()
    {
        var origin = new ChunkVector(0, 0, 0);

        IslandChunkData islandChunkData = IslandLayoutFactory.CreateIslandChunkData(
            chunkTile: origin,
            notchDirections: Array.Empty<ChunkDirection>(),
            neighborChunks: origin.AsEnumerable(),
            isBuildable: true,
            flipped: false,
            out _);

        for (int i = 0; i < islandChunkData.TileVoidFlags_L.Length; i++)
        {
            islandChunkData.TileVoidFlags_L[i] = true;
        }

        yield return new KeyValuePair<ChunkVector, IslandChunkData>(origin, islandChunkData);
    }

    private IIslandConnectorData FoundationConnectors(ChunkLayoutLookup<ChunkVector, IslandChunkData> chunkLayout)
    {
        return new IslandConnectorData(
            new[]
            {
                FluidInputConnector(ChunkDirection.East),
                FluidInputConnector(ChunkDirection.South),
                FluidInputConnector(ChunkDirection.West),
                FluidInputConnector(ChunkDirection.North)
            },
            chunkLayout.ChunkPositions);

        EntityIO<LocalChunkPivot, IIslandConnector> FluidInputConnector(ChunkDirection dir)
        {
            var chunkPivot = new LocalChunkPivot(ChunkVector.Zero, dir);
            return new EntityIO<LocalChunkPivot, IIslandConnector>(chunkPivot, new SpacePipeInputConnector());
        }
    }
}
