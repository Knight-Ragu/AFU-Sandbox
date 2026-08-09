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
    public const byte NO_SELECTION = byte.MaxValue;

    public unsafe fixed byte Selections[MAX_LAYERS];
    public byte Depth = 1;

    public FPVector2 Cursor = FPVector2.Zero;

    private byte _refreshedUI = 0;


    public JPInstaller.Custom.IComponent SetID(int id)
    {
        CustomID = id;
        return this;
    }
     
    public unsafe byte GetLayerSelection(int layer, int length)
    {
        if (layer >= MAX_LAYERS) return NO_SELECTION;

        byte index = 0;

        fixed (byte* selections = this.Selections)
            index = selections[layer % length];

        return index;
    }

    public unsafe byte GetCurrentSelection(int length)
        => this.GetLayerSelection(this.Depth, length);

    public unsafe bool WasCancelledEarly()
    {
        bool WasCancelledEarly = false;

        for (int i = 0; i < this.Depth; i++)
            WasCancelledEarly = this.Selections[i] == NO_SELECTION;

        return WasCancelledEarly;
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
        if (this.Depth == 0)
            this.Depth = 1;

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
                selected = FPMath.FloorToInt((angle * FP.FromFloat_UNSAFE((float)entryCount)) - FP._0_10);
            }
            else if (cursorMagnitude > FP._0_10)
            {
                if (selected == NO_SELECTION && input->trigger.WasPressed)
                {
                    selector->Depth--;
                    selector->_refreshedUI = 0;
                }          
            }

            selector->Selections[selector->Depth - 1] = (byte)selected;

            if (selected != NO_SELECTION && input->trigger.WasPressed)
            {
                selector->Depth++;
                selector->_refreshedUI = 0;
            }

            // Gfx stuff

            FPVector3 aimDir = player->cameraState.AimDir();
            FPVector3 humanoidPosition = f.Get<Transform3D>(player->controlledEntity).Position;

            var gfx = Sandbox.radialMenuGfx.GetOrCreate(player->controlledEntity, k => {
                var ret = RadialMenuGfx.Create(player->controlledEntity);
                ret.transform.forward = -aimDir.ToUnityVector3();
                return ret;
            });

            int len = selector->CurrentSelectionEntryCount(menu);

            gfx.SelectedSection = selected;
            gfx.CursorPosition = selector->Cursor.ToUnityVector2();
            gfx.Position = (humanoidPosition + (FPVector3.Up * (FP._0_50 + FP._0_10)) + aimDir).ToUnityVector3();
            gfx.Forward = -aimDir.ToUnityVector3();
            
            if (selector->_refreshedUI == 0)
            {
                selector->_refreshedUI++;
                gfx.QueueCreateSections(len);
                gfx.transform.position = f.Get<Transform3D>(player->controlledEntity).Position.ToUnityVector3();
            }
        }
    }
}
