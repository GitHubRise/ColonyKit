#nullable disable

﻿using Verse;

namespace rjw
{
	public class RaceTagDef : Def
	{
		/// <summary>
		/// 仅用于向后兼容。不应在此添加更多特殊情况。
		/// </summary>
		public bool DefaultWhenNoRaceGroupDef(Pawn pawn)
		{
			return defName switch
			{
				"Demon" => xxx.is_demon(pawn),
				"Slime" => xxx.is_slime(pawn),
				"Skin" => true,
				_ => false,
			};
		}
	}
}
