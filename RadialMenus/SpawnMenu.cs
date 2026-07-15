using Il2CppQuantum;

namespace AfuSandbox;

public class SpawnMenu : RadialMenu<AssetRef<EntityPrototype>>
{
    public static SpawnMenu Init(Frame f)
    {
        var eQ = f.EquipmentConfig();

        return new SpawnMenu {
            // Melee Weapons
            new SpawnMenu {
                eQ.bat,
                eQ.pipe,
                eQ.crowbar,
                eQ.machete,
                eQ.riotStick,
                eQ.katana,
                eQ.axe,
            },
            // Ranged Weapons
            new SpawnMenu {
                eQ.blaster,
                eQ.SMG,
                eQ.revolver,
                eQ.shotgun,
                eQ.kalashnikov,
                eQ.plasmaPistol,
                eQ.minigun,
            },
            // Special Weapons
            new SpawnMenu {
                eQ.laser,
                eQ.chain,
                eQ.trafficSign,
                eQ.chainsaw,
                eQ.sledgehammer,
            },
            // Throwables
            new SpawnMenu {
                eQ.molotov,
                eQ.cherryBomb,
                eQ.fragGrenade,
                eQ.shuriken,
                eQ.brick,
                eQ.flashbang,
                eQ.caltrops,
                eQ.EMPgrenade,
                eQ.foamBomb,
            },
            // Special
            eQ.shieldPrototype,
        };
    }
}