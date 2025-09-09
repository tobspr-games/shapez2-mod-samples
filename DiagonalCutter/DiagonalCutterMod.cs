using System;
using System.IO;
using Core.Localization;
using JetBrains.Annotations;
using ShapezShifter;
using ShapezShifter.Fluent;
using ShapezShifter.Fluent.Atomic;
using ShapezShifter.Fluent.Toolbar;
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
    private readonly IDisposable Extender;

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

        IBuildingGroupBuilder diagonalCutterGroup = BuildingGroup.Create(groupId)
           .WithTitle(title.T())
           .WithDescription(description.T())
           .WithIcon(LoadIcon())
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
           .WithCopiedStaticDrawData(new BuildingDefinitionId("CutterHalfInternalVariant"))
           .WithPrediction(new AtomicPredictor<Operation>(new OpResult(4), ResultingShape))
           .WithoutSound()
           .WithoutSimulationConfiguration()
           .WithEfficiencyData(new BuildingEfficiencyData(2.0f, 1));

        Extender = AtomicBuildings.Extend()
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

    public void Dispose()
    {
        Extender.Dispose();
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
}
