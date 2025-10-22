using Game.Core.Simulation;

public class DiagonalCutterSimulation : Simulation<DiagonalCutterSimulationState>, IItemSimulation, IUpdatableSimulation
{
    public ShapeCollapseResult CurrentWaste => State.CurrentWaste;
    public ShapeCollapseResult CurrentCollapseResult => State.CurrentCollapseResult;
    public bool ProducingEmptyShape => State.ProducingEmptyShape;

    public readonly BeltLane InputLane;
    public readonly BeltLane OutputLane;
    public readonly DelayBeltLane ProcessingLane;

    /// <inheritdoc />
    public int NumItemReceivers => 1;

    /// <inheritdoc />
    public int NumItemProviders => 1;

    public DiagonalCutterSimulation(
        DiagonalCutterSimulationState simulationState,
        IDiagonalCutterConfiguration cutterConfiguration,
        IShapeIdRegistry shapeRegistry,
        ShapeOperationDiagonalCut diagonalCut) : base(simulationState)
    {
        OutputLane = new BeltLane(cutterConfiguration.BeltSpeed, simulationState.OutputLaneState);
        ProcessingLane = new DelayBeltLane(
            cutterConfiguration.ProcessingDelay,
            simulationState.ProcessingLaneState,
            OutputLane);
        InputLane = new BeltLane(cutterConfiguration.BeltSpeed, simulationState.InputLaneState, ProcessingLane);

        ProcessingLane.AcceptHook = (IItemReceiver _, ref IBeltItem item, ref Ticks _) =>
        {
            ShapeDefinition definition = ((ShapeItem)item).Definition;
            ShapeDiagonalCutResult result = diagonalCut.Execute(definition);

            State.CurrentWaste = result.LeftSide;
            State.CurrentCollapseResult = result.RightSide;

            ShapeItem resultItem = shapeRegistry.GetItem(result.RightSide?.Shape ?? ShapeId.Invalid);
            if (resultItem != null)
            {
                item = resultItem;
                State.ProducingEmptyShape = false;
            }
            else
            {
                State.ProducingEmptyShape = true;
            }
        };
        OutputLane.AcceptHook = (IItemReceiver _, ref IBeltItem item, ref Ticks _) =>
        {
            if (State.ProducingEmptyShape)
            {
                item = null;
            }
        };
    }

    /// <inheritdoc />
    public IItemReceiver GetItemReceiver(int index)
    {
        return InputLane;
    }

    /// <inheritdoc />
    public IItemProvider GetItemProvider(int index)
    {
        return OutputLane;
    }

    /// <inheritdoc />
    public void TraverseLanes<TTraverser>(TTraverser traverser)
        where TTraverser : IBeltLaneTraverser
    {
        traverser.Traverse(InputLane);
        traverser.Traverse(ProcessingLane);
        traverser.Traverse(OutputLane);
    }

    /// <inheritdoc />
    public void ClearContent()
    {
        TraverseLanes(ClearItemsBeltLaneTraverser.Default);
    }

    /// <inheritdoc />
    public void Update(Ticks startTicks, Ticks deltaTicks)
    {
        OutputLane.Update(deltaTicks);
        ProcessingLane.Update(deltaTicks);
        InputLane.Update(deltaTicks);
    }
}
