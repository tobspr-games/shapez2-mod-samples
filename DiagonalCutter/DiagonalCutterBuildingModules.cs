using System.Collections.Generic;

public class DiagonalCutterBuildingModules : IBuildingModules
{
    public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IBuildingDefinition definition)
    {
        yield break;
    }

    public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IMapModel map, BuildingModel building)
    {
        yield break;
    }
}
