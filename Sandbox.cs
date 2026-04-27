using HarmonyLib;
using Il2CppPhoton.Deterministic;
using Il2CppQuantum;
using Il2CppQuantum.Core;
using MelonLoader;

[assembly: MelonInfo(typeof(AfuSandbox.Sandbox), "Noclip", "0.0.1", "Knight-Ragu", null)]
[assembly: MelonGame("Videocult", "Airframe")]

namespace AfuSandbox;

public partial class Sandbox : MelonMod
{
    internal static MelonLogger.Instance Log => Melon<Sandbox>.Instance.LoggerInstance;


    [HarmonyPatch(typeof(FrameContext), "OnFrameSimulationBegin")]
    private class Simulate
    {
        public static void Postfix(FrameBase f)
        {            
            Il2CppSystem.Collections.Generic.List<EntityRef> refs = new();
            f.GetAllEntityRefs(refs);

            unsafe { // Toggle flight

            foreach (var r in refs)
                if (f.Has<Player>(r))
            {
                Player* player = f.GetPointer<Player>(r);
                if (!f.Exists(player->controlledEntity)) continue;

                // Log.Msg($"inputs: {player.cameraState.firstPerson}, {player.cameraState.firstPerson.Value}");
                // Log.Msg($"inputs: {Convert.ToString(player.unpackedInput.buttonsHeld, 2)}");

                var input = player->unpackedInput;

                // Flight toggle

                uint btnMask = 0b100010; // Jump and Crouch

                if (!f.Has<FoamBlob>(r))
                {
                    if (player->holdBuy >= 10 && (input.buttonsHeld & btnMask) == btnMask)
                    {
                        player->holdBuy = -1;

                        (bool onBike, EntityRef eRef) controlled = (false, player->controlledEntity);

                        if (f.Get<Humanoid>(controlled.eRef).vehicle is EntityRef vehicle && f.Exists(vehicle))
                            controlled = (true, vehicle);
                        
                        unsafe {
                            var v = new NoclipController {
                                Player = r,
                                Controlled = controlled.eRef,
                                Speed = controlled.onBike ? FP._1_50 : FP._1,
                            };

                            f.Set(r, 53, &v);
                        }
                    }
                }
                else
                    if (player->holdBuy >= 10)
                    {
                        unsafe {
                            ((NoclipController*)f.GetPointer<FoamBlob>(r))->StopFlying = true;
                        }
                    }
            }}
        
            unsafe { // Flight
                
            foreach (var r in refs)
                if (f.Has<FoamBlob>(r))
            {
                NoclipController* controller = (NoclipController*)f.GetPointer<FoamBlob>(r);
                Player player = f.Get<Player>(controller->Player);
                var input = player.unpackedInput;

                // The Entity we want to control, could be anything with a Transform3D and PhysicsBody3D
                EntityRef controlledEntity = controller->Controlled;

                if (!f.Exists(controlledEntity))
                {
                    f.Remove<FoamBlob>(r);
                    continue;
                }

                // Handle translation

                Transform3D* t = f.GetPointer<Transform3D>(controlledEntity);

                FPMath.SinCos(player.cameraState.aimAngles.X * FP.Deg2Rad, out var sin, out var cos);

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

                if (controller->StopFlying) f.Remove<FoamBlob>(r);
            }}
        }
    }
}

public struct NoclipController
{
    // public const int SIZE = 24;
    // public const int ALIGNMENT = 8;

    public NoclipController() {}

    public bool StopFlying;
    public FP Speed = FP._1;
    public EntityRef Player;
    public EntityRef Controlled;
}
