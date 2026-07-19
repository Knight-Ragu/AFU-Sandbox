using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppHUD;
using Il2CppQuantum;
using Il2CppUI_Localization;
using Il2CppView_Access;
using Il2CppView_Entities;
using JPInstaller;

namespace AfuSandbox;

public static class CustomEquipment
{
    const int ZERO_ID = 999;
    private readonly static Dictionary<int, EquipmentData> customEquipment = [];


    unsafe public static int AsInt(this EquipmentID eqID)
        => ((EquipmentIdAsInt*)&eqID)->Integer;
    
    struct EquipmentIdAsInt()
    {
        internal int Integer;
    }
    
    unsafe public static bool IsCustom(this EquipmentID eqID)
        => eqID.AsInt() > ZERO_ID;

    public static void RegisterEquipment(EquipmentData equipmentData)
    {
        int highestID = ZERO_ID;

        foreach (var keyValuePair in customEquipment)
            if (keyValuePair.Key > highestID)
                highestID = keyValuePair.Key;
        
        customEquipment.Add(highestID + 1, equipmentData);
    }

    public static bool TryGetEquipmentData(EquipmentID eqID, out EquipmentData equipmentData)
        => customEquipment.TryGetValue(eqID.AsInt(), out equipmentData);

    public static bool IsWeapon(this EquipmentID eqID)
    {
        int equipmentID = eqID.AsInt();

        if (equipmentID > ZERO_ID && customEquipment.TryGetValue(eqID.AsInt(), out var equipmentData))
            return equipmentData.Type == HUD_Access.EquipmentColor.Weapon;
        else
            return EquipmentExtensions.IsWeapon(eqID);
    }

    public static bool IsSecondary(this EquipmentID eqID)
    {
        int equipmentID = eqID.AsInt();

        if (equipmentID > ZERO_ID && customEquipment.TryGetValue(eqID.AsInt(), out var equipmentData))
            return equipmentData.Type == HUD_Access.EquipmentColor.Secondary;
        else
            return EquipmentExtensions.IsSecondary(eqID);
    }

    public static bool IsMisc(this EquipmentID eqID)
    {
        int equipmentID = eqID.AsInt();

        if (equipmentID > ZERO_ID && customEquipment.TryGetValue(eqID.AsInt(), out var equipmentData))
            return equipmentData.Type == HUD_Access.EquipmentColor.Misc;
        else
            return EquipmentExtensions.IsMisc(eqID);
    }
}

public class EquipmentData()
{
    public string Name = "CustomEquipment";
    public HUD_Access.EquipmentColor Type = HUD_Access.EquipmentColor.None;

    public unsafe Action<Frame, IntPtr, EntityRef, EntityRef> OnHeld;
}

////////////////////////////////////////////////////////////////////
// Workarounds and fixes to make custom Equipment work
////////////////////////////////////////////////////////////////////

// Make this method recognize our custom equipments, fixes most weapon functionality

[HarmonyPatch(typeof(EquipmentExtensions), nameof(EquipmentExtensions.TryGetWeapon))]
class EquipmentExtensions_TryGetWeapon_Patch
{
    unsafe static void Postfix(ref bool __result, Frame f, EntityRef humanoidEntity, ref EntityRef weaponEntity)
    {
        if (!__result)
        {
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    f.Has<HeldByHumanoid>(entity)
                && f.Get<HeldByHumanoid>(entity).humanoidEntity == humanoidEntity
                && CustomEquipment.IsWeapon(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    weaponEntity = entity;
                    __result = true;
                    return;
                }
        }
    }
}

// Make this method recognize our custom equipments, fixes most secondary (throwables) functionality

[HarmonyPatch(typeof(EquipmentExtensions), nameof(EquipmentExtensions.TryGetSecondary))]
class EquipmentExtensions_TryGetSecondary_Patch
{
    unsafe static void Postfix(ref bool __result, Frame f, EntityRef humanoidEntity, ref EntityRef secondaryEntity)
    {
        if (!__result)
        {
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    f.Has<HeldByHumanoid>(entity)
                && f.Get<HeldByHumanoid>(entity).humanoidEntity == humanoidEntity
                && CustomEquipment.IsSecondary(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    secondaryEntity = entity;
                    __result = true;
                    return;
                }
        }
    }
}

