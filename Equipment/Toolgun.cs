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

    public int selectedSubmenu = 0;
    public int selectedPrototype = 0;

    public const int FIRE_RATE = 27;
    public int cooldown = 0;

    public JPInstaller.Custom.IComponent SetID(int id)
    {
        CustomId = id;
        return this;
    }


    unsafe public static void SimulateHeld(Frame f, Input* input, EntityRef humanoid, EntityRef pickup)
    {
        var toolgun = f.CustomGetPointer<Toolgun>(pickup);

        if (toolgun->cooldown > 0 && !input->trigger.IsDown)
            toolgun->cooldown = 1;
        
        if (toolgun->cooldown > 0)
        {
            var button = input->trigger;

            button._frameUp = 1;
            button._frameDown = 0;

            input->trigger = button;
        }
            
        // Sandbox .Log.Msg($"{toolgun->cooldown}, {input->trigger._frameCurrent}, {input->trigger._frameUp}, {input->trigger._frameDown}");

        if (input->trigger.IsDown)
        {
            if (toolgun->cooldown == 0)
            {
                toolgun->cooldown = FIRE_RATE;
                toolgun->SpawnPrototype(f, f.Get<Transform3D>(humanoid).Position);
            }
        }

        toolgun->cooldown = Math.Max(toolgun->cooldown - 1, 0);

        if (toolgun->cooldown > 0)
        {
            var button = input->trigger;

            button._frameUp = 1;
            button._frameDown = 0;

            input->trigger = button;
        }
    }

    public readonly void SpawnPrototype(Frame f, FPVector3 position)
    {
        AssetRef<EntityPrototype> prototype = null;
        var spawnMenu = SpawnMenu.Init(f);

        switch (spawnMenu.Get(selectedSubmenu, out var subMenu, out var entry))
        {
            case EntryType.SubMenu:
                switch (subMenu.Get(selectedPrototype, out _, out var subEntry))
                {
                    case EntryType.Entry:
                        prototype = subEntry;
                        break;
                }
            break;

            case EntryType.Entry:
                prototype = entry;
            break;
        }

        if (prototype is null) return;

        f.Set(f.Create(prototype), Transform3D.Create(position));
    }


    public static EntityRef Create(Frame f, FPVector3 position)
    {
        var eRef = f.Create(f.EquipmentConfig().revolver);

        ((Toolgun*)f.GetPointer<Equipment>(eRef))->CustomId = 1002;
        f.Remove<Gun>(eRef);
        f.CustomSet(eRef, new Toolgun());

        eRef = f.Create(f.EquipmentConfig().revolver);

        ((Toolgun*)f.GetPointer<Equipment>(eRef))->CustomId = 1001;
        f.Remove<Gun>(eRef);
        f.CustomSet(eRef, new Toolgun());

        eRef = f.Create(f.EquipmentConfig().revolver);

        ((Toolgun*)f.GetPointer<Equipment>(eRef))->CustomId = 1000;
        f.Remove<Gun>(eRef);
        f.CustomSet(eRef, new Toolgun());

        return eRef;
    }
}