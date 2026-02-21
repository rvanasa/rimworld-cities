using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cities {

    [StaticConstructorOnStartup]
    public static class Patches_RimCities {
        static Patches_RimCities() {
            var harmony = new Harmony("Cabbage.RimCities");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class Config_Cities {
        public static Config_Cities Instance =>
            LoadedModManager.GetMod<Mod_Cities>().GetSettings<ModSettings_Cities>().config;

        static readonly Config_Cities Defaults = new Config_Cities();

        public bool customCitySize = true;
        public int citySizeScale = 200;
        public bool enableQuestSystem = true;
        public bool enableDefendQuest = false;
        public bool enableEvents = true;
        public bool enableTurrets = true;
        public bool enableMortars = true;
        public bool enableLooting;
        public float lootScale = 1;
        public float abandonedChance = 0.3F;
        public IntRange citiesPer100kTiles = new IntRange(10, 15);
        public IntRange abandonedPer100kTiles = new IntRange(5, 10);
        public IntRange compromisedPer100kTiles = new IntRange(3, 6);
        public int minCitadelsPerWorld = 1;

        // public bool customMapSizes;
        public int customMapX = 200;
        public int customMapZ = 200;

        public void ExposeData() {
            Scribe_Values.Look(ref customCitySize, "limitCitySize", Defaults.customCitySize);
            Scribe_Values.Look(ref citySizeScale, "limitCitySizeScale", Defaults.citySizeScale);
            Scribe_Values.Look(ref enableQuestSystem, "enableQuestSystem", Defaults.enableQuestSystem);
            Scribe_Values.Look(ref enableDefendQuest, "enableDefendQuest", Defaults.enableDefendQuest);
            Scribe_Values.Look(ref enableEvents, "enableEvents", Defaults.enableEvents);
            Scribe_Values.Look(ref enableTurrets, "enableTurrets", Defaults.enableTurrets);
            Scribe_Values.Look(ref enableMortars, "enableMortars", Defaults.enableMortars);
            Scribe_Values.Look(ref enableLooting, "enableLooting", Defaults.enableLooting);
            Scribe_Values.Look(ref lootScale, "lootScale", Defaults.lootScale);
            Scribe_Values.Look(ref citiesPer100kTiles, "citiesPer100kTiles", Defaults.citiesPer100kTiles);
            Scribe_Values.Look(ref abandonedPer100kTiles, "abandonedPer100kTiles", Defaults.abandonedPer100kTiles);
            Scribe_Values.Look(ref compromisedPer100kTiles, "compromisedPer100kTiles", Defaults.compromisedPer100kTiles);
            Scribe_Values.Look(ref minCitadelsPerWorld, "minCitadelsPerWorld", Defaults.minCitadelsPerWorld);

            // Scribe_Values.Look(ref customMapSizes, "customMapSizes", Defaults.customMapSizes);
            Scribe_Values.Look(ref customMapX, "customMapX", Defaults.customMapX);
            Scribe_Values.Look(ref customMapZ, "customMapZ", Defaults.customMapZ);
        }
    }

    public class Mod_Cities : Mod {
        readonly ModSettings_Cities settings;

        Config_Cities Config => settings.config;

        public Mod_Cities(ModContentPack content) : base(content) {
            settings = GetSettings<ModSettings_Cities>();
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("LimitCitySize".Translate().Formatted(Config.citySizeScale, Config.citySizeScale), ref Config.customCitySize);
            Config.citySizeScale = (int)listing.Slider(Config.citySizeScale, 50, 500);
            listing.CheckboxLabeled("EnableCityQuests".Translate(), ref Config.enableQuestSystem);
            if (Config.enableQuestSystem) {
                listing.CheckboxLabeled("EnableDefendQuest".Translate(), ref Config.enableDefendQuest);
            }
            listing.Gap();
            listing.CheckboxLabeled("EnableCityEvents".Translate(), ref Config.enableEvents);
            listing.CheckboxLabeled("EnableCityTurrets".Translate(), ref Config.enableTurrets);
            listing.CheckboxLabeled("EnableCityMortars".Translate(), ref Config.enableMortars);
            listing.CheckboxLabeled("EnableCityLooting".Translate(), ref Config.enableLooting);
            listing.Gap();
            listing.Label("CityLootScale".Translate().Formatted(GenMath.RoundTo(Config.lootScale, 0.05F)));
            Config.lootScale = listing.Slider(Config.lootScale, 0, 3);
            // listing.Label("AbandonedCityChance".Translate().Formatted(GenMath.RoundTo(Config.abandonedChance, 0.01F)));
            // Config.abandonedChance = listing.Slider(Config.abandonedChance, 0, 1);
            listing.Label("CitiesPer100kTiles".Translate());
            listing.IntRange(ref Config.citiesPer100kTiles, 0, 100);
            listing.Label("AbandonedPer100kTiles".Translate());
            listing.IntRange(ref Config.abandonedPer100kTiles, 0, 100);
            listing.Label("CompromisedPer100kTiles".Translate());
            listing.IntRange(ref Config.compromisedPer100kTiles, 0, 100);
            listing.Label("MinCitadelsPerWorld".Translate().Formatted(Config.minCitadelsPerWorld));
            listing.IntAdjuster(ref Config.minCitadelsPerWorld, 1, 1);
            listing.Gap();
            {
                ///////////////
                // listing.Gap();
                // listing.CheckboxLabeled("Override all map sizes (disables upon exiting the game): [{0} x {1}]".Formatted(Config.customMapX, Config.customMapZ), ref Config.customMapSizes);
                // listing.Gap();
                // listing.Label("Custom map size X: [{0}]".Formatted(Config.customMapX));
                // listing.IntAdjuster(ref Config.customMapX, 5, 10);
                // listing.IntAdjuster(ref Config.customMapX, 100, 10);
                // listing.Label("Custom map size Z: [{0}]".Formatted(Config.customMapZ));
                // listing.IntAdjuster(ref Config.customMapZ, 5, 10);
                // listing.IntAdjuster(ref Config.customMapZ, 100, 10);
                // listing.Gap();
                // listing.Gap();
            }
            if (listing.ButtonText("CitiesConfigReset".Translate())) {
                settings.config = new Config_Cities();
            }
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() {
            return "RimCities";
        }
    }

    public class ModSettings_Cities : ModSettings {

        public Config_Cities config;

        public ModSettings_Cities() {
            Reset();
        }

        public override void ExposeData() {
            base.ExposeData();
            config.ExposeData();
        }

        public void Reset() {
            config = new Config_Cities();
        }
    }
}