using Core.Factory;

public class DiagonalCutterSimulationFactory : IFactory<DiagonalCutterSimulationState, DiagonalCutterSimulation>
{
    private readonly IDiagonalCutterConfiguration Configuration;
    private readonly ShapeOperationDiagonalCut DiagonalCut;
    private readonly IShapeIdRegistry ShapeRegistry;

    public DiagonalCutterSimulationFactory(IDiagonalCutterConfiguration configuration, IShapeIdRegistry shapeRegistry,
        ShapeOperationDiagonalCut diagonalCut)
    {
        Configuration = configuration;
        ShapeRegistry = shapeRegistry;
        DiagonalCut = diagonalCut;
    }

    public DiagonalCutterSimulation Produce(DiagonalCutterSimulationState simulationState)
    {
        return new DiagonalCutterSimulation(simulationState, Configuration, ShapeRegistry, DiagonalCut);
    }
}

public interface IDiagonalCutterConfiguration
{
    public BeltSpeed BeltSpeed { get; }
    public BeltDelay ProcessingDelay { get; }
}