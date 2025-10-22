using Game.Core.Coordinates;
using JetBrains.Annotations;

[UsedImplicitly]
public class DiagonalCutterSimulationRenderer
    : StatelessBuildingSimulationRenderer<DiagonalCutterSimulation, IDiagonalCutterDrawData>
{
    public DiagonalCutterSimulationRenderer(
        IMapModel map,
        IBuildingSoundManager soundManager,
        IShapeIdRegistry shapeRegistry) : base(map) { }

    public override void OnDrawDynamic(in Entity entity, FrameDrawOptions options)
    {
        DiagonalCutterSimulation simulation = entity.Simulation;

        DrawBeltItem(entity.Transform, options, simulation.InputLane, entity.DrawData.InputLaneRenderingDefinition);
        DrawBeltItem(entity.Transform, options, simulation.OutputLane, entity.DrawData.OutputLaneRenderingDefinition);

        if (entity.Simulation.ProcessingLane.HasItem)
        {
            DrawWaste(entity, options);
            DrawRemaining(entity, options);
        }

        float progress = entity.Simulation.ProcessingLane.Progress;
        float alpha = entity.Simulation.ProducingEmptyShape ? 1.0f - progress : 1.0f;
        DrawShapeSupportMesh(
            entity.Transform,
            options,
            pos_L: options.Renderers.BeltItems.BeltShapeHeight * LocalVector.Up,
            alpha: alpha);
    }

    private void DrawRemaining(in Entity entity, FrameDrawOptions options)
    {
        if (entity.Simulation.CurrentCollapseResult == null)
        {
            return;
        }
        float progress = entity.Simulation.ProcessingLane.Progress;
        DrawShapeCollapseResult(
            entity.Transform,
            options,
            result: entity.Simulation.CurrentCollapseResult,
            pos_L: LocalVector.Up
                   * (options.Renderers.BeltItems.BeltShapeHeight
                      + options.Renderers.BeltItems.ShapeRenderer.SupportMeshHeight),
            progress_FallDown: progress,
            progress_ScaleX: progress,
            progress_ScaleY: progress);
    }

    private void DrawWaste(in Entity entity, FrameDrawOptions options)
    {
        if (entity.Simulation.CurrentWaste == null)
        {
            return;
        }

        float progress = entity.Simulation.ProcessingLane.Progress;
        float wasteOpacity = 1.0f - progress;

        float height = options.Renderers.BeltItems.BeltShapeHeight
                       + options.Renderers.BeltItems.ShapeRenderer.SupportMeshHeight;

        float collapseProgress = 1.0f;

        if (wasteOpacity > 0.0f)
        {
            DrawShapeCollapseResult(
                entity.Transform,
                options,
                result: entity.Simulation.CurrentWaste,
                pos_L: LocalVector.Up * height,
                progress_FallDown: collapseProgress,
                progress_ScaleX: collapseProgress,
                progress_ScaleY: collapseProgress,
                mainShapeDrawer: CustomDissolveShapeDrawer.WithOpacity(wasteOpacity));
        }
    }
}
