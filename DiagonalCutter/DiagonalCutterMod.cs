using System;
using Core.Collections;
using Core.Localization;
using Game.Core.Research;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using UnityEngine;
using ILogger = Core.Logging.ILogger;
using OpResult = ShapeOperationDiagonalCut;
using Operation = ShapeDiagonalCutResult;
using Renderer = DiagonalCutterSimulationRenderer;
using Simulation = DiagonalCutterSimulation;
using RendererData = IDiagonalCutterDrawData;

[UsedImplicitly]
public class DiagonalCuttersMod : IMod
{
    public DiagonalCuttersMod(ILogger logger)
    {
        BuildingDefinitionGroupId groupId = new("DiagonalCutterGroup");
        BuildingDefinitionId definitionId = new("DiagonalCutter");

        string titleId = "building-variant.cutter-diagonal.title";
        string titleDescription = "building-variant.cutter-diagonal.description";

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<DiagonalCuttersMod>().SubLocator("Resources");

        using var assetBundleHelper =
            AssetBundleHelper.CreateForAssetBundleEmbeddedWithMod<DiagonalCuttersMod>("Resources/DiagonalCutter");

        var iconPath = modResourcesLocator.SubPath("DiagonalCutter_Icon.png");
        
        IBuildingGroupBuilder diagonalCutterGroup = BuildingGroup.Create(groupId)
           .WithTitle(titleId.T())
           .WithDescription(titleDescription.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(iconPath, out _))
           .AsNonTransportableBuilding()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.LinePerpendicular)
           .WithDefaultThroughputDisplayHelper();

        IBuildingConnectorData connectorData = BuildingConnectors.SingleTile()
           .AddShapeInput(ShapeConnectorConfig.DefaultInput())
           .AddShapeOutput(ShapeConnectorConfig.DefaultOutput())
           .Build();

        IBuildingBuilder diagonalCutterBuilder = Building.Create(definitionId)
           .WithConnectorData(connectorData)
           .DynamicallyRendering<Renderer, Simulation, RendererData>(new DiagonalCutterDrawData())
           .WithStaticDrawData(CreateDrawData(modResourcesLocator))
           .WithPrediction(new AtomicBuildingPredictor<Operation>(new OpResult(4), ResultingShape))
           .WithoutSound()
           .WithoutSimulationConfiguration()
           .WithEfficiencyData(new BuildingEfficiencyData(2.0f, 1));

        IPresentableUnlockableSideUpgradeBuilder sideUpgradeBuilder = SideUpgrade.New()
           .WithPresentationData(CreateSideUpgradePresentationData(titleId, titleDescription))
           .WithCost(new ResearchCostPoints(new ResearchPointCurrency(50)).AsEnumerable())
           .WithCustomRequirements(Array.Empty<ResearchMechanicId>(), Array.Empty<ResearchUpgradeId>());
        AtomicBuildings.Extend()
           .AllScenarios()
           .WithBuilding(diagonalCutterBuilder, diagonalCutterGroup)
           .UnlockedWithNewSideUpgrade(sideUpgradeBuilder)
           .WithDefaultPlacement()
           .InToolbar(ToolbarElementLocator.Root().ChildAt(0).ChildAt(2).ChildAt(^1).InsertAfter())
           .WithSimulation(new DiagonalCutterFactoryBuilder())
           .WithAtomicShapeProcessingModules(BuiltinResearchSpeed.CutterSpeed, 2.0f)
           .Build();

        return;

        ShapeCollapseResult ResultingShape(Operation result)
        {
            return result.RightSide;
        }
    }

    public void Dispose() { }

    private SideUpgradePresentationData CreateSideUpgradePresentationData(string titleId, string titleDescription)
    {
        return new SideUpgradePresentationData(
            new ResearchUpgradeId("Patience"),
            null,
            null,
            titleId.T(),
            titleDescription.T(),
            false,
            "Buildings");
    }

    private static BuildingDrawData CreateDrawData(ModFolderLocator modResourcesLocator)
    {
        string baseMeshPath = modResourcesLocator.SubPath("DiagonalCutter.fbx");
        Mesh baseMesh = FileMeshLoader.LoadSingleMeshFromFile(baseMeshPath);

        string placementHelperMeshPath = modResourcesLocator.SubPath("DiagonalCutter.fbx");
        Mesh placementHelperMesh = FileMeshLoader.LoadSingleMeshFromFile(placementHelperMeshPath);

        LOD6Mesh baseModLod = MeshLod.Create().AddLod0Mesh(baseMesh).BuildLod6Mesh();

        return new BuildingDrawData(
            renderVoidBelow: false,
            new ILODMesh[] { baseModLod, baseModLod, baseModLod },
            baseModLod,
            baseModLod,
            baseModLod.LODClose,
            new LODEmptyMesh(),
            BoundingBoxHelper.CreateBasicCollider(baseMesh),
            new DiagonalCutterDrawData(),
            false,
            null,
            false);
    }
}
