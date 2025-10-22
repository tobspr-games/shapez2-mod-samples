using System;
using Game.Content.Features.SpacePaths;

namespace SandboxIslands
{
    public class FluidTrashSimulation : IItemBundleSimulation
    {
        public int NumItemReceiverBundles => 4;
        public int NumItemProviderBundles => 0;

        private readonly ItemReceiverBundle<DiscardItemLane>
            InputBundle0 = ItemReceiverBundle.Create<DiscardItemLane>();

        private readonly ItemReceiverBundle<DiscardItemLane>
            InputBundle1 = ItemReceiverBundle.Create<DiscardItemLane>();

        private readonly ItemReceiverBundle<DiscardItemLane>
            InputBundle2 = ItemReceiverBundle.Create<DiscardItemLane>();

        private readonly ItemReceiverBundle<DiscardItemLane>
            InputBundle3 = ItemReceiverBundle.Create<DiscardItemLane>();

        public IItemReceiverBundle GetItemReceiverBundle(int inputIndex)
        {
            return inputIndex switch
            {
                0 => InputBundle0,
                1 => InputBundle1,
                2 => InputBundle2,
                3 => InputBundle3,
                _ => throw new ArgumentOutOfRangeException(nameof(inputIndex), inputIndex, null)
            };
        }

        public void TraverseLanes<TTraverser>(TTraverser traverser)
            where TTraverser : IBeltLaneTraverser { }
    }
}
