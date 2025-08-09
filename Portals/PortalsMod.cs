using System;
using System.Collections.Generic;
using System.Linq;
using Core.Collections;
using Core.Localization;
using Game.Core.Coordinates;
using Game.Core.Rendering.MeshGeneration;
using JetBrains.Annotations;
using ShapezShifter;
using ShapezShifter.Buildings;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class PortalsMod : IMod
{
    private readonly List<ExtenderHandle> ExtenderHandles = new();

    private readonly PortalBuildingsExtender PortalBuildingsExtender;

    public PortalsMod(ILogger logger)
    {
        PortalBuildingsExtender = new PortalBuildingsExtender(logger);
        var handle = ShapezExtensions.AddExtender(PortalBuildingsExtender);
        ExtenderHandles.Add(handle);
    }

    public void Dispose()
    {
        foreach (var handle in ExtenderHandles)
        {
            ShapezExtensions.RemoveExtender(handle);
        }

        PortalBuildingsExtender.Dispose();
        ExtenderHandles.Clear();
    }
}

public class PortalBuildingsExtender : IPlacementInitiatorsExtender, IToolbarDataExtender, IBuildingsExtender,
    IBuildingModulesExtender, IResearchExtender, IDisposable
{
    private readonly ILogger Logger;
    private readonly BuildingDefinitionGroupId PortalReceiverGroupId = new("Portal_ReceiverGroup");

    private readonly BuildingDefinitionId PortalReceiverId = new("Portal_Receiver");

    private readonly BuildingDefinitionGroupId
        PortalSenderGroupId = new("Portal_SenderGroup");

    private readonly BuildingDefinitionId PortalSenderId = new("Portal_Sender");
    private BuildingsModulesLookup ModulesLookup;
    private PlacementInitiatorId? PlacementInitiatorId;
    private BuildingDefinitionGroup PortalReceiverGroup;
    private BuildingDefinitionGroup PortalSenderGroup;
    private AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId> RuleProcessor;

    public PortalBuildingsExtender(ILogger logger)
    {
        Logger = logger;
    }


    public void AddModules(BuildingsModulesLookup modulesLookup)
    {
        ModulesLookup = modulesLookup;
    }

    public GameBuildings ModifyGameBuildings(MetaGameModeBuildings metaBuildings, GameBuildings gameBuildings,
        IMeshCache meshCache, VisualThemeBaseResources theme)
    {
        Logger.Info?.Log($"Modifying Game Buildings. Object reference: {gameBuildings.GetRefId()}");
        if (gameBuildings._DefinitionsById.ContainsKey(PortalSenderId))
        {
            return gameBuildings;
        }

        PortalSenderGroup = BuildingHelper.CreateBuildingGroup(PortalSenderGroupId.Id,
            CreateMockSprite(32, 32, Color.red),
            "Portal Sender",
            "Sends a shape through a portal", false, DefaultPreferredPlacementMode.Single);

        PortalReceiverGroup = BuildingHelper.CreateBuildingGroup(PortalReceiverGroupId.Id,
            CreateMockSprite(32, 32, Color.green),
            "Portal Receiver",
            "Receives a shape through a portal", false, DefaultPreferredPlacementMode.Single);

        var senderInput = new BuildingItemInput
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.West
        };

        var receiverOutput = new BuildingItemOutput
        {
            IOType = BuildingItemIOType.ElevatedBorder,
            Position_L = TileVector.Zero,
            Seperators = false,
            StandType = BuildingBeltStandType.Normal,
            TileDirection = TileDirection.East
        };

        var tiles = new[]
        {
            TileVector.Zero
        };

        var tileBounds = LocalTileBounds.From_SLOW_DO_NOT_USE(tiles);
        var tileDimensions = tileBounds.Dimensions;
        var center = LocalVector.Lerp((LocalVector)tileBounds.Min, (LocalVector)tileBounds.Max, 0.5f);

        IBuildingConnectorData senderConnectorData =
            new BuildingConnectorData(senderInput.AsEnumerable(), tiles,
                tileBounds,
                center,
                tileDimensions);

        IBuildingConnectorData receiverConnectorData =
            new BuildingConnectorData(receiverOutput.AsEnumerable(), tiles,
                tileBounds,
                center,
                tileDimensions);

        var portalSender = CreateDefinition(PortalSenderId, senderConnectorData,
            PortalSenderGroup, gameBuildings);
        PortalSenderGroup.AddInternalVariant(portalSender);

        var portalReceiver = CreateDefinition(PortalReceiverId, receiverConnectorData,
            PortalReceiverGroup, gameBuildings);
        PortalReceiverGroup.AddInternalVariant(portalReceiver);

        gameBuildings._All.Add(PortalSenderGroup);
        gameBuildings._All.Add(PortalReceiverGroup);

        gameBuildings._DefinitionsById.Add(PortalSenderId, portalSender);
        gameBuildings._DefinitionsById.Add(PortalReceiverId, portalReceiver);

        gameBuildings._VariantsById.Add(PortalSenderGroupId, PortalSenderGroup);
        gameBuildings._VariantsById.Add(PortalReceiverGroupId, PortalReceiverGroup);

        return gameBuildings;
    }

    public void Dispose()
    {
        RuleProcessor?.Dispose();
    }


    public void ExtendInitiators(IEntityPlacementRunner placementRunner,
        IPlacementInitiatorIdRegistry placementRegistry)
    {
        CreatePlacementInitiator(placementRunner, placementRegistry);
    }

    public void ModifyResearch(ResearchManager research)
    {
        research.Layout.Levels[^1].Rewards = research.Layout.Levels[0].Rewards
            .Append(
                new ResearchRewardBuildingGroup(PortalSenderGroupId))
            .Append(
                new ResearchRewardBuildingGroup(PortalReceiverGroupId))
            .ToList();
    }

    public ToolbarData ModifyToolbarData(
        ToolbarData toolbarData)
    {
        if (!PlacementInitiatorId.HasValue)
        {
            throw new Exception("Expected placement initiator extension to be executed before toolbar one");
        }

        // Todo: Add XML-like queries for toolbar elements
        var shapeBuildings = (ParentToolbarElementData)toolbarData.RootToolbarElement.Children[0];
        var transportGroup = (ParentToolbarElementData)shapeBuildings.Children[0];
        var elements = transportGroup.Children.ToList();
        var belt = (IPresentableToolbarElementData)elements.First();

        var title = new LazyLocalizedText(new TranslationId(belt.Title.ToString()));
        var description = new LazyLocalizedText(new TranslationId(belt.Description.ToString()));
        var elementCopy =
            new PlacementToolbarElementData(title, description, PlacementInitiatorId.Value, belt.Icon);
        elements.Add(elementCopy);
        transportGroup.Children = elements.ToArray();
        return toolbarData;
    }

    private BuildingDefinition CreateDefinition(BuildingDefinitionId id, IBuildingConnectorData connectorData,
        IBuildingDefinitionGroup definitionGroup, GameBuildings gameBuildings)
    {
        var runtimeDefinition = new BuildingDefinition(id, connectorData);

        runtimeDefinition.CustomData.Attach(definitionGroup.DefaultPreferredPlacementMode);

        runtimeDefinition.CustomData.Attach(
            new EntityPlacementPreferenceData(
                false,
                100));

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
        runtimeDefinition.CustomData.Attach(gameBuildings.BeltPortSender.CustomData.Get<IBuildingDrawData>());

        runtimeDefinition.CustomData.Attach(gameBuildings.BeltPortSender.CustomData.Get<IBuildingSoundDefinition>());

        runtimeDefinition.CustomData.Attach(new EmptyCustomSimulationConfiguration());

        var efficiencyData = new BuildingEfficiencyData(
            1,
            1);
        runtimeDefinition.CustomData.Attach(efficiencyData);

        return runtimeDefinition;
    }

    private void CreatePlacementInitiator(IEntityPlacementRunner placementRunner,
        IPlacementInitiatorIdRegistry placementRegistry)
    {
        var portalSenderDefinition = PortalSenderGroup.Definitions[0];
        var placerData = new PlacerDataBasedOnRepresentingBuilding(portalSenderDefinition, ModulesLookup);
        var senderEntityPlacer = new ModularEntityPlacer(
            new SinglePlacer<GlobalTileTransform, GlobalTileCoordinate, TileVector, TileDirection, GlobalTilePivot,
                LocalTilePivot, TileAxis, BuildingInstanceModel, BuildingConnector, BuildingConnection>(
                portalSenderDefinition,
                new BuildingPlacementAdapter(),
                new BuildingAccessorAdapter(),
                new BuildingMapLayoutRegisterAdapter()),
            placerData);

        RuleProcessor = new AnyIdUnlockedWithResearchRewards<BuildingDefinitionGroupId>(
            GameHelper.Core.Research.Progress,
            PortalSenderGroup.Id,
            new BuildingResearchLockStatusSolver(),
            new BuildingRewardIdSolver());

        IPlacementInitiator portalSender = new GamePlacementInitiator(
            RuleProcessor,
            senderEntityPlacer,
            placementRunner);

        PlacementInitiatorId =
            placementRegistry.RegisterInitiator("PortalSenderPlacementInitiator", portalSender);
    }


    private static Sprite CreateMockSprite(int width, int height, Color color)
    {
        // Create and fill the texture
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Create and return the sprite
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}