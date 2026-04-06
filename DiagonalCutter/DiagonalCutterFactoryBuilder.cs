using Core.Factory;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack;

internal class DiagonalCutterFactoryBuilder
    : IBuildingSimulationFactoryBuilder<DiagonalCutterSimulation, DiagonalCutterSimulationState,
        DiagonalCutterConfiguration>
{
    public IFactory<DiagonalCutterSimulationState, DiagonalCutterSimulation> BuildFactory(
        SimulationSystemsDependencies dependencies,
        out DiagonalCutterConfiguration config)
    {
        config = new DiagonalCutterConfiguration(
            BuffableBeltSpeed.DiscreteSpeed.OneSecondPerTile,
            BuffableBeltDelay.DiscreteDuration.OnePointFiveSeconds,
            new ResearchSpeedId("CutterSpeed"));

        var diagonalCut = new ShapeOperationDiagonalCut(
            dependencies.Mode.MaxShapeLayers,
            dependencies.ShapeRegistry,
            dependencies.ShapeIdManager);

        return new DiagonalCutterSimulationFactory(config, dependencies.ShapeRegistry, diagonalCut);
    }
}
