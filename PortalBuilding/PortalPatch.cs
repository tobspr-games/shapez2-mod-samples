using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// This mod is a proof of concept for adding a new building
/// </summary>
/// <remarks>
/// Some architecture choices are not great and the building itself has issues (serialization/saving, efficiency) that may or may not be fixed in the future
/// </remarks>
public class PortalPatch : IMod
{
    public ModMetadata Metadata => new ModMetadata("Portal Building", "lorenzofman", "0.0.1f2");

    private AssetBundle PortalBundle;

    public void Init(string path)
    {
        var portalAssetBundlePath = Path.Combine(path, "resources", "portal");
        ShapezCallbackExt.OnPreGameStart += () => BeforeGameStart(portalAssetBundlePath);
    }

    private void BeforeGameStart(string portalAssetBundlePath)
    {
        if (PortalBundle == null)
        {
            PortalBundle = AssetBundle.LoadFromFile(portalAssetBundlePath);
            RuntimeLoadedPortalResources.EntrancePortal = PortalBundle.LoadAsset<Material>("Assets/Materials/PortalEntrance.mat");
            RuntimeLoadedPortalResources.ExitPortal = PortalBundle.LoadAsset<Material>("Assets/Materials/PortalExit.mat");
            CreateEntity();
        }

    }

    private void CreateEntity()
    {
        HookHelper.CreatePostfixHook<SavegameCoordinator>(coordinator => coordinator.InitAfterCoreLoad(), AddBuildingAfterGameLoadButBeforeHudInitialization);

        void AddBuildingAfterGameLoadButBeforeHudInitialization()
        {
            var entrancePortalMeshes = PortalBundle.LoadAssetWithSubAssets<Mesh>("Assets/Models/PortalEntrance.fbx");
            var exitPortalMeshes = PortalBundle.LoadAssetWithSubAssets<Mesh>("Assets/Models/PortalExit.fbx");

            MetaBuilding portalMetaBuilding = new MetaBuilding
            {
                Categories = new List<string>() { "transport" },

                Icon = PortalBundle.LoadAsset<Sprite>("Assets/Icons/PortalIcon.png"),
                Variants = new List<MetaBuildingVariant>
                {
                    CreateEntrance(entrancePortalMeshes[1], entrancePortalMeshes[0]), CreateExit(exitPortalMeshes[1], exitPortalMeshes[0])
                }
            };

            if (GameCore.G.Mode.Buildings.Contains(portalMetaBuilding))
            {
                return;
            }
            GameCore.G.Mode.Buildings.Add(portalMetaBuilding);

            var researchable = new MetaResearchable();

            var unlocksFieldInfo = typeof(MetaResearchable).GetField("_Unlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            List<IResearchUnlock> unlocks = portalMetaBuilding.Variants.Cast<IResearchUnlock>().ToList();
            unlocksFieldInfo.SetValue(researchable, unlocks);

            var cost = new ResearchUnlockCost()
            {
                AmountThroughput = 4,
                DefinitionHash = "CuCuCuCu",
            };

            var levels = GameCore.G.Research.Tree.Levels;


            ResearchLevelHandle newLevel = new ResearchLevelHandle(researchable, cost, levels.Length, Array.Empty<ResearchSideGoalHandle>(), levels[^1]);
            var newLevels = levels.Concat(new ResearchLevelHandle[] { newLevel }).ToArray();

            var levelsPropertyInfo = typeof(ResearchTreeHandle).GetProperty("Levels");
            levelsPropertyInfo.SetValue(GameCore.G.Research.Tree, newLevels, null);
        }
    }

    private static MetaBuildingVariant CreateEntrance(Mesh portalBase, Mesh portal)
    {
        MetaBuildingVariant portalEntranceVariant = new MetaBuildingVariant();
        MetaBuildingInternalVariant portalEntranceInternalVariant = new MetaBuildingInternalVariant();

        portalEntranceVariant.InternalVariants = new MetaBuildingInternalVariant[]
        {
            portalEntranceInternalVariant
        };


        portalEntranceInternalVariant.Variant = portalEntranceVariant;

        portalEntranceInternalVariant.Implementation.ClassID = typeof(PortalEntranceEntity).AssemblyQualifiedName;
        portalEntranceInternalVariant.HasMainMesh = true;
        portalEntranceInternalVariant.MainMeshLOD = new LOD4Mesh()
        {
            LODMinimal = portalBase,
            LODNormal = portalBase,
            LODClose = portalBase,
            LODFar = portalBase
        };

        portalEntranceInternalVariant.SupportMeshesInternalLOD = new LOD2Mesh[]
        {
            new LOD2Mesh()
            {
                LODClose = portal,
                LODNormal = portal
            }
        };

        portalEntranceInternalVariant.Colliders = new MetaBuildingInternalVariant.CollisionBox[]
        {
            new MetaBuildingInternalVariant.CollisionBox()
            {
                Center_L = float3.zero,
                Dimensions_L = new float3(1, 1, 1)
            }
        };
        portalEntranceInternalVariant.Tiles = new TileDirection[] { TileDirection.Zero };

        portalEntranceInternalVariant.BeltInputs = new MetaBuildingInternalVariant.BeltIO[]
        {
            new MetaBuildingInternalVariant.BeltIO()
            {
                Direction_L = Grid.Direction.Left,
                IOType = MetaBuildingInternalVariant.BeltIOType.Regular,
                StandType = MetaBuildingInternalVariant.BeltStandType.None,
                Seperators = false,
                OutputPredictorClass = null
            }
        };


        portalEntranceInternalVariant.BeltLaneDefinitions = new BeltLaneDefinition[]
        {
            new BeltLaneDefinition()
            {
                Name = "PortalEntrance",
                Duration = 0.5f,
                Length_W = 0.5f,
                ItemStartPos_L = new float3(-0.5f, 0, 0),
                ItemEndPos_L = new float3(0.5f, 0, 0),
                Filter = BeltLaneDefinition.ItemFilter.None,
                Speed = GetBeltSpeed()
            }
        };

        return portalEntranceVariant;
    }

    private MetaBuildingVariant CreateExit(Mesh portalBase, Mesh portal)
    {
        MetaBuildingVariant portalExitVariant = new MetaBuildingVariant();
        MetaBuildingInternalVariant portalExitInternalVariant = new MetaBuildingInternalVariant();

        portalExitVariant.InternalVariants = new MetaBuildingInternalVariant[]
        {
            portalExitInternalVariant
        };

        portalExitInternalVariant.Variant = portalExitVariant;

        portalExitInternalVariant.Implementation.ClassID = typeof(PortalExitEntity).AssemblyQualifiedName;
        portalExitInternalVariant.HasMainMesh = true;
        portalExitInternalVariant.MainMeshLOD = new LOD4Mesh()
        {
            LODMinimal = portalBase,
            LODNormal = portalBase,
            LODClose = portalBase,
            LODFar = portalBase
        };

        portalExitInternalVariant.SupportMeshesInternalLOD = new LOD2Mesh[]
        {
            new LOD2Mesh()
            {
                LODClose = portal,
                LODNormal = portal
            }
        };

        portalExitInternalVariant.Colliders = new MetaBuildingInternalVariant.CollisionBox[] 
        {
            new MetaBuildingInternalVariant.CollisionBox()
            {
                Center_L = float3.zero,
                Dimensions_L = new float3(1, 1, 1)
            }
        };
        

        portalExitInternalVariant.Tiles = new TileDirection[] { TileDirection.Zero };

        portalExitInternalVariant.BeltOutputs = new MetaBuildingInternalVariant.BeltIO[]
        {
            new MetaBuildingInternalVariant.BeltIO()
            {
                Direction_L = Grid.Direction.Right,
                IOType = MetaBuildingInternalVariant.BeltIOType.Regular,
                StandType = MetaBuildingInternalVariant.BeltStandType.None,
                Seperators = false,
                OutputPredictorClass = new EditorClassIDSingleton<BuildingOutputPredictor>("BuildingOutputPredictor")
    }
        };

        portalExitInternalVariant.BeltLaneDefinitions = new BeltLaneDefinition[]
        {
            new BeltLaneDefinition()
            {
                Name = "PortalExit",
                Duration = 0.5f,
                Length_W = 0.5f,
                ItemStartPos_L = new float3(-0.5f, 0, 0),
                ItemEndPos_L = new float3(0.5f, 0, 0),
                Filter = BeltLaneDefinition.ItemFilter.None,
                Speed = GetBeltSpeed()
            }
        };

        return portalExitVariant;
    }

    private static MetaResearchSpeed GetBeltSpeed()
    {
        Debug.Assert(GameCore.G.Mode.Research.Speeds != null);
        // Currently there is no way to just access a valid speed node. This function is a very hacky way to get some valid reference
        return GameCore.G.Mode.Research.Speeds.CachedEntries.First().Key;
    }
}
