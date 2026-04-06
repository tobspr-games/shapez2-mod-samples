using Core.Factory;
using SandboxIslands;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack;

internal class FluidTrashFactoryBuilder : IBuildingSimulationFactoryBuilder<FluidTrashSimulation>
{
    public IFactory<FluidTrashSimulation> BuildFactory(SimulationSystemsDependencies dependencies)
    {
        return new ParameterlessConstructionFactory<FluidTrashSimulation>();
    }
}
