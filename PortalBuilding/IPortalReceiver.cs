internal interface IPortalReceiver
{
    BeltLane OutputLane { get; }

    void ReceiveItem(BeltItem item, int excessSteps_S);
}

