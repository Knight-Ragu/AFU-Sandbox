using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using Il2CppQuantum_Game;

namespace AfuSandbox;

public class SpawnMenu : RadialMenu<Spawnable>
{    
    public static SpawnMenu Init(Frame f)
    {
        return new SpawnMenu {

            // Melee Weapons

            new SpawnMenu {
                Spawnable.Equipment(EquipmentID.Bat),
                Spawnable.Equipment(EquipmentID.Pipe),
                Spawnable.Equipment(EquipmentID.Crowbar),
                Spawnable.Equipment(EquipmentID.Machete),
                Spawnable.Equipment(EquipmentID.RiotStick),
                Spawnable.Equipment(EquipmentID.Katana),
                Spawnable.Equipment(EquipmentID.Axe),
            },
            
            // Ranged Weapons
            
            new SpawnMenu {
                Spawnable.Equipment(EquipmentID.Blaster),
                Spawnable.Equipment(EquipmentID.SMG),
                Spawnable.Equipment(EquipmentID.Revolver),
                Spawnable.Equipment(EquipmentID.Shotgun),
                Spawnable.Equipment(EquipmentID.Kalashnikov),
                Spawnable.Equipment(EquipmentID.PlasmaPistol),
                Spawnable.Equipment(EquipmentID.Minigun),
            },

            // Special Weapons

            new SpawnMenu {
                Spawnable.Equipment(EquipmentID.Chain),
                Spawnable.Equipment(EquipmentID.TrafficSign),
                Spawnable.Equipment(EquipmentID.Chainsaw),
                Spawnable.Equipment(EquipmentID.Sledgehammer),
                Spawnable.Equipment(EquipmentID.Laser_Melee),
            },

            // Throwables

            new SpawnMenu {
                Spawnable.Equipment(EquipmentID.Molotov),
                Spawnable.Equipment(EquipmentID.CherryBomb),
                Spawnable.Equipment(EquipmentID.FragGrenade),
                Spawnable.Equipment(EquipmentID.Shuriken),
                Spawnable.Equipment(EquipmentID.Brick),
                Spawnable.Equipment(EquipmentID.Flashbang),
                Spawnable.Equipment(EquipmentID.Caltrops),
                Spawnable.Equipment(EquipmentID.EMPgrenade),
                Spawnable.Equipment(EquipmentID.Jerrycan),
                Spawnable.Equipment(EquipmentID.FoamBomb)
            },

            // Special

            new SpawnMenu {
                Spawnable.Equipment(EquipmentID.RiotShield),
                Spawnable.Equipment(EquipmentID.BODY_ARMOR_DUMMY),
                Spawnable.Pickup(PickupType.Ammo),
                Spawnable.Pickup(PickupType.Armor),
                Spawnable.Pickup(PickupType.Boost),
                Spawnable.Pickup(PickupType.Health),
                Spawnable.Pickup(PickupType.Money),
                Spawnable.Pickup(PickupType.Repair),
            },

            // Props

            new SpawnMenu {
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().humanoidConfig.Id).Cast<HumanoidConfig>().prototype),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().cars.Id).Cast<EntityPrototypeCollection>().prototypes[0]),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().trucks.Id).Cast<EntityPrototypeCollection>().prototypes[0]),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().hoverBikeConfig.Id).Cast<HoverBikeConfig>().prototype_medium),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().hoverBikeConfig.Id).Cast<HoverBikeConfig>().prototype_light),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().hoverBikeConfig.Id).Cast<HoverBikeConfig>().prototype_heavy),
                Spawnable.EntityPrototypeAssetRef(f.Context.ResourceManager.GetAsset(f.GameConfig().hoverBikeConfig.Id).Cast<HoverBikeConfig>().prototype_booster),
                Spawnable.EntityPrototypeAssetRef(f.GameConfig().policeHelicopter),
                Spawnable.EntityPrototypeAssetRef(f.GameConfig().airPlane),
            }
        };
    }
}

public struct Spawnable
{
    public SpawnableType Type;

    public PickupType PickupType;
    public EquipmentID EquipmentID;

    public EntityPrototype Prototype;

    public AssetRef<EntityPrototype> AssetRef;

    public static Spawnable Equipment(EquipmentID eqID)
        => new() {
            Type = SpawnableType.Pickup,
            PickupType = PickupType.Equipment,
            EquipmentID = eqID,
        };

    public static Spawnable Pickup(PickupType pickType)
        => new() {
            Type = SpawnableType.Pickup,
            PickupType = pickType,
        };
    
    public static Spawnable EntityPrototype(EntityPrototype entityPrototype)
        => new() {
            Type = SpawnableType.EntityPrototype,
            Prototype = entityPrototype,
        };

    public static Spawnable EntityPrototypeAssetRef(AssetRef<EntityPrototype> assetRef)
        => new() {
            Type = SpawnableType.AssetRef,
            AssetRef = assetRef,
        };
    
    unsafe public readonly EntityRef Spawn(Frame f, FPVector3 position)
    {
        EntityRef entity = EntityRef.None;

        switch (this.Type)
        {
            case SpawnableType.Pickup:
                var pickup = PickupSpawnSystem.SpawnPickup(f, this.PickupType, this.EquipmentID, 0, position);

                if (this.PickupType == PickupType.Equipment)
                {
                    // foreach (var e in Sandbox._eRefs.Iter())
                    //     if (
                    //         f.Has<HeldByPickup>(e)
                    //     //  && f.Get<HeldByPickup>(e).pickupEntity == pickup
                    //     ) {
                    //         entity = e;
                    //         f.Remove<HeldByPickup>(e);

                    //         // Sandbox .Log.Msg($"fouudn2");
                    //     }
                    //     else if (f.Has<Equipment>(e))
                    //     {
                    //         f.GetPointer<Equipment>(e)->enterPickupModeTimer = 100;
                    //         // Sandbox .Log.Msg($"fouudn3");
                    //     }
                }
            break;

            case SpawnableType.EntityPrototype:
                entity = f.Create(this.Prototype);
                f.Set(entity, Transform3D.Create(position));
            break;

            case SpawnableType.AssetRef:
                entity = f.Create(this.AssetRef);
                f.Set(entity, Transform3D.Create(position));
            break;
        }

        return entity;
            
        // f.GetPointer<Equipment>(pickup)->enterPickupModeTimer = 100;
    }

    public enum SpawnableType
    {
        Pickup,
        EntityPrototype,
        AssetRef,
    }
}