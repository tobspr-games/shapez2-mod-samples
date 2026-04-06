using Game.Core.Serialization;
using Game.Core.Simulation;

[SyncableIdentifier("DiagonalCutterState")]
public class DiagonalCutterSimulationState : ISimulationState
{
    public readonly BeltLaneState InputLaneState = new();
    public readonly BeltLaneState OutputLaneState = new();
    public readonly BeltLaneState ProcessingLaneState = new();
    public ShapeCollapseResult CurrentCollapseResult;

    public ShapeCollapseResult CurrentWaste;

    public bool ProducingEmptyShape;

    public void Sync(ISerializationVisitor visitor)
    {
        InputLaneState.Sync(visitor);
        ProcessingLaneState.Sync(visitor);
        OutputLaneState.Sync(visitor);

        var collapseResultSerializer = visitor.GetSerializer<ShapeCollapseResult>();

        collapseResultSerializer.Sync(ref CurrentWaste);
        collapseResultSerializer.Sync(ref CurrentCollapseResult);

        visitor.SyncBool_1(ref ProducingEmptyShape);
    }
}
