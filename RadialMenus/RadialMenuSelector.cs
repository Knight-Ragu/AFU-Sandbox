using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Il2CppInput;
using Il2CppInterop.Generator.Extensions;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using Il2CppQuantum_Core;
using JPInstaller.Custom;

namespace AfuSandbox;

[StructLayout(LayoutKind.Sequential)]
public struct RadialMenuSelector() : JPInstaller.Custom.IComponent
{
    private int CustomID;

    public const int MAX_LAYERS = 8;
    public const byte NO_SELECTION = 255;

    public unsafe fixed byte Selections[MAX_LAYERS];
    public byte Depth = 1;

    public FPVector2 Cursor = FPVector2.Zero;

    private byte _refreshedUI = 0;


    public JPInstaller.Custom.IComponent SetID(int id)
    {
        CustomID = id;
        return this;
    }

    unsafe public byte GetLayerSelection(int layer, int length)
    {
        if (layer >= MAX_LAYERS) return byte.MaxValue;

        byte index = 0;

        fixed (byte* selections = this.Selections)
            index = selections[layer % length];

        return index;
    }

    public T IndexMenu<T>(RadialMenu<T> menu)
    {
        RadialMenu<T> currentLayer = menu;

        for (int i = 0; i < MAX_LAYERS; i++)
        {
            switch (currentLayer.Get(this.GetLayerSelection(i, currentLayer.Count), out var subMenu, out var entry))
            {
                case EntryType.Entry:
                    return entry;

                case EntryType.SubMenu:
                    currentLayer = subMenu;
                continue;
            }
        }

        // Reaching this point should be impossible
        // Since to never hit the return in the switch, it would have to run into an empty sub-RadialMenu
        // Which should be impossible, because the RadialMenus don't let you add empty sub-RadialMenus
        // And you cannot remove elements from RadialMenus
        throw new InvalidProgramException("Indexing RadialMenu ran into an empty sub-menu!");
    }

    public int CurrentSelectionEntryCount<T>(RadialMenu<T> menu)
    {
        RadialMenu<T> currentLayer = menu;

        for (int i = 0; i < Math.Min(Depth, (byte)MAX_LAYERS); i++)
        {
            if (i == this.Depth - 1) return currentLayer.Count;

            switch (currentLayer.Get(this.GetLayerSelection(i, currentLayer.Count), out var subMenu, out _))
            {
                case EntryType.SubMenu:
                    currentLayer = subMenu;
                continue;

                case EntryType.Entry:
                    return currentLayer.Count;
            }
        }

        throw new InvalidProgramException("nope");
    }



    internal static unsafe void Simulate(Frame f, Player* player, Input* input)
    {
        if (f.CustomTryGetPointer<RadialMenuSelector>(player->controlledEntity, out var selector))
        {
            SpawnMenu menu = SpawnMenu.Init(f);

            bool KbmMode = input->KBM_MODE.IsDown;

            FPVector2 rStick = FetchInputSystem.SixteenBitsToVector2(input->sticks >> 16);
            input->sticks =
                UnityEngine.GameObject.FindFirstObjectByType<PlayerInputHandler>().Vector2To16Bits(UnityEngine.Vector2.zero) << 16
                & (input->sticks << 16) >> 16;

            int selected = NO_SELECTION;
            int entryCount = selector->CurrentSelectionEntryCount(menu);

            FP cursorMagnitude;

            if (KbmMode)
            {
                selector->Cursor += rStick;
                cursorMagnitude = selector->Cursor.Magnitude;

                if (cursorMagnitude > FP._1)
                    selector->Cursor = selector->Cursor.Normalized;
            }
            else
            {
                selector->Cursor = rStick;
                cursorMagnitude = selector->Cursor.Magnitude;
            }

            if (cursorMagnitude > FP._0_50)
            {
                FP angle = (FPMath.Atan2(selector->Cursor.X, -selector->Cursor.Y) / FP.PiTimes2) + FP._0_50;
                selected = FPMath.FloorToInt(angle * FP.FromFloat_UNSAFE((float)entryCount));
            }
            else if (cursorMagnitude > FP._0_05)
            {
                // Adding stuff here later                
            }

            selector->Selections[selector->Depth - 1] = (byte)selected;

            if (selected != NO_SELECTION && input->trigger.WasPressed)
            {
                selector->Depth++;

                selector->_refreshedUI = 0;
                entryCount = selector->CurrentSelectionEntryCount(menu);

                for (int i = 0; i < MAX_LAYERS; i++)
                    Sandbox .Log.Msg($"layer{i}: {selector->Selections[i]}");
            }

            // Gfx stuff

            FPMath.SinCos(player->cameraState.aimAngles.X * FP.Deg2Rad, out var sin, out var cos);
            var forw = new FPVector3(sin, FP._0, cos);

            var gfx = Sandbox.radialMenuGfx.GetOrCreate(player->controlledEntity, k => {
                var ret = RadialMenuGfx.Create(player->controlledEntity);
                ret.transform.right = forw.ToUnityVector3();
                return ret;
            });

            gfx.SelectedSection = selected;
            gfx.CursorPosition = selector->Cursor.ToUnityVector2();
            gfx.Position = (f.Get<Transform3D>(player->controlledEntity).Position + forw + FPVector3.Up * FP._0_33).ToUnityVector3();
            gfx.Forward = -forw.ToUnityVector3();
            
            if (selector->_refreshedUI == 0)
            {
                selector->_refreshedUI++;
                gfx.QueueCreateSections(entryCount);
                gfx.transform.position = f.Get<Transform3D>(player->controlledEntity).Position.ToUnityVector3();
            }
        }
    }
}
