using System.Runtime.InteropServices;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using JPInstaller.Custom;

namespace AfuSandbox;

internal static class Noclip
{
    internal unsafe static void NoClip(Frame f, EntityRef entity, Player* player)
    {
        // Flight toggle

        var input = player->unpackedInput;
        uint btnMask = 0b100010; // Jump and Crouch

        if (f.CustomTryGetPointer<NoclipController>(entity, out var controller))
            {
                if (player->holdBuy >= 10)
                {
                    Sandbox.Log.Msg("holdBuy");
                    unsafe {
                        controller->StopFlying = true;
                    }
                }

                NoClipMove(f, entity, player, controller);
            }
            else
            {
                if (player->holdBuy >= 10 && (input.buttonsHeld & btnMask) == btnMask)
                {
                    player->holdBuy = -1;
                    Sandbox.Log.Msg($"holdBuy2 {player->holdBuy}");

                    (bool onBike, EntityRef eRef) controlled = (false, player->controlledEntity);

                    if (f.Get<Humanoid>(controlled.eRef).vehicle is EntityRef vehicle && f.Exists(vehicle))
                        controlled = (true, vehicle);
                    
                    f.CustomSet(entity, new NoclipController {
                        Player = entity,
                        Controlled = controlled.eRef,
                        Speed = controlled.onBike ? FP._1_50 : FP._1,
                    });
                }
            }
    }

    internal unsafe static void NoClipMove(Frame f, EntityRef entity, Player* player, NoclipController* controller)
    {
        // The Entity we want to control, could be anything with a Transform3D and PhysicsBody3D
        var input = player->unpackedInput;
        EntityRef controlledEntity = controller->Controlled;

        if (!f.Exists(controlledEntity))
        {
            f.CustomRemove<NoclipController>(entity);
            return;
        }

        // Handle translation

        Transform3D* t = f.GetPointer<Transform3D>(controlledEntity);

        FPMath.SinCos(player->cameraState.aimAngles.X * FP.Deg2Rad, out var sin, out var cos);

        var forw = new FPVector3(sin, FP._0, cos) * input.leftStick.Y;
        var rigt = new FPVector3(cos, FP._0, -sin) * input.leftStick.X;

        var vert = FPVector3.Up * (
            (input.buttonsHeld & 0b10) != 0 ? FP._1 : FP._0 // If Jumping
            + (input.buttonsHeld & 0b100000) != 0 ? -FP._1 : FP._0 // If Crouching
        );

        var speed = (input.buttonsHeld & 0b1000) != 0 ? FP._1_50 : FP._0_25 + FP._0_05; // If Sprinting
        speed = (input.buttonsHeld & 0b10000000) != 0 ? FP._0_05 : speed; // If Braking

        var moveVector = (rigt + forw + vert) * speed * controller->Speed;

        t->Position += moveVector;

        // Handle Velocity

        var vel = FPVector3.Up * FP._0_05; // Small up vector to counteract gravity

        // Convert all movement into velocity when flight gets disabled
        if (controller->StopFlying) vel += moveVector / f.DeltaTime;
        // Otherwise just add a little velocity for fx
        else vel += moveVector.Normalized * (controller->Speed * FP._0_10) / f.DeltaTime;

        f.GetPointer<PhysicsBody3D>(controlledEntity)->Velocity = vel;

        if (controller->StopFlying) f.CustomRemove<NoclipController>(entity);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NoclipController() : JPInstaller.Custom.IComponent
{
    // public const int SIZE = 24;
    // public const int ALIGNMENT = 8;

    private int CustomId;

    public bool StopFlying;
    public FP Speed = FP._1;
    public EntityRef Player;
    public EntityRef Controlled;

    public JPInstaller.Custom.IComponent SetID(int id)
    {
        CustomId = id;
        return this;
    }
}
