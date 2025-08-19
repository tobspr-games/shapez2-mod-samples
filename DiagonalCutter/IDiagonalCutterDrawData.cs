public interface IDiagonalCutterDrawData : IBuildingCustomDrawData
{
    IBeltLaneRendererDefinition InputLaneRenderingDefinition { get; }
    IBeltLaneRendererDefinition OutputLaneRenderingDefinition { get; }
}
