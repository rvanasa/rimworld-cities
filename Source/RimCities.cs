using System.Reflection;
using HarmonyLib;
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

        public bool limitCitySize = true;
        public bool enableQuestSystem = true;
        public bool enableEvents = true;
        public float abandonedChance = 0.3F;
        public IntRange citiesPer100kTiles = new IntRange(10, 15);
        public IntRange abandonedPer100kTiles = new IntRange(5, 10);
        public int minCitadelsPerWorld = 1;

        public void ExposeData() {
            Scribe_Values.Look(ref limitCitySize, "limitCitySize", Defaults.limitCitySize);
            Scribe_Values.Look(ref enableQuestSystem, "enableQuestSystem", Defaults.enableQuestSystem);
            Scribe_Values.Look(ref enableEvents, "enableEvents", Defaults.enableEvents);
            Scribe_Values.Look(ref citiesPer100kTiles, "citiesPer100kTiles", Defaults.citiesPer100kTiles);
            Scribe_Values.Look(ref abandonedPer100kTiles, "abandonedPer100kTiles", Defaults.abandonedPer100kTiles);
            Scribe_Values.Look(ref minCitadelsPerWorld, "minCitadelsPerWorld", Defaults.minCitadelsPerWorld);
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
            listing.Gap();
            listing.CheckboxLabeled("LimitCitySize".Translate(), ref Config.limitCitySize);
            listing.Gap();
            listing.CheckboxLabeled("EnableCityQuests".Translate(), ref Config.enableQuestSystem);
            listing.Gap();
            listing.CheckboxLabeled("EnableCityEvents".Translate(), ref Config.enableEvents);
            listing.Gap();
            listing.Label("AbandonedCityChance".Translate().Formatted(GenMath.RoundTo(Config.abandonedChance, 0.01F)));
            Config.abandonedChance = listing.Slider(Config.abandonedChance, 0, 1);
            listing.Gap();
            listing.Label("CitiesPer100kTiles".Translate());
            listing.IntRange(ref Config.citiesPer100kTiles, 0, 100);
            listing.Gap();
            listing.Label("MinCitadelsPerWorld".Translate().Formatted(Config.minCitadelsPerWorld));
            listing.IntAdjuster(ref Config.minCitadelsPerWorld, 1, 1);
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