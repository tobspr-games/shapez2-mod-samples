using System;
using System.Collections.Generic;
using Unity.Mathematics;

[Serializable]
public class PortalExitEntity : MapEntity, IPortalReceiver
{
    public BeltLane OutputLane { get; }

    public PortalExitEntity(CtorArgs payload) : base(payload)
    {
        OutputLane = new BeltLane(InternalVariant.BeltLaneDefinitions[0]);
    }

    public override BeltLane Belts_GetLaneForOutput(int index)
    {
        return OutputLane;
    }

    public override HUDSidePanelModule[] HUD_GetInfoModules()
    {
        return new HUDSidePanelModule[]
        {
            new HUDSidePanelModuleBuildingEfficiency(this, OutputLane),
            new HUDSidePanelModuleBeltItemContents(new List<BeltLane> { OutputLane })
        };
    }

    static public new HUDSidePanelModuleBaseStat[] HUD_ComputeStats(
        MetaBuildingInternalVariant internalVariant
    )
    {
        return new HUDSidePanelModuleBaseStat[]
        {
            new HUDSidePanelModuleStatProcessingTime(
                internalVariant.BeltLaneDefinitions[0].ScaledDuration_NonDeterministic,
                internalVariant.BeltLaneDefinitions[0].Speed
            )
        };
    }

    protected override void Hook_OnUpdate(TickOptions options)
    {
        BeltSimulation.UpdateLane(options, OutputLane);
        if (!OutputLane.HasItem)
        {
            PortalPool.SubscribePortal(this);
        }
    }

    protected override void Hook_OnDrawDynamic(ChunkFrameDrawOptions options)
    {
        DrawDynamic_BeltLane(options, OutputLane);
        DrawDynamic_SupportMesh(options, 0, float3.zero);
    }

    public void ReceiveItem(BeltItem item, int excessSteps_S)
    {
        BeltSimulation.TransferToLane(
                    item,
                    OutputLane,
                    OutputLane.Definition.T_From_S(excessSteps_S)
                );
        OutputLane.Item = item;
    }
}