// Make this method recognize our custom equipments, fixes most misc (special) functionality

[HarmonyPatch(typeof(EquipmentExtensions), nameof(EquipmentExtensions.TryGetMisc))]
class EquipmentExtensions_TryGetMisc_Patch
{
    unsafe static void Postfix(ref bool __result, Frame f, EntityRef humanoidEntity, ref EntityRef miscEntity)
    {
        if (!__result)
        {
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    f.Has<HeldByHumanoid>(entity)
                && f.Get<HeldByHumanoid>(entity).humanoidEntity == humanoidEntity
                && CustomEquipment.IsMisc(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    miscEntity = entity;
                    __result = true;
                    return;
                }
        }
    }
}

// Fix to make the player drop the equipment they're curently holding when picking up a custom one of the same type

[HarmonyPatch(typeof(EquipmentExtensions), nameof(EquipmentExtensions.GrabEquipment))]
class EquipmentExtensions_GrabEquipment_Patch
{
    unsafe static void Prefix(Frame f, EntityRef holderEntity, EntityRef equipmentEntity)
    {        
        var eqID = f.Get<Il2CppQuantum.Equipment>(equipmentEntity).eqID;
        if (!eqID.IsCustom()) return;
            
        if (eqID.IsWeapon())
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    entity != equipmentEntity
                 && f.Has<HeldByHumanoid>(entity)
                 && f.Get<HeldByHumanoid>(entity).humanoidEntity == holderEntity
                 && CustomEquipment.IsWeapon(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    EquipmentExtensions.DropEquipment(f, entity);
                    return;
                }
        
        if (eqID.IsSecondary())
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    entity != equipmentEntity
                 && f.Has<HeldByHumanoid>(entity)
                 && f.Get<HeldByHumanoid>(entity).humanoidEntity == holderEntity
                 && CustomEquipment.IsSecondary(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    EquipmentExtensions.DropEquipment(f, entity);
                    return;
                }
        
        if (eqID.IsMisc())
            foreach (var entity in Sandbox._eRefs.Iter())
                if (
                    entity != equipmentEntity
                 && f.Has<HeldByHumanoid>(entity)
                 && f.Get<HeldByHumanoid>(entity).humanoidEntity == holderEntity
                 && CustomEquipment.IsMisc(f.Get<Il2CppQuantum.Equipment>(entity).eqID)
                ) {
                    EquipmentExtensions.DropEquipment(f, entity);
                    return;
                }
    }
}

// Make custom ids return custom names

[HarmonyPatch(typeof(DisplayNames), nameof(DisplayNames.GetEquipmentName))]
class DisplayNames_GetEquipmentName_Patch
{
    static unsafe void Postfix(ref string __result, EquipmentID eqID)
    {
        if (CustomEquipment.TryGetEquipmentData(eqID, out var data))
            __result = data.Name;
    }
}

// Fix to make custom pickups have the correct name color

[HarmonyPatch(typeof(Pickup_View), nameof(Pickup_View.GetHUDEquipmentColor))]
class Pickup_View_GetHUDEquipmentColor_Patch
{
    unsafe static void Postfix(ref HUD_Access.EquipmentColor __result, EquipmentID eqID)
    {
        if (CustomEquipment.TryGetEquipmentData(eqID, out var data))
            __result = data.Type;
    }
}

// Fix to make custom pickups have the correct particle color

[HarmonyPatch(typeof(Pickup_View), nameof(Pickup_View.UIColor))]
class Pickup_View_UIColor_Patch
{
    unsafe static void Postfix(ref UnityEngine.Color __result, EquipmentID eqID)
    {
        if (CustomEquipment.TryGetEquipmentData(eqID, out var data))
            __result = HUD_Access.GetEquipmentColor(data.Type);
    }
}

// Fix to make custom pickups have the correct outline color

[HarmonyPatch(typeof(Pickup_View), nameof(Pickup_View.EffectColor))]
class Pickup_View_EffectColor_Patch
{
    unsafe static void Postfix(ref UnityEngine.Color __result, EquipmentID eqID)
    {
        if (CustomEquipment.TryGetEquipmentData(eqID, out var data))
            __result = HUD_Access.GetEquipmentColor(data.Type);
    }
}
