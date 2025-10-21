using System;
using System.Diagnostics.CodeAnalysis;
using Game.Content.Features.SpacePaths;

public class ItemReceiverBundle<TItemReceiver> : Bundle<TItemReceiver>, IItemReceiverBundle
    where TItemReceiver : IItemReceiver
{
    internal ItemReceiverBundle(TItemReceiver[] lanes) : base(lanes) { }

    public IItemReceiver GetReceiver(short laneIndex, short layerIndex) => GetEntry(laneIndex, layerIndex);
}

public static class ItemReceiverBundle
{
    public static ItemReceiverBundle<TLane> Create<TLane>()
        where TLane : IItemReceiver, new()
    {
        TLane[] lanes = CreateLanes<TLane>();
        var bundle = new ItemReceiverBundle<TLane>(lanes);

        return bundle;
    }

    public static ItemReceiverBundle<TLane> Create<TLane, TState>(
        [NotNull] IBundleState<TState> bundleState,
        [NotNull] Func<TState, TLane> factory)
        where TLane : IItemLane
        where TState : class
    {
        TLane[] lanes = CreateLanes(bundleState, factory);
        var bundle = new ItemReceiverBundle<TLane>(lanes);

        return bundle;
    }

    internal static TLane[] CreateLanes<TLane>()
        where TLane : IItemReceiver, new()
    {
        var lanes = new TLane[SpacePathConstants.TotalEntriesPerBundle];

        for (short laneIndex = 0; laneIndex < SpacePathConstants.NumLanes; laneIndex++)
        {
            for (short layerIndex = 0; layerIndex < SpacePathConstants.NumLayers; layerIndex++)
            {
                var lane = new TLane();
                lanes[Bundle.ToArrayIndex(laneIndex, layerIndex)] = lane;
            }
        }

        return lanes;
    }

    internal static TLane[] CreateLanes<TLane, TState>(
        [NotNull] IBundleState<TState> bundleState,
        [NotNull] Func<TState, TLane> factory)
        where TLane : IItemReceiver
        where TState : class
    {
        var lanes = new TLane[SpacePathConstants.TotalEntriesPerBundle];

        for (short laneIndex = 0; laneIndex < SpacePathConstants.NumLanes; laneIndex++)
        {
            for (short layerIndex = 0; layerIndex < SpacePathConstants.NumLayers; layerIndex++)
            {
                TLane lane = factory.Invoke(bundleState.GetState(laneIndex, layerIndex));
                lanes[Bundle.ToArrayIndex(laneIndex, layerIndex)] = lane;
            }
        }

        return lanes;
    }
}
