using JetBrains.Annotations;

[UsedImplicitly]
public class DiagonalCutterOutputPredictor : ShapeProcessingOutputPredictor
{
    private readonly ShapeOperationDiagonalCut DiagonalCut;

    public DiagonalCutterOutputPredictor(ShapeOperationDiagonalCut diagonalCut)
    {
        DiagonalCut = diagonalCut;
    }

    public override void PredictValidCombination(
        SimulationPredictionInputCombinationMap combination,
        SimulationPredictionOutputWriter outputWriter,
        IShapeRegistry shapes)
    {
        if (!combination.TryPopShapeAtInput(0, out ShapeItem shapeItem1))
        {
            return;
        }

        ShapeDiagonalCutResult shapeCutResult = DiagonalCut.Execute(shapeItem1.Definition);
        ShapeCollapseResult rightSide = shapeCutResult.RightSide;
        ShapeId id = rightSide?.Shape ?? ShapeId.Invalid;
        ShapeItem shapeItem2 = shapes.GetItem(id);
        outputWriter.PushShapeAtOutput(0, shapeItem2);
    }
}
