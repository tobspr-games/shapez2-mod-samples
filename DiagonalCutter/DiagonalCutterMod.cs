using Core.Localization;
using JetBrains.Annotations;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
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
        string title = "Diagonal Destroyer";
        string description = "<gl>Destroys</gl> the <gl>Even Parts</gl> of any shape.";

        TranslationBatch.Begin()
           .AddEntry(titleId, TranslationEntry.WithDefault(title))
           .AddEntry(titleDescription, TranslationEntry.WithDefault(description))
           .Flush();

        ModFolderLocator modResourcesLocator =
            ModDirectoryLocator.CreateLocator<DiagonalCuttersMod>().SubLocator("Resources");

        using var assetBundleHelper = AssetBundleHelper.CreateForAssetBundleEmbeddedWithMod<DiagonalCuttersMod>();
        var material = assetBundleHelper.LoadAsset<Material>("Assets/Materials/PortalEntrance.mat");

        IBuildingGroupBuilder diagonalCutterGroup = BuildingGroup.Create(groupId)
           .WithTitle(title.T())
           .WithDescription(description.T())
           .WithIcon(FileTextureLoader.LoadTextureAsSprite(modResourcesLocator.SubPath("DiagonalCutter_Icon.png")))
           .AsNonTransportableBuilding()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.LinePerpendicular)
           .WithDefaultThroughputDisplayHelper();

        IBuildingConnectorData connectorData = Connectors.SingleTile()
           .AddShapeInput(ShapeConnectorConfig.DefaultInput())
           .AddShapeOutput(ShapeConnectorConfig.DefaultOutput())
           .Build();

        IBuildingBuilder diagonalCutterBuilder = Building.Create(definitionId)
           .WithConnectorData(connectorData)
           .DynamicallyRendering<Renderer, Simulation, RendererData>(new DiagonalCutterDrawData())
           .WithStaticDrawData(CreateDrawData(modResourcesLocator))
           .WithPrediction(new AtomicPredictor<Operation>(new OpResult(4), ResultingShape))
           .WithoutSound()
           .WithoutSimulationConfiguration()
           .WithEfficiencyData(new BuildingEfficiencyData(2.0f, 1));

        AtomicBuildings.Extend()
           .AllScenarios()
           .WithBuilding(diagonalCutterBuilder, diagonalCutterGroup)
           .WithDefaultPlacement()
           .InToolbar(ToolbarElementLocator.Root().ChildAt(0).ChildAt(3).ChildAt(^1).InsertAfter())
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

    private static BuildingDrawData CreateDrawData(ModFolderLocator modResourcesLocator)
    {
        string baseMeshPath = modResourcesLocator.SubPath("SandboxIslands.fbx");
        Mesh baseMesh = FileMeshLoader.LoadSingleMeshFromFile(baseMeshPath);

        string placementHelperMeshPath = modResourcesLocator.SubPath("SandboxIslands.fbx");
        Mesh placementHelperMesh = FileMeshLoader.LoadSingleMeshFromFile(placementHelperMeshPath);

        LOD6Mesh baseModLod = MeshLod.Create().AddLod0Mesh(baseMesh).BuildLod6Mesh();

        return new BuildingDrawData(
            renderVoidBelow: false,
            new ILODMesh[] { baseModLod, baseModLod, baseModLod },
            baseModLod,
            baseModLod,
            baseModLod.LODClose,
            null,
            BoundingBoxHelper.CreateBasicCollider(baseMesh),
            new DiagonalCutterDrawData(),
            false,
            null,
            false);
    }
}
