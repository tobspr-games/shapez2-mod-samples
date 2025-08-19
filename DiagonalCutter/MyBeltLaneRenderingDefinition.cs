using Game.Core.Coordinates;

internal class MyBeltLaneRenderingDefinition : IBeltLaneRendererDefinition
{
    public LocalVector ItemStartPos_L { get; }
    public LocalVector ItemEndPos_L { get; }

    public MyBeltLaneRenderingDefinition(LocalVector itemStartPos_L, LocalVector itemEndPos_L)
    {
        ItemStartPos_L = itemStartPos_L;
        ItemEndPos_L = itemEndPos_L;
    }
}
