internal class DiagonalCutterConfiguration : IDiagonalCutterConfiguration
{
    public BeltSpeed BeltSpeed => _Speed;

    public BeltDelay ProcessingDelay => _Delay;
    private readonly BuffableBeltDelay _Delay;

    private readonly BuffableBeltSpeed _Speed;

    public DiagonalCutterConfiguration(
        BuffableBeltSpeed.DiscreteSpeed beltSpeed,
        BuffableBeltDelay.DiscreteDuration cutDuration,
        ResearchSpeedId researchSpeed)
    {
        _Speed = new BuffableBeltSpeed
        {
            BaseSpeed = beltSpeed,
            ResearchId = researchSpeed
        };

        _Delay = new BuffableBeltDelay
        {
            BaseDuration = cutDuration,
            Research = researchSpeed
        };

        _Speed.OnAfterDeserialize();
        _Delay.OnAfterDeserialize();
    }
}
