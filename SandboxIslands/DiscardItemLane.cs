using Game.Core.Simulation;
using ShapezShifter;

namespace SandboxIslands
{
    internal class DiscardItemLane : IItemReceiver
    {
        public Steps MaxStep_S => LaneConstants.ItemSpacing;

        /// Always accepts items.
        public bool CanAcceptItem(IBeltItem itemToDiscard)
        {
            return true;
        }

        /// Accepted items will be discarded.
        public void HandOverItem(IBeltItem itemToDiscard, Ticks remainingTicks)
        {
            Debugging.Logger.Info?.Log("Received item");

            // don't do anything with the item
        }
    }
}
