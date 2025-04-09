using BetterStacks;
using HarmonyLib;
using MelonLoader;

using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Stations;
using Il2CppScheduleOne.UI.Shop;

[assembly: MelonInfo(typeof(BetterStacksMod), "Better Stacks", "2.0.0", "Zarnes")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterStacks;

public class BetterStacksMod : MelonMod
{
    private static ModConfig _config = new ModConfig();
    private static HashSet<ItemDefinition> _alreadyModifiedItems = new HashSet<ItemDefinition>();

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
        //harmony.Patch(
        //    AccessTools.Method(typeof(ItemInstance), "SetQuantity"),
        //    prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(SetQuantityPatch))
        //);

        // Patch ChangeQuantity
        //harmony.Patch(
        //    AccessTools.Method(typeof(ItemInstance), "ChangeQuantity"),
        //    prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(ChangeQuantityPatch))
        //);

        // Patch Mixing Station capacity
        harmony.Patch(
            AccessTools.Method(typeof(MixingStation), "Start"),
            prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(PatchMixingStationCapacity))
        );

        // Patch Drying Rack capacity
        harmony.Patch(
            AccessTools.Method(typeof(DryingRackCanvas), "SetIsOpen"),
            prefix: new HarmonyMethod(typeof(BetterStacksMod), nameof(PatchDryingRackCapacity))
        );

        //Patch Delivery stack limit
        harmony.Patch(
            AccessTools.Method(typeof(ListingEntry), "Initialize"),
            postfix: new HarmonyMethod(typeof(BetterStacksMod), nameof(InitializeListingEntryPatch))
        );
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
        if (!_alreadyModifiedItems.Contains(__instance.Definition))
        {
            EItemCategory cat = __instance.Category;
            __result *= GetCapacityModifier(cat);
        }
    }

    //private static bool SetQuantityPatch(ItemInstance __instance, int quantity)
    //{
    //    int stackLimit = __instance.StackLimit;
    //    //MelonLogger.Msg($"SetQuantity called on {__instance.Name}, stack limit is {stackLimit}");

    //    if (quantity < 0)
    //    {
    //        MelonLogger.Error("SetQuantity called with negative quantity");
    //        return false;
    //    }
    //    quantity = Math.Min(quantity, stackLimit);
    //    __instance.Quantity = quantity;
    //    __instance.InvokeDataChange();
    //    return false;
    //}

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

    public static bool PatchMixingStationCapacity(MixingStation __instance)
    {
        __instance.MixTimePerItem /= _config.MixingStationSpeed;
        __instance.MaxMixQuantity = __instance.MaxMixQuantity * _config.MixingStationCapacity;
        //MelonLogger.Msg($"Set mixing station capacity to {__instance.MaxMixQuantity}");
        return true;
    }

    public static void PatchDryingRackCapacity(DryingRackCanvas __instance, DryingRack rack, bool open)
    {
        //MelonLogger.Msg($"On DryingRackCanvas.SetIsOpen");
        if (rack is not null && rack.ItemCapacity != _config.Product)
        {
            rack.ItemCapacity = _config.DryingRackCapacity * 20;
            //MelonLogger.Msg($"Set drying rack capacity to {rack.ItemCapacity}");
        }
    }

    public static bool DeliveryLimitPatch(DeliveryShop __instance)
    {
        int totalStacks = 0;
        foreach (ListingEntry? listingEntry in __instance.listingEntries._items)
        {
            if (listingEntry is null || listingEntry.SelectedQuantity == 0)
                continue;

            StorableItemDefinition item = listingEntry.MatchingListing.Item;
            int stackCapacity = item.StackLimit * GetCapacityModifier(item.Category);
            int stacksNeeded = (int) Math.Ceiling((double)listingEntry.SelectedQuantity / stackCapacity);
            totalStacks += stacksNeeded;
        }

        MelonLogger.Msg($"Order need {totalStacks} stacks");
        return totalStacks <= DeliveryShop.DELIVERY_VEHICLE_SLOT_CAPACITY;
    }

    public static void InitializeListingEntryPatch(ListingEntry __instance, ShopListing match)
    {
        StorableItemDefinition item = __instance.MatchingListing.Item;
        if (!_alreadyModifiedItems.Contains(item))
        {
            _alreadyModifiedItems.Add(item);
            int originalStackLimit = item.StackLimit;
            item.StackLimit = originalStackLimit * GetCapacityModifier(item.Category);
            MelonLogger.Msg($"Set {item.Name} shop listing stack limit from {originalStackLimit} to {item.StackLimit}");
        }
    }

    private static int GetCapacityModifier(EItemCategory category)
    {
        switch (category)
        {
            case EItemCategory.Product:
                return _config.Product;
            case EItemCategory.Packaging:
                return _config.Packaging;
            case EItemCategory.Growing:
                return _config.Growing;
            case EItemCategory.Tools:
                return _config.Tools;
            case EItemCategory.Furniture:
                return _config.Furniture;
            case EItemCategory.Lighting:
                return _config.Lighting;
            case EItemCategory.Cash:
                return _config.Cash;
            case EItemCategory.Consumable:
                return _config.Consumable;
            case EItemCategory.Equipment:
                return _config.Equipment;
            case EItemCategory.Ingredient:
                return _config.Ingredient;
            case EItemCategory.Decoration:
                return _config.Decoration;
            case EItemCategory.Clothing:
                return _config.Clothing;
            default:
                return 1;
        }
    }
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

    public int MixingStationCapacity { get; set; } = 1;
    public int MixingStationSpeed { get; set; } = 3;

    public int DryingRackCapacity { get; set; } = 1;
}