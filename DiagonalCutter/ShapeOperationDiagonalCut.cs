using System.Linq;

public class ShapeOperationDiagonalCut : ShapeOperation<ShapeDefinition, ShapeDiagonalCutResult>
{
    public ShapeOperationDiagonalCut(int maxShapeLayers) : base(maxShapeLayers)
    {
    }

    public override ShapeDiagonalCutResult ExecuteInternal(ShapeDefinition shape)
    {
        ShapeLogic.UnfoldResult unfolded = ShapeLogic.Unfold(shape.Layers);
        var firstSide = unfolded.References.Where(reference => reference.PartIndex % 2 == 0).ToList();
        var secondSide = unfolded.References.Where(reference => reference.PartIndex % 2 == 1).ToList();

        ShapeCollapseResult leftResult = ShapeLogic.Collapse(
            firstSide,
            shape.PartCount,
            MaxShapeLayers,
            unfolded.FusedReferences);
        ShapeCollapseResult rightResult = ShapeLogic.Collapse(
            secondSide,
            shape.PartCount,
            MaxShapeLayers,
            unfolded.FusedReferences);

        return new ShapeDiagonalCutResult(leftResult, rightResult);
    }
}