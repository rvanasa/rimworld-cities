using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Cities {

    // [HarmonyPatch(typeof(WorldObject))]
    // [HarmonyPatch(nameof(WorldObject.GetGizmos))]
    // internal static class WorldObject_GetGizmos {
    //     static void Postfix(ref WorldObject __instance, ref IEnumerable<Gizmo> __result) {
    //         foreach (var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
    //             __result = __result.Concat(quest.GetGizmos(__instance));
    //             if (quest.Targets.targets.Contains(__instance)) {
    //                 if (quest.def.Worker is IncidentWorker_Quest worker) {
    //                     var gizmo = new Command_Action {
    //                         icon = QuestIcons.InfoIcon,
    //                         defaultLabel = quest.Name,
    //                         defaultDesc = worker.LetterText,
    //                         action = () => worker.MakeLetter().OpenLetter(),
    //                     };
    //                     __result = __result.AddItem(gizmo);
    //                 }
    //             }
    //         }
    //     }
    // }
    //
    // [HarmonyPatch(typeof(Thing))]
    // [HarmonyPatch(nameof(Thing.GetGizmos))]
    // internal static class Thing_GetGizmos {
    //     static void Postfix(ref Thing __instance, ref IEnumerable<Gizmo> __result) {
    //         foreach (var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
    //             __result = __result.Concat(quest.GetGizmos(__instance));
    //         }
    //     }
    // }
    //
    // [HarmonyPatch(typeof(Thing))]
    // [HarmonyPatch(nameof(Thing.GetFloatMenuOptions))]
    // internal static class Thing_GetFloatMenuOptions {
    //     static void Postfix(ref Thing __instance, ref IEnumerable<FloatMenuOption> __result, Pawn selPawn) {
    //         foreach (var quest in Find.World.GetComponent<WorldComponent_QuestTracker>().quests) {
    //             __result = __result.Concat(quest.GetFloatMenuOptions(__instance, selPawn));
    //         }
    //     }
    // }
}