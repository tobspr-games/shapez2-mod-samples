using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PortalPatch : IMod
{
    public ModMetadata Metadata => new ModMetadata("Portal Building", "lorenzofman", "0.0.1f2");

    private AssetBundle PortalBundle;

    public void Init(string path)
    {
        var portalAssetBundlePath = Path.Combine(path, "resources", "portal");
        ShapezCallbackExt.OnPreGameStart += () => OnGameStart(portalAssetBundlePath);
    }

    private void OnGameStart(string portalAssetBundlePath)
    {
        if (PortalBundle == null)
        {
            PortalBundle = AssetBundle.LoadFromFile(portalAssetBundlePath);

            CreateEntity();
        }

    }

    private void CreateEntity()
    {
        var hooks = DetourOnEnableForScriptableObjects(typeof(MetaBuilding), typeof(MetaBuildingVariant), typeof(MetaBuildingInternalVariant));

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

        RestoreEnableScriptableObjects(portalMetaBuilding, hooks);

        HookHelper.CreatePostfixHook<SavegameCoordinator>(coordinator => coordinator.InitAfterCoreLoad(), AddBuildingAfterGameLoadButBeforeHudInitialization);

        void AddBuildingAfterGameLoadButBeforeHudInitialization()
        {
            if (GameCore.G.Mode.Buildings.Contains(portalMetaBuilding))
            {
                return;
            }
            GameCore.G.Mode.Buildings.Add(portalMetaBuilding);
            MetaResearchLevel node = new MetaResearchLevel
            {
                Cost = new ResearchUnlockCost[] 
                {
                    new ResearchUnlockCost()
                    {
                        AmountThroughput = 4,
                        DefinitionHash = "CuCuCuCu",
                    }
                },
                Unlocks = portalMetaBuilding.Variants.Cast<IResearchUnlock>().ToList(),
            };
            GameCore.G.Mode.Research.Nodes.Add(node);
        }
    }


    private void RestoreEnableScriptableObjects(MetaBuilding portalMetaBuilding, Hook[] hooks)
    {
        foreach (var hook in hooks)
        {
            hook.Dispose();
        }

        typeof(MetaBuilding).GetInstancePrivateMethod("OnEnable").Invoke(portalMetaBuilding, null);

        foreach (var variant in portalMetaBuilding.Variants)
        {
            typeof(MetaBuildingVariant).GetInstancePrivateMethod("OnEnable").Invoke(variant, null);

            foreach (var internalVariant in variant.InternalVariants)
            {
                typeof(MetaBuildingInternalVariant).GetInstancePrivateMethod("OnEnable").Invoke(internalVariant, null);
            }
        }
    }

    private Hook[] DetourOnEnableForScriptableObjects(params Type[] types)
    {
        Hook[] hooks = new Hook[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            hooks[i] = new Hook(types[i].GetInstancePrivateMethod("OnEnable"),
                typeof(PortalPatch).GetMethod(nameof(Empty)));
        }
        return hooks;
    }

    public static void Empty(object obj)
    {
        
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
        portalEntranceInternalVariant.Tiles = new int3[] { int3.zero };

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
                ItemEndPos_L = float3.zero,
                Filter = BeltLaneDefinition.ItemFilter.None,
                Speed = new MetaResearchSpeed()
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
        

        portalExitInternalVariant.Tiles = new int3[] { int3.zero };

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
                ItemStartPos_L = new float3(0, 0, 0),
                ItemEndPos_L = new float3(0.5f, 0, 0),
                Filter = BeltLaneDefinition.ItemFilter.None,
                Speed = new MetaResearchSpeed()
            }
        };

        return portalExitVariant;
    }
}
