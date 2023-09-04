using System;
using System.Collections.Generic;
using Unity.Mathematics;

[Serializable]
internal class PortalEntranceEntity : MapEntity
{
    protected BeltLane InputLane;
    protected IPortalReceiver Receiver;
    protected BeltPathLogic Path;
    public PortalEntranceEntity(CtorArgs payload) : base(payload)
    {
        InputLane = new BeltLane(InternalVariant.BeltLaneDefinitions[0]);

        Path = new BeltPathLogic(BeltLaneDefinition.STEPS_PER_UNIT * 2);
        InputLane.PreAcceptHook = InputPreAcceptHook;
        InputLane.PostAcceptHook = InputPostAcceptHook;
        InputLane.MaxStepClampHook = InputClampHook;
    }

    private int InputClampHook(BeltLane lane, int maxStep_S)
    {
        if (Receiver == null)
        {
            return 0;
        }

        return Math.Min(Path.FirstItemDistance_S - BeltLaneDefinition.ITEM_SPACING_S, maxStep_S);
    }

    private BeltItem InputPreAcceptHook(BeltItem item)
    {
        // Only accepts item if there is an available receiver (since we only operate here with one belt lane)
        var randomReceiver = PortalPool.GetRandom();
        if (randomReceiver == null)
        {
            return null;
        }

        Receiver = randomReceiver;
        return item;
    }

    public void InputPostAcceptHook(BeltLane inputLane, ref int excessTicks_T)
    {
        var steps_S = InputLane.Definition.S_From_T(excessTicks_T);

        // Try handing over the item to the path (must not fail, otherwise MaxStep_S is wrong)
        if (Path.AcceptItem(
                inputLane.Item,
                initialDistance_S: steps_S,
                maxProgress_S: Path_ComputeMaxProgress_S()
            ))
        {

            inputLane.ClearLaneRaw_UNSAFE();
            UpdateInputLaneMaxStep();
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                "Path logic didn't accept item - but there is no reason to. (Max="
                + Path_ComputeMaxProgress_S()
                + ")"
            );
        }
    }

    public int Path_ComputeMaxProgress_S()
    {
        return Path.Length_S + Receiver.OutputLane.MaxStep_S;
    }

    public override BeltLane Belts_GetLaneForInput(int index)
    {
        return InputLane;
    }

    public override HUDSidePanelModule[] HUD_GetInfoModules()
    {
        return new HUDSidePanelModule[]
        {
            new HUDSidePanelModuleBuildingEfficiency(this, InputLane),
            new HUDSidePanelModuleBeltItemContents(
                new List<BeltLane>
                {
                    InputLane
                }
            )
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
        Path.Update(
                options,
                availableSteps_S: InputLane.Definition.S_From_T(options.DeltaTicks_T),
                endIsConnected: true,
                transferHandler: Path_ItemTransferHandler,
                minStepsHandler_S: Path_HandlerGetMinStepsToEnd_S
            );
        UpdateInputLaneMaxStep();
        BeltSimulation.UpdateLane(options, InputLane);
    }

    private int Path_HandlerGetMinStepsToEnd_S()
    {
        return -Receiver.OutputLane.MaxStep_S;
    }

    public void UpdateInputLaneMaxStep()
    {
        // Can move as far as possible without intersecting with the first item of the path
        // This ensures we never call Path.AcceptItem in the PostAcceptHook because if there
        // is an item that is "blocking" we would never advance that far in the first place.
        InputLane.MaxStep_S = Path.FirstItemDistance_S - BeltLaneDefinition.ITEM_SPACING_S;
    }

    public bool Path_ItemTransferHandler(BeltItem item, int excessPathSteps_S)
    {
        var excessTicks_T = InputLane.Definition.T_From_S(excessPathSteps_S);

        return BeltSimulation.TransferToLane(item, Receiver.OutputLane, excessTicks_T);
    }

    protected override void Hook_OnDrawDynamic(ChunkFrameDrawOptions options)
    {
        DrawDynamic_BeltLane(options, InputLane);

        DrawDynamic_SupportMesh(options, 0, float3.zero);
    }
}
