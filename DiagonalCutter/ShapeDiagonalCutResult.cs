public readonly struct ShapeDiagonalCutResult
{
    public readonly ShapeCollapseResult LeftSide;
    public readonly ShapeCollapseResult RightSide;

    public ShapeDiagonalCutResult(ShapeCollapseResult leftSide, ShapeCollapseResult rightSide)
    {
        LeftSide = leftSide;
        RightSide = rightSide;
    }
}