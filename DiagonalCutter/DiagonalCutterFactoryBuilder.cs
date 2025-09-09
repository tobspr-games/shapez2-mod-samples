using Core.Factory;
using ShapezShifter;
using ShapezShifter.Fluent.Atomic;

internal class DiagonalCutterFactoryBuilder
    : IFactoryBuilder<DiagonalCutterSimulation, DiagonalCutterSimulationState, DiagonalCutterConfiguration>
{
    public IFactory<DiagonalCutterSimulationState, DiagonalCutterSimulation> BuildFactory(
        SimulationSystemsDependencies dependencies,
        out DiagonalCutterConfiguration config)
    {
        config = new DiagonalCutterConfiguration(
            BuffableBeltSpeed.DiscreteSpeed.OneSecondPerTile,
            BuffableBeltDelay.DiscreteDuration.OnePointFiveSeconds,
            new ResearchSpeedId("CutterSpeed"));

        var diagonalCut = new ShapeOperationDiagonalCut(dependencies.Mode.MaxShapeLayers);

        return new DiagonalCutterSimulationFactory(config, dependencies.ShapeRegistry, diagonalCut);
    }
}
