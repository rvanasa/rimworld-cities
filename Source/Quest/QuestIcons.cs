using UnityEngine;
using Verse;

namespace Cities
{
	[StaticConstructorOnStartup]
	public static class QuestIcons {
		public static readonly Texture2D InfoIcon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");
		//public static readonly Texture2D AttackIcon = ContentFinder<Texture2D>.Get("UI/Commands/SquadAttack");
	}
}