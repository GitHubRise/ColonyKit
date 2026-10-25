#nullable disable

﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Multiplayer.API;

namespace rjw
{
	public class Recipe_ForceOffGear : Recipe_Surgery
	{
		public static bool is_wearing(Pawn p, ThingDef apparel_def)
		{
			if (p.apparel != null)
				foreach (var app in p.apparel.WornApparel)
					if (app.def == apparel_def)
						return true;
			return false;
		}

		public static BodyPartRecord find_part_record(BodyPartDef part_def, Pawn p)
		{
			return p.RaceProps.body.AllParts.Find((BodyPartRecord bpr) => bpr.def == part_def);
		}

		// 仅在 "p" 穿着相关服装时才把配方放入手术列表。这里的小技巧是产出
		// null 会让游戏把配方放进列表，但不会真正应用到身体部位。
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn p, RecipeDef generic_def)
		{
			var r = (force_off_gear_def)generic_def;
			if (is_wearing(p, r.removes_apparel))
				yield return null;
		}

		[SyncMethod]
		public static void apply_burns(Pawn p, List<BodyPartDef> parts, float min_severity, float max_severity)
		{
			foreach (var part in parts)
			{
				var rec = find_part_record(part, p);
				if (rec != null)
				{
					//Rand.PopState();
					//Rand.PushState(RJW_Multiplayer.PredictableSeed());
					var to_deal = Rand.Range(min_severity, max_severity) * part.GetMaxHealth(p);
					var dealt = 0.0f;
					var counter = 0;
					while ((counter < 100) && (dealt < to_deal) && (!p.health.hediffSet.PartIsMissing(rec)))
					{
						var dam = Rand.RangeInclusive(3, 5);
						p.TakeDamage(new DamageInfo(DamageDefOf.Burn, dam, 999, -1.0f, null, rec, null));
						++counter;
						dealt += (float)dam;
					}
				}
			}
		}

		[SyncMethod]
		public override void ApplyOnPawn(Pawn p, BodyPartRecord null_part, Pawn surgeon, List<Thing> ingredients,Bill bill)
		{
			var r = (force_off_gear_def)recipe;
			if ((surgeon != null) &&
				(p.apparel != null) &&
				(!CheckSurgeryFail(surgeon, p, ingredients, find_part_record(r.failure_affects, p),bill)))
			{
				// 移除服装
				foreach (var app in p.apparel.WornApparel)
					if (app.def == r.removes_apparel)
					{
						p.apparel.Remove(app);
						break;
					}

				// 摧毁部位
				//Rand.PopState();
				//Rand.PushState(RJW_Multiplayer.PredictableSeed());
				var def_to_destroy = r.destroys_one_of.RandomElement<BodyPartDef>();
				if (def_to_destroy != null)
				{
					var record_to_destroy = find_part_record(def_to_destroy, p);
					if (record_to_destroy != null)
					{
						var dam = (int)(1.5f * def_to_destroy.GetMaxHealth(p));
						p.TakeDamage(new DamageInfo(DamageDefOf.Burn, dam, 999, -1.0f, null, record_to_destroy, null));
					}
				}

				if (r.major_burns_on != null)
					apply_burns(p, r.major_burns_on, 0.30f, 0.60f);
				if (r.minor_burns_on != null)
					apply_burns(p, r.minor_burns_on, 0.15f, 0.35f);
			}
		}
	}
}
