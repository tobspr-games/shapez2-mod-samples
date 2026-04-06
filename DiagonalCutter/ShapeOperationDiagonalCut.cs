using System.Diagnostics.CodeAnalysis;
using System.Linq;

public class ShapeOperationDiagonalCut : ShapeOperation<ShapeDefinition, ShapeDiagonalCutResult>, IItemOperation1In1Out
{
    private readonly int MaxShapeLayers;

    public ShapeOperationDiagonalCut(
        int maxShapeLayers,
        [DisallowNull] IShapeRegistry shapeRegistry,
        [DisallowNull] IShapeIdManager shapeIdManager) : base(shapeRegistry, shapeIdManager)
    {
        MaxShapeLayers = maxShapeLayers;
    }

    public bool TryExecute(IItem input, out IItem output1)
    {
        if (input is not ShapeItem shapeItem)
        {
            output1 = null;
            return false;
        }
        ShapeDiagonalCutResult shapeCutResult = Execute(shapeItem.Definition);
        output1 = shapeCutResult.LeftSide != null ? ShapeRegistry.GetItem(shapeCutResult.LeftSide.Shape) : (IItem)null;
        return true;
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
            ShapeIdManager,
            unfolded.FusedReferences);
        ShapeCollapseResult rightResult = ShapeLogic.Collapse(
            secondSide,
            shape.PartCount,
            MaxShapeLayers,
            ShapeIdManager,
            unfolded.FusedReferences);

        return new ShapeDiagonalCutResult(leftResult, rightResult);
    }
}
