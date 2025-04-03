using BetterStacks;
using HarmonyLib;
using MelonLoader;

using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Il2CppScheduleOne.UI.Phone.Delivery;

[assembly: MelonInfo(typeof(BetterStacksMod), "Better Stacks", "1.0.0", "Zarnes")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterStacks;

public class BetterStacksMod : MelonMod
{
    private const int STACK_CAP = 200;
    private static ModConfig _config = new ModConfig();

    public override void OnInitializeMelon()
    {
        _config = LoadConfig();

        var harmony = new HarmonyLib.Harmony("com.zarnes.betterstacks");

        // Patch StackLimit
        harmony.Patch(
            AccessTools.PropertyGetter(typeof(ItemInstance), "StackLimit"),
            postfix: new HarmonyMethod(typeof(BetterStacksMod), nameof(StackLimitPatch))
        );

        // Patch SetQuantity
        harmony.Patch(
            AccessTools.Method(typeof(ItemInstance), "SetQuantity"),
            prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(SetQuantityPatch))
        );

        // Patch ChangeQuantity
        harmony.Patch(
            AccessTools.Method(typeof(ItemInstance), "ChangeQuantity"),
            prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(ChangeQuantityPatch))
        );

        // Patch MixingStation Start
        harmony.Patch(
            AccessTools.Method(typeof(MixingStation), "Start"),
            prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(MixingStationStartPath))
        );

        // Patch DeliveryShop CanOrder
        //harmony.Patch(
        //    AccessTools.Method(typeof(DeliveryShop), "CanOrder"),
        //    prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(CanOrderPatch))
        //);
    }

    public static ModConfig LoadConfig()
    {
        string configPath = Path.Combine(MelonEnvironment.ModsDirectory, "BetterStackConfig.json");

        if (File.Exists(configPath))
        {
            string jsonContent = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<ModConfig>(jsonContent);
        }
        else
        {
            MelonLogger.Warning($"Config file not found: {configPath}");
            return new ModConfig(); // Returns default values (all 1s)
        }
    }

    private static void StackLimitPatch(ItemInstance __instance, ref int __result)
    {
        EItemCategory cat = __instance.Category;
        switch (cat)
        {
            case EItemCategory.Product:
                __result *= _config.Product;
                break;
            case EItemCategory.Packaging:
                __result *= _config.Packaging;
                break;
            case EItemCategory.Growing:
                __result *= _config.Growing;
                break;
            case EItemCategory.Tools:
                __result *= _config.Tools;
                break;
            case EItemCategory.Furniture:
                __result *= _config.Furniture;
                break;
            case EItemCategory.Lighting:
                __result *= _config.Lighting;
                break;
            case EItemCategory.Cash:
                __result *= _config.Cash;
                break;
            case EItemCategory.Consumable:
                __result *= _config.Consumable;
                break;
            case EItemCategory.Equipment:
                __result *= _config.Equipment;
                break;
            case EItemCategory.Ingredient:
                __result *= _config.Ingredient;
                break;
            case EItemCategory.Decoration:
                __result *= _config.Decoration;
                break;
            case EItemCategory.Clothing:
                __result *= _config.Clothing;
                break;
        }
    }

    private static bool SetQuantityPatch(ItemInstance __instance, int quantity)
    {
        int stackLimit = __instance.StackLimit;
        //MelonLogger.Msg($"SetQuantity called on {__instance.Name}, stack limit is {stackLimit}");

        if (quantity < 0)
        {
            MelonLogger.Error("SetQuantity called with negative quantity");
            return false;
        }
        quantity = Math.Min(quantity, stackLimit);
        __instance.Quantity = quantity;
        __instance.InvokeDataChange();
        return false;
    }

    private static bool ChangeQuantityPatch(ItemInstance __instance, int change)
    {
        int num = __instance.Quantity + change;
        if (num < 0)
        {
            MelonLogger.Error("ChangeQuantity resulted in negative quantity");
            return false;
        }
        __instance.Quantity = num;
        __instance.InvokeDataChange();
        return false;
    }

    public static bool MixingStationStartPath(MixingStation __instance)
    {
        __instance.MixTimePerItem = 1;
        __instance.MaxMixQuantity = STACK_CAP;
        return true;
    }

    //public static bool CanOrderPatch(DeliveryShop __instance, ref string reason)
    //{
    //    reason = "";
    //    return true;
    //}
}

public class ModConfig
{
    public int Product { get; set; } = 1;
    public int Packaging { get; set; } = 1;
    public int Growing { get; set; } = 1;
    public int Tools { get; set; } = 1;
    public int Furniture { get; set; } = 1;
    public int Lighting { get; set; } = 1;
    public int Cash { get; set; } = 1;
    public int Consumable { get; set; } = 1;
    public int Equipment { get; set; } = 1;
    public int Ingredient { get; set; } = 1;
    public int Decoration { get; set; } = 1;
    public int Clothing { get; set; } = 1;
}
