using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Collections;
using Core.Localization;
using Game.Core.Coordinates;
using Game.Core.Rendering.MeshGeneration;
using JetBrains.Annotations;
using ShapezShifter;
using ShapezShifter.Flow;
using ShapezShifter.Hijack;
using ShapezShifter.Kit;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class PortalsMod : IMod
{
    private readonly List<RewirerHandle> ExtenderHandles = new();

    private readonly PortalBuildingsExtender PortalBuildingsExtender;

    public PortalsMod(ILogger logger)
    {
        PortalBuildingsExtender = new PortalBuildingsExtender(logger);
        RewirerHandle handle = GameRewirers.AddRewirer(PortalBuildingsExtender);
        ExtenderHandles.Add(handle);
    }

    public void Dispose()
    {
        foreach (RewirerHandle handle in ExtenderHandles)
        {
            GameRewirers.RemoveRewirer(handle);
        }

        PortalBuildingsExtender.Dispose();
        ExtenderHandles.Clear();
    }
}

public class PortalBuildingsExtender
    : IShapeBuildingPlacementRewirers,
      IToolbarDataRewirer,
      IBuildingsRewirer,
      IBuildingModulesRewirer,
      IGameScenarioRewirer,
      IDisposable
{
    private readonly ILogger Logger;
    private readonly BuildingDefinitionGroupId PortalReceiverGroupId = new("Portal_ReceiverGroup");

    private readonly BuildingDefinitionId PortalReceiverId = new("Portal_Receiver");
    private readonly AssetBundle PortalResources;

    private readonly BuildingDefinitionGroupId PortalSenderGroupId = new("Portal_SenderGroup");

    private readonly BuildingDefinitionId PortalSenderId = new("Portal_Sender");
    private BuildingsModulesLookup ModulesLookup;

    private BuildingDefinitionGroup PortalReceiverGroup;
    private AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId> PortalReceiverRuleProcessor;
    private BuildingDefinitionGroup PortalSenderGroup;
    private AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId> PortalSenderRuleProcessor;
    private PlacementInitiatorId? ReceiverPlacementInitiatorId;
    private PlacementInitiatorId? SenderPlacementInitiatorId;

    public PortalBuildingsExtender(ILogger logger)
    {
        Logger = logger;
        string basePath = Path.GetDirectoryName(typeof(PortalBuildingsExtender).Assembly.Location);
        string resourcesPath = Path.Combine(basePath, "Resources", "Portal");
        PortalResources = AssetBundle.LoadFromFile(resourcesPath);
    }

    public void Dispose()
    {
        PortalSenderRuleProcessor?.Dispose();
    }

    public void AddModules(BuildingsModulesLookup modulesLookup)
    {
        ModulesLookup = modulesLookup;
        modulesLookup.AddModule(PortalSenderId, PortalSenderGroup.Definitions[0], new NoBuildingModules());
        modulesLookup.AddModule(PortalReceiverId, PortalReceiverGroup.Definitions[0], new NoBuildingModules());
    }

    public GameBuildings ModifyGameBuildings(
        MetaGameModeBuildings metaBuildings,
        GameBuildings gameBuildings,
        IMeshCache meshCache,
        VisualThemeBaseResources theme)
    {
        Logger.Info?.Log($"Modifying Game Buildings. Object reference: {gameBuildings.GetRefId()}");
        if (gameBuildings._DefinitionsById.ContainsKey(PortalSenderId))
        {
            return gameBuildings;
        }

        PortalSenderGroup = BuildingGroup.Create(PortalSenderGroupId)
           .WithTitle(new RawText("Portal Sender"))
           .WithDescription(new RawText("Sends a shape through a portal"))
           .WithIcon(CreateMockSprite(32, 32, Color.red))
           .AsNonTransportableBuilding()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
           .BuildAndRegister(gameBuildings);

        PortalReceiverGroup = BuildingGroup.Create(PortalReceiverGroupId)
           .WithTitle(new RawText("Portal Receiver"))
           .WithDescription(new RawText("Receives a shape through a portal"))
           .WithIcon(CreateMockSprite(32, 32, Color.green))
           .AsNonTransportableBuilding()
           .WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
           .BuildAndRegister(gameBuildings);

        BuildingItemInput senderInput = new()
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.West
        };

        BuildingItemOutput receiverOutput = new()
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.East
        };

        TileVector[] tiles = { TileVector.Zero };

        LocalTileBounds tileBounds = LocalTileBounds.From_SLOW_DO_NOT_USE(tiles);
        TileDimensions tileDimensions = tileBounds.Dimensions;
        LocalVector center = LocalVector.Lerp((LocalVector)tileBounds.Min, (LocalVector)tileBounds.Max, 0.5f);

        IBuildingConnectorData senderConnectorData = new BuildingConnectorData(
            senderInput.AsEnumerable(),
            tiles,
            tileBounds,
            center,
            tileDimensions);

        IBuildingConnectorData receiverConnectorData = new BuildingConnectorData(
            receiverOutput.AsEnumerable(),
            tiles,
            tileBounds,
            center,
            tileDimensions);

        BuildingDefinition portalSender = CreateDefinition(
            PortalSenderId,
            senderConnectorData,
            PortalSenderGroup,
            gameBuildings.BeltPortSender);
        PortalSenderGroup.AddInternalVariant(portalSender);

        BuildingDefinition portalReceiver = CreateDefinition(
            PortalReceiverId,
            receiverConnectorData,
            PortalReceiverGroup,
            gameBuildings.BeltPortReceiver);
        PortalReceiverGroup.AddInternalVariant(portalReceiver);

        gameBuildings._DefinitionsById.Add(PortalSenderId, portalSender);
        gameBuildings._DefinitionsById.Add(PortalReceiverId, portalReceiver);

        return gameBuildings;
    }

    public ToolbarData ModifyToolbarData(ToolbarData toolbarData)
    {
        if (!SenderPlacementInitiatorId.HasValue || !ReceiverPlacementInitiatorId.HasValue)
        {
            throw new Exception("Expected placement initiator extension to be executed before toolbar one");
        }

        // Todo: Add XML-like queries for toolbar elements
        var shapeBuildings = (ParentToolbarElementData)toolbarData.RootToolbarElement.Children[0];
        var transportGroup = (ParentToolbarElementData)shapeBuildings.Children[0];
        List<IToolbarElementData> elements = transportGroup.Children.ToList();
        var belt = (IPresentableToolbarElementData)elements.First();

        LazyLocalizedText title = new(new TranslationId(belt.Title.ToString()));
        LazyLocalizedText description = new(new TranslationId(belt.Description.ToString()));

        var icon = PortalResources.LoadAsset<Sprite>("Assets/Icons/PortalIcon.png");

        PlacementToolbarElementData sender = new(title, description, SenderPlacementInitiatorId.Value, icon);
        PlacementToolbarElementData receiver = new(title, description, ReceiverPlacementInitiatorId.Value, icon);
        elements.Add(sender);
        elements.Add(receiver);
        transportGroup.Children = elements.ToArray();
        return toolbarData;
    }

    public void ModifyBuildingPlacers(BuildingInitiatorsParams @params, IPlacementInitiatorIdRegistry placementRegistry)
    {
        CreatePlacementInitiator(@params.EntityPlacementRunner, placementRegistry);
    }

    public GameScenario ModifyGameScenario(GameScenario gameScenario)
    {
        gameScenario.Progression.Levels[^1].Rewards = gameScenario.Progression.Levels[^1]
           .Rewards.Append(new ResearchRewardBuildingGroup(PortalSenderGroupId))
           .Append(new ResearchRewardBuildingGroup(PortalReceiverGroupId))
           .ToList();
        return gameScenario;
    }

    private BuildingDefinition CreateDefinition(
        BuildingDefinitionId id,
        IBuildingConnectorData connectorData,
        IBuildingDefinitionGroup definitionGroup,
        IBuildingDefinition blueprint)
    {
        BuildingDefinition runtimeDefinition = new(id, connectorData);

        runtimeDefinition.CustomData.Attach(definitionGroup.DefaultPreferredPlacementMode);

        runtimeDefinition.CustomData.Attach(new EntityPlacementPreferenceData(false, 100));

        runtimeDefinition.CustomData.Attach(connectorData);

        runtimeDefinition.CustomData.Attach(
            new EntityReplacementPreferenceData(
                definitionGroup.AllowNonForcingReplacementByOtherBuildings,
                definitionGroup.IsTransportBuilding,
                definitionGroup.ShouldSkipReplacementIOChecks));

        runtimeDefinition.CustomData.Attach(new NoOutputPredictor());

        // Attach the parent group
        runtimeDefinition.CustomData.Attach(definitionGroup);

        // Attach the draw data
        runtimeDefinition.CustomData.Attach(blueprint.CustomData.Get<IBuildingDrawData>());

        runtimeDefinition.CustomData.Attach(blueprint.CustomData.Get<IBuildingSoundDefinition>());

        runtimeDefinition.CustomData.Attach(new EmptyCustomSimulationConfiguration());

        BuildingEfficiencyData efficiencyData = new(1, 1);
        runtimeDefinition.CustomData.Attach(efficiencyData);

        return runtimeDefinition;
    }

    private void CreatePlacementInitiator(
        IEntityPlacementRunner placementRunner,
        IPlacementInitiatorIdRegistry placementRegistry)
    {
        IBuildingDefinition portalSenderDefinition = PortalSenderGroup.Definitions[0];
        IBuildingDefinition portalReceiverDefinition = PortalReceiverGroup.Definitions[0];

        PlacerDataBasedOnRepresentingBuilding placerData = new(portalSenderDefinition, ModulesLookup);
        ModularEntityPlacer senderEntityPlacer = new(
            new SinglePlacer<GlobalTileTransform, GlobalTileCoordinate, TileVector, TileDirection, GlobalTilePivot,
                LocalTilePivot, TileAxis, BuildingInstanceModel, BuildingConnector, BuildingConnection>(
                portalSenderDefinition,
                new BuildingPlacementAdapter(),
                new BuildingAccessorAdapter(),
                new BuildingMapLayoutRegisterAdapter()),
            placerData);

        ModularEntityPlacer receiverEntityPlacer = new(
            new SinglePlacer<GlobalTileTransform, GlobalTileCoordinate, TileVector, TileDirection, GlobalTilePivot,
                LocalTilePivot, TileAxis, BuildingInstanceModel, BuildingConnector, BuildingConnection>(
                portalReceiverDefinition,
                new BuildingPlacementAdapter(),
                new BuildingAccessorAdapter(),
                new BuildingMapLayoutRegisterAdapter()),
            placerData);

        PortalSenderRuleProcessor = new AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId>(
            GameHelper.Core.Research.Progress,
            PortalSenderGroup.Id,
            new BuildingResearchLockStatusSolver(),
            new BuildingRewardIdSolver());

        PortalReceiverRuleProcessor = new AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId>(
            GameHelper.Core.Research.Progress,
            PortalSenderGroup.Id,
            new BuildingResearchLockStatusSolver(),
            new BuildingRewardIdSolver());

        IPlacementInitiator portalSender = new GamePlacementInitiator(
            PortalSenderRuleProcessor,
            senderEntityPlacer,
            placementRunner);

        IPlacementInitiator portalReceiver = new GamePlacementInitiator(
            PortalReceiverRuleProcessor,
            receiverEntityPlacer,
            placementRunner);

        SenderPlacementInitiatorId =
            placementRegistry.RegisterInitiator("PortalSenderPlacementInitiator", portalSender);
        ReceiverPlacementInitiatorId = placementRegistry.RegisterInitiator(
            "PortalReceiverPlacementInitiator",
            portalReceiver);
    }

    private static Sprite CreateMockSprite(int width, int height, Color color)
    {
        // Create and fill the texture
        Texture2D texture = new(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Create and return the sprite
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}

public class NoBuildingModules : IBuildingModules
{
    public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IBuildingDefinition definition)
    {
        yield break;
    }

    public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IMapModel map, BuildingModel building)
    {
        yield break;
    }
}
