using System;
using System.Runtime.InteropServices;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using Il2CppQuantum.Physics3D;
using Il2CppQuantum_Core;
using Il2CppView_Humanoid;
using JPInstaller.Custom;

namespace AfuSandbox;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Toolgun() : JPInstaller.Custom.IComponent
{
    internal int CustomId;

    public RadialMenuSelector selectedPrototype = default;

    public const int FIRE_RATE = 20;
    public int cooldown = 0;

    public JPInstaller.Custom.IComponent SetID(int id)
    {
        CustomId = id;
        return this;
    }


    public static unsafe void OnHeld(Frame f, Player player, EntityRef humanoid, EntityRef pickup)
    {
        Input* input = f.GetPlayerInput(player.playerRef);
        var toolgun = f.CustomGetPointer<Toolgun>(pickup);
        bool disablePrimary = false;

        if (input->weaponSecondary.WasPressed)
            f.CustomSet(humanoid, new RadialMenuSelector());
        
        if (input->weaponSecondary.IsDown)
            disablePrimary = true;

        if (input->weaponSecondary.WasReleased && f.CustomTryGet<RadialMenuSelector>(humanoid, out var selector))
        {
            if (!selector.WasCancelledEarly())
                toolgun->selectedPrototype = selector;
            
            Sandbox.RemoveRadialMenu(f, humanoid);
        }

        
        if (disablePrimary) return;


        if (toolgun->cooldown > 0 && !input->trigger.IsDown)
            toolgun->cooldown = 1;
        
        if (toolgun->cooldown > 0)
        {
            input->trigger._frameUp = 1;
            input->trigger._frameDown = 0;
        }
            
        // Primary Fire

        if (input->trigger.IsDown)
        {
            if (toolgun->cooldown == 0)
            {
                input->trigger._frameUp = 1;
                input->trigger._frameDown = 0;

                var humanoidPosition = f.Get<Transform3D>(humanoid).Position;

                FPVector3 aimDir = player.cameraState.AimDir() * FP._100;
                FPVector3 spawnPosition = humanoidPosition + aimDir;

                toolgun->cooldown = FIRE_RATE;

                if (Raycasts.StaticTerrainLineCast(f, humanoidPosition, spawnPosition, out Hit3D hit))
                    spawnPosition =  hit.Point + hit.Normal * FP._0_33;

                toolgun->SpawnPrototype(f, spawnPosition);

                Sandbox .Log.Msg($"focusPos: {player.cameraState.focusPos}");

                Humanoid_View view = UnityEngine.GameObject.Find(humanoid.ToString()).GetComponent<Humanoid_View>();
                view.holdWeaponParams.weaponAnimationType = Humanoid_View.HoldWeaponParams.WeaponAnimationType.SmallGun;

                view.AddAnimation(new GunAnimation(view));

                view.gunAnimation.eqID = EquipmentID.Revolver;
                view.gunAnimation.Shoot(FPMathUtils.ToUnityVector3(aimDir), 0.1f, 25.0f, 0.1f);

                Sounds.PlaySound(0, 4, 2.5f, 1.0f, FPMathUtils.ToUnityVector3(humanoidPosition));
            }
        }

        toolgun->cooldown = Math.Max(toolgun->cooldown - 1, 0);
    }

    public readonly void SpawnPrototype(Frame f, FPVector3 position)
        => selectedPrototype.IndexMenu(SpawnMenu.Init(f)).Spawn(f, position);


    public static EntityRef Create(Frame f, FPVector3 position)
    {
        var eRef = f.Create(f.EquipmentConfig().revolver);

        ((Toolgun*)f.GetPointer<Equipment>(eRef))->CustomId = 1000;
        f.Remove<Gun>(eRef);
        f.CustomSet(eRef, new Toolgun());
        f.GetPointer<Transform3D>(eRef)->Position = position;

        return eRef;
    }
}