using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Localization;
using Game.Core.Coordinates;
using Game.Core.Rendering.MeshGeneration;
using ShapezShifter;
using ShapezShifter.Buildings;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

public class DiagonalCutterBuildingsExtender
    : IShapeBuildingsPlacementInitiatorsExtender,
      IToolbarDataExtender,
      IBuildingsExtender,
      IBuildingModulesExtender,
      IGameScenarioExtender,
      ISimulationSystemsExtender,
      IBuffablesExtender,
      IDisposable
{
    private readonly BuildingDefinitionGroupId DiagonalCutterGroupId = new("DiagonalCutterGroup");

    private readonly BuildingDefinitionId DiagonalCutterId = new("DiagonalCutter");

    // private readonly AssetBundle DiagonalCutterResources;

    private readonly ILogger Logger;

    private BuildingDefinitionGroup DiagonalCutterGroup;
    private PlacementInitiatorId? DiagonalCutterPlacementInitiatorId;
    private BuildingsModulesLookup ModulesLookup;
    private DiagonalCutterConfiguration BuffableConfiguration;

    public DiagonalCutterBuildingsExtender(ILogger logger)
    {
        Logger = logger;

        // string basePath = Path.GetDirectoryName(typeof(DiagonalCutterBuildingsExtender).Assembly.Location);
        // string resourcesPath = Path.Combine(basePath, "Resources", "DiagonalCutter");
        // DiagonalCutterResources = AssetBundle.LoadFromFile(resourcesPath);
    }

    public void Dispose() { }

    public void AddModules(BuildingsModulesLookup modulesLookup)
    {
        ModulesLookup = modulesLookup;

        var speed = new ResearchSpeedId("CutterSpeed");
        modulesLookup.AddModule(
            DiagonalCutterId,
            DiagonalCutterGroup.Definitions[0],
            new ItemSimulationBuildingModuleDataProvider(speed, 2.0f));
    }

    public GameBuildings ModifyGameBuildings(
        MetaGameModeBuildings metaBuildings,
        GameBuildings gameBuildings,
        IMeshCache meshCache,
        VisualThemeBaseResources theme)
    {
        DiagonalCutterGroup = BuildingHelper.CreateBuildingGroup(
            DiagonalCutterGroupId.Id,
            LoadIcon(),
            "building-variant.cutter-diagonal.title".T(),
            "building-variant.cutter-diagonal.description".T(),
            false,
            DefaultPreferredPlacementMode.LinePerpendicular,
            throughputDisplayHelper: new MetaBuildingThroughputDisplayHelper
            {
                LockToFirstDefinition = false,
                MeshSpacing = -0.1f,
                Slots = Array.Empty<MetaBuildingThroughputDisplayHelper.IOData>()
            });

        BuildingItemInput input = new()
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.West
        };

        BuildingItemOutput output = new()
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.East
        };

        var tiles = new[] { TileVector.Zero };

        LocalTileBounds tileBounds = LocalTileBounds.From_SLOW_DO_NOT_USE(tiles);
        TileDimensions tileDimensions = tileBounds.Dimensions;
        LocalVector center = LocalVector.Lerp((LocalVector)tileBounds.Min, (LocalVector)tileBounds.Max, 0.5f);

        IBuildingConnectorData connectorData = new BuildingConnectorData(
            new BuildingBaseIO[] { input, output },
            tiles,
            tileBounds,
            center,
            tileDimensions);

        IBuildingDefinition halfCutter =
            gameBuildings.GetVariant(gameBuildings.HalfCutterDefinitionGroupId).Definitions[0];

        BuildingDefinition diagonalCutter = CreateDefinition(
            DiagonalCutterId,
            connectorData,
            DiagonalCutterGroup,
            halfCutter.CustomData.Get<IBuildingDrawData>());
        DiagonalCutterGroup.AddInternalVariant(diagonalCutter);

        gameBuildings._All.Add(DiagonalCutterGroup);

        gameBuildings._DefinitionsById.Add(DiagonalCutterId, diagonalCutter);

        gameBuildings._VariantsById.Add(DiagonalCutterGroupId, DiagonalCutterGroup);

        return gameBuildings;
    }

    public void ExtendSimulationSystems(
        ICollection<ISimulationSystem> simulationSystems,
        SimulationSystemsDependencies dependencies)
    {
        simulationSystems.Add(CreateDiagonalCutterSystem(dependencies));
    }

    public ToolbarData ModifyToolbarData(ToolbarData toolbarData)
    {
        if (!DiagonalCutterPlacementInitiatorId.HasValue)
        {
            throw new Exception("Expected placement initiator extension to be executed before toolbar one");
        }

        // Todo: Add XML-like queries for toolbar elements
        var shapeBuildings = (ParentToolbarElementData)toolbarData.RootToolbarElement.Children[0];

        // Dividers are also elements, that is why the index here is three
        var cuttersGroup = (ParentToolbarElementData)shapeBuildings.Children[3];
        var elements = cuttersGroup.Children.ToList();

        var title = new LazyLocalizedText(new TranslationId("building-variant.cutter-diagonal.title"));
        var description = new LazyLocalizedText(new TranslationId("building-variant.cutter-diagonal.description"));

        PlacementToolbarElementData cutterPlacer = new(
            title,
            description,
            DiagonalCutterPlacementInitiatorId.Value,
            LoadIcon());
        elements.Add(cutterPlacer);
        cuttersGroup.Children = elements.ToArray();
        return toolbarData;
    }

    public void ExtendBuildingInitiators(
        BuildingInitiatorsParams @params,
        IPlacementInitiatorIdRegistry placementRegistry)
    {
        CreatePlacementInitiator(@params, placementRegistry);
    }

    public GameScenario ExtendGameScenario(GameScenario gameScenario)
    {
        gameScenario.Progression.Levels[^1].Rewards = gameScenario.Progression.Levels[^1]
                                                                  .Rewards.Append(
                                                                       new ResearchRewardBuildingGroup(
                                                                           DiagonalCutterGroupId))
                                                                  .ToList();
        return gameScenario;
    }

    public ICollection<object> ExtendBuffables(ICollection<object> buffables)
    {
        buffables.Add(BuffableConfiguration);
        return buffables;
    }

    private BuildingDefinition CreateDefinition(
        BuildingDefinitionId id,
        IBuildingConnectorData connectorData,
        IBuildingDefinitionGroup definitionGroup,
        IBuildingDrawData drawDataReference)
    {
        BuildingDefinition runtimeDefinition = new(id, connectorData);

        runtimeDefinition.CustomData.Attach(definitionGroup.DefaultPreferredPlacementMode);

        runtimeDefinition.CustomData.Attach(new EntityPlacementPreferenceData(true, 100));

        runtimeDefinition.CustomData.Attach(connectorData);

        runtimeDefinition.CustomData.Attach(
            new EntityReplacementPreferenceData(
                definitionGroup.AllowNonForcingReplacementByOtherBuildings,
                definitionGroup.IsTransportBuilding,
                definitionGroup.ShouldSkipReplacementIOChecks));

        runtimeDefinition.CustomData.Attach(new DiagonalCutterOutputPredictor(new ShapeOperationDiagonalCut(4)));

        // Attach the parent group
        runtimeDefinition.CustomData.Attach(definitionGroup);

        var customDrawData = new DiagonalCutterDrawData();

        // Attach the draw data copying most of them from a reference (otherwise we need to setup way too many meshes)
        var drawData = new BuildingDrawData(
            false,
            drawDataReference.MainMeshPerLayer,
            drawDataReference.IsolatedBlueprintMesh,
            drawDataReference.CombinedBlueprintMesh,
            drawDataReference.PreviewMesh,
            drawDataReference.GlassMesh,
            drawDataReference.Colliders,
            new DiagonalCutterDrawData(),
            false,
            null,
            false);
        runtimeDefinition.CustomData.Attach(drawData);
        runtimeDefinition.CustomData.Attach(customDrawData);

        runtimeDefinition.CustomData.Attach(new BuildingSoundDefinition(SoundLOD.None, SoundPriority.Disabled, null));

        runtimeDefinition.CustomData.Attach(new EmptyCustomSimulationConfiguration());

        BuildingEfficiencyData efficiencyData = new(2.0f, 1);
        runtimeDefinition.CustomData.Attach(efficiencyData);

        return runtimeDefinition;
    }

    private void CreatePlacementInitiator(
        BuildingInitiatorsParams buildingInitiatorsParams,
        IPlacementInitiatorIdRegistry placementRegistry)
    {
        IBuildingDefinition buildingDefinition = DiagonalCutterGroup.Definitions[0];

        // TODO: Add creator to the extender interface
        var buildingsCreator = new ShapeBuildingsPlacersCreator(
            buildingInitiatorsParams.Buildings,
            buildingInitiatorsParams.ProgressManager,
            buildingInitiatorsParams.EntityPlacementRunner,
            buildingInitiatorsParams.BuildingsModules,
            buildingInitiatorsParams.PipetteMap,
            (ITutorialState)buildingInitiatorsParams.TutorialState,
            buildingInitiatorsParams.ViewportLayerController);

        IPlacementInitiator diagonalCutter = buildingsCreator.CreateDefaultPlacer(buildingDefinition);

        DiagonalCutterPlacementInitiatorId = placementRegistry.RegisterInitiator(
            "DiagonalCutterPlacementInitiator",
            diagonalCutter);
    }

    private static Sprite LoadIcon()
    {
        string basePath = Path.GetDirectoryName(typeof(DiagonalCuttersMod).Assembly.Location);
        string resourcesPath = Path.Combine(basePath, "Resources", "DiagonalCutter_Icon.png");
        byte[] data = File.ReadAllBytes(resourcesPath);

        var texture = new Texture2D(2, 2);
        texture.LoadImage(data);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private ISimulationSystem CreateDiagonalCutterSystem(SimulationSystemsDependencies dependencies)
    {
        var diagonalCut = new ShapeOperationDiagonalCut(dependencies.Mode.MaxShapeLayers);

        IBuildingDefinition definition = DiagonalCutterGroup.Definitions[0];

        DiagonalCutterConfiguration configuration = new(
            BuffableBeltSpeed.DiscreteSpeed.OneSecondPerTile,
            BuffableBeltDelay.DiscreteDuration.OnePointFiveSeconds,
            new ResearchSpeedId("CutterSpeed"));
        BuffableConfiguration = configuration;
        DiagonalCutterSimulationFactory factory = new(configuration, dependencies.ShapeRegistry, diagonalCut);

        return new AtomicStatefulBuildingSimulationSystem<DiagonalCutterSimulation, DiagonalCutterSimulationState>(
            factory,
            definition.Id);
    }
}
