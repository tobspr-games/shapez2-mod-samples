using System.Collections.Generic;
using Core.Localization;
using JetBrains.Annotations;
using ShapezShifter;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class DiagonalCuttersMod : IMod
{
    private readonly DiagonalCutterBuildingsExtender DiagonalCutterBuildingsExtender;
    private readonly List<ExtenderHandle> ExtenderHandles = new();

    public DiagonalCuttersMod(ILogger logger)
    {
        DiagonalCutterBuildingsExtender = new DiagonalCutterBuildingsExtender(logger);
        ExtenderHandle handle = ShapezExtensions.AddExtender(DiagonalCutterBuildingsExtender);
        ExtenderHandles.Add(handle);

        var database = (LocalizationDatabase)Globals.Localization.DatabaseProvider.CurrentDatabase;

        var newKeys = new Dictionary<string, string>
        {
            { "building-variant.cutter-diagonal.title", "Diagonal Destroyer" },
            {
                "building-variant.cutter-diagonal.description",
                "<gl>Destroys</gl> the <gl>Even Parts</gl> of any shape."
            }
        };
        var translationParser = new TranslationParser().Parse(newKeys);
        foreach (var parsedTranslation in translationParser)
        {
            database.Entries.Add(parsedTranslation.Key, parsedTranslation.Value);
        }
    }

    public void Dispose()
    {
        foreach (ExtenderHandle handle in ExtenderHandles)
        {
            ShapezExtensions.RemoveExtender(handle);
        }

        DiagonalCutterBuildingsExtender.Dispose();
        ExtenderHandles.Clear();
    }
}
