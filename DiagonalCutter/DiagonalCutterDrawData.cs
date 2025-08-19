using Game.Core.Coordinates;

internal class DiagonalCutterDrawData : IDiagonalCutterDrawData
{
    public IBeltLaneRendererDefinition InputLaneRenderingDefinition => new MyBeltLaneRenderingDefinition(
        new LocalVector(-0.5f, 0.0f, 0.0f),
        new LocalVector(0.0f, 0.0f, 0.0f));

    public IBeltLaneRendererDefinition OutputLaneRenderingDefinition => new MyBeltLaneRenderingDefinition(
        new LocalVector(0.0f, 0.0f, 0.0f),
        new LocalVector(0.5f, 0.0f, 0.0f));
}
