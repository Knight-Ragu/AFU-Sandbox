using System;
using System.Runtime.InteropServices;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
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


    public static unsafe void OnHeld(Frame f, IntPtr inputPointer, EntityRef humanoid, EntityRef pickup)
    {
        Input* input = (Input*)inputPointer;
        var toolgun = f.CustomGetPointer<Toolgun>(pickup);
        bool disablePrimary = false;

        if (input->weaponSecondary.WasPressed)
            f.CustomSet(humanoid, new RadialMenuSelector());
        
        if (input->weaponSecondary.IsDown)
            disablePrimary = true;

        if (input->weaponSecondary.WasReleased && f.CustomTryGet<RadialMenuSelector>(humanoid, out var selection))
        {
            toolgun->selectedPrototype = selection;
            
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
            
        // Sandbox .Log.Msg($"{toolgun->cooldown}, {input->trigger._frameCurrent}, {input->trigger._frameUp}, {input->trigger._frameDown}");

        if (input->trigger.IsDown)
        {
            if (toolgun->cooldown == 0)
            {
                toolgun->cooldown = FIRE_RATE;
                toolgun->SpawnPrototype(f, f.Get<Transform3D>(humanoid).Position);

                input->trigger._frameUp = 1;
                input->trigger._frameDown = 0;
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

        return eRef;
    }
}