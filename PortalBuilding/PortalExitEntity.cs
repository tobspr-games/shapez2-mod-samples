using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

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
                internalVariant.BeltLaneDefinitions[0].ScaledDuration_NonDeterministic
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

        // We don't use LODs in this case
        var lodMesh = InternalVariant.SupportMeshesInternalLOD[0];
        ref var reference = ref lodMesh.GetEntry(options.BuildingsLOD);

        options.InstanceManager.AddInstance(
            key: reference.InstancingID,
            mesh: reference.Mesh,
            material: RuntimeLoadedPortalResources.ExitPortal,
            transform: FastMatrix.TranslateRotate(W_From_L(float3.zero), Rotation_G),
            camera: options.MainTargetRenderCamera
        );
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
