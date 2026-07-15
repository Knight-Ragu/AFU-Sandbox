using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using Il2CppQuantum.Core;
using Il2CppQuantum_Game;
using Il2CppView_Access;
using JPInstaller;
using JPInstaller.Custom;
using MelonLoader;

[assembly: MelonInfo(typeof(AfuSandbox.Sandbox), "AfuSandbox", "0.1.1", "Knight-Ragu", null)]
[assembly: MelonGame("Videocult", "Airframe")]

namespace AfuSandbox;

public partial class Sandbox : QuantumMod
{
    internal static MelonLogger.Instance Log => Melon<Sandbox>.Instance.LoggerInstance;
   
    internal static AllEntityRefs _eRefs = null;

    // public static EntityPrototype Toolgun = EntityPrototype.Create();

    public override void OnInitializeMelon()
    {
        CustomManager.RegisterCustomComponent<NoclipController>();
        CustomManager.RegisterCustomComponent<Toolgun>();

        CustomEquipment.RegisterEquipment(new EquipmentData { Name = nameof(Toolgun), Type = HUD_Access.EquipmentColor.Weapon });

        // Toolgun.Container = ComponentPrototypeSet.FromArray(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<ComponentPrototype>([
        //     new ComponentPrototype { ComponentType = JPInstaller.Custom.CustomManager }
        // ]));
    }

    public unsafe override void Simulate(Frame f, AllEntityRefs eRefs)
    {
        _eRefs = eRefs;

        List<EntityRef> players = [];
        List<EntityRef> heldByHumanoids = [];

        foreach (var entity in eRefs.Iter())
            if (f.Has<Player>(entity)) players.Add(entity);
            else if (f.Has<HeldByHumanoid>(entity)) heldByHumanoids.Add(entity);

        foreach (var playerEntity in players)
        {
            Player* player = f.GetPointer<Player>(playerEntity);

            if (!f.Exists(player->controlledEntity)) continue;

            var input = f.GetPlayerInput(player->playerRef);

            if (input->menu.IsDown && input->duck.IsDown)
            {
                input->menu = new Button {
                    _frameCurrent = input->menu._frameCurrent,
                    _frameDown = 0,
                    _frameUp = 1
                };

                
            }

            Noclip.NoClip(f, playerEntity, player);
        }

        foreach (var heldByHumanoidEntity in heldByHumanoids)
        {
            EntityRef humanoid = f.Get<HeldByHumanoid>(heldByHumanoidEntity).humanoidEntity;
            Toolgun* equipment = (Toolgun*)f.GetPointer<Equipment>(heldByHumanoidEntity);
            Input* input = f.GetPlayerInput(players.Select(f.Get<Player>).First(p => p.controlledEntity == humanoid).playerRef);

            switch (equipment->CustomId)
            {
                case 1000:
                    Toolgun.SimulateHeld(f, input, humanoid, heldByHumanoidEntity);
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(BikeRespawnSystem), nameof(BikeRespawnSystem.SpawnBike))]
class BikeRespawnSystem_SpawnBike_Patch
{
    static unsafe void Postfix(FrameBase __0)
    {
        if (__0.TryCast<Frame>() is not Frame f) return;

        Toolgun.Create(f, FPVector3.Up * FP._0_10);

        
        // Toolgun* pointer = (Toolgun*)f.GetPointer<Equipment>(rev);
        // pointer->CustomId = 36;
    }
}

// [HarmonyPatch(typeof(DeterministicSession), nameof(DeterministicSession.SendCommand))]
// class DeterministicSession_SendCommand_Patch
// {
//     unsafe static void Postfix()
//     {
//         Sandbox .Log.Msg($"DeterministicSession.SendCommand: {Il2CppSystem.Environment.StackTrace}");
//     }
// }

static class FrameExtensions
{
    public static EquipmentConfig EquipmentConfig(this Frame f)
        => f.Context.ResourceManager.GetAsset(f.GameConfig().equipmentConfig.Id).Cast<EquipmentConfig>();
}
