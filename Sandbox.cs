using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppQuantum;
using Il2CppView_Access;
using JPInstaller;
using JPInstaller.Custom;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(AfuSandbox.Sandbox), "AfuSandbox", "0.3.1", "Knight-Ragu", null)]
[assembly: MelonGame("Videocult", "Airframe")]

namespace AfuSandbox;

public partial class Sandbox : QuantumMod
{
    internal static MelonLogger.Instance Log => Melon<Sandbox>.Instance.LoggerInstance;

    internal static string Data => MelonEnvironment.UserDataDirectory + "\\Sandbox";
    internal static string Assets => Data + "\\assets";
   
    internal static AllEntityRefs _eRefs = null;

    public override void OnInitializeMelon()
    {   
        this.RegisterTypes();
        
        CustomManager.RegisterCustomComponent<NoclipController>();
        CustomManager.RegisterCustomComponent<RadialMenuSelector>();
        CustomManager.RegisterCustomComponent<Toolgun>();

        CustomEquipment.RegisterEquipment(new EquipmentData {
            Name = nameof(Toolgun),
            Type = HUD_Access.EquipmentColor.Weapon,
            OnHeld = Toolgun.OnHeld,
        });

        Sounds.LoadSounds();
    }

    partial void RegisterTypes();

    public unsafe override void Simulate(Frame f, AllEntityRefs eRefs)
    {
        _eRefs = eRefs;

        List<EntityRef> playersERefs = [];
        List<EntityRef> heldByHumanoids = [];

        foreach (var entity in eRefs.Iter())
            if (f.Has<Player>(entity))
            {
                playersERefs.Add(entity);

                Player* player = f.GetPointer<Player>(entity);
                var input = f.GetPlayerInput(player->playerRef);

                Noclip.Simulate(f, entity, player);

                if (!f.Exists(player->controlledEntity)) continue;

                if (input->duck.IsDown && input->menu.WasPressed)
                    EquipmentExtensions.GrabEquipment(f, Toolgun.Create(f, f.Get<Transform3D>(player->controlledEntity).Position), player->controlledEntity);

                RadialMenuSelector.Simulate(f, player, input);
            }
            else if (f.Has<HeldByHumanoid>(entity)) heldByHumanoids.Add(entity);
        
        Player[] players = [.. playersERefs.Select(f.Get<Player>)];

        foreach (var heldByHumanoidEntity in heldByHumanoids)
        {
            EntityRef humanoid = f.Get<HeldByHumanoid>(heldByHumanoidEntity).humanoidEntity;
            Equipment equipment = f.Get<Equipment>(heldByHumanoidEntity);
            Player player = players.First(p => p.controlledEntity == humanoid);

            if (CustomEquipment.TryGetEquipmentData(equipment.eqID, out var data))
            {
                data.OnHeld(f, player, humanoid, heldByHumanoidEntity);
            }
        }
    }
}

static class FrameExtensions
{
    public static EquipmentConfig EquipmentConfig(this Frame f)
        => f.Context.ResourceManager.GetAsset(f.GameConfig().equipmentConfig.Id).Cast<EquipmentConfig>();
}

// [HarmonyPatch(typeof(DeterministicSession), nameof(DeterministicSession.SendCommand))]
// class DeterministicSession_SendCommand_Patch
// {
//     unsafe static void Postfix()
//     {
//         Sandbox .Log.Msg($"DeterministicSession.SendCommand: {Il2CppSystem.Environment.StackTrace}");
//     }
// }
