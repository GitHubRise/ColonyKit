#nullable disable

﻿namespace rjw;

public enum Orientation
{
	None,
	Asexual,
	Pansexual,
	Heterosexual,
	MostlyHeterosexual,
	LeaningHeterosexual,
	Bisexual,
	LeaningHomosexual,
	MostlyHomosexual,
	Homosexual
}

public static class OrientationExtensions
{
	// 用于非标准性向的特殊金赛值
	public const int KINSEY_ASEXUAL = -1;
	public const int KINSEY_PANSEXUAL = -2;
	public const int KINSEY_NONE = -3;

	/// <summary>
	/// 将金赛评分转换为 Orientation。
	/// 标准量表：0（异性恋）到 6（同性恋）
	/// 特殊值：-1（无性恋），-2（泛性恋）
	/// </summary>
	public static Orientation FromKinseyRating(int kinseyRating)
	{
		switch (kinseyRating)
		{
			case 0:
				return Orientation.Heterosexual;
			case 1:
				return Orientation.MostlyHeterosexual;
			case 2:
				return Orientation.LeaningHeterosexual;
			case 3:
				return Orientation.Bisexual;
			case 4:
				return Orientation.LeaningHomosexual;
			case 5:
				return Orientation.MostlyHomosexual;
			case 6:
				return Orientation.Homosexual;
			case KINSEY_ASEXUAL:
				return Orientation.Asexual;
			case KINSEY_PANSEXUAL:
				return Orientation.Pansexual;
			default:
				return Orientation.None;
		}
	}

	/// <summary>
	/// 将 Orientation 转换为其对应的金赛评分。
	/// </summary>
	public static int ToKinseyRating(this Orientation orientation)
	{
		switch (orientation)
		{
			case Orientation.Heterosexual:
				return 0;
			case Orientation.MostlyHeterosexual:
				return 1;
			case Orientation.LeaningHeterosexual:
				return 2;
			case Orientation.Bisexual:
				return 3;
			case Orientation.LeaningHomosexual:
				return 4;
			case Orientation.MostlyHomosexual:
				return 5;
			case Orientation.Homosexual:
				return 6;
			case Orientation.Asexual:
				return KINSEY_ASEXUAL;
			case Orientation.Pansexual:
				return KINSEY_PANSEXUAL;
			case Orientation.None:
			default:
				return KINSEY_NONE;
		}
	}

	/// <summary>
	/// 获取映射到标准 0-6 量表的金赛评分。
	/// 泛性恋映射为 3（相当于双性恋）。
	/// 无性恋和 None 返回 null。
	/// </summary>
	public static int? ToStandardKinseyRating(this Orientation orientation)
	{
		switch (orientation)
		{
			case Orientation.Heterosexual:
				return 0;
			case Orientation.MostlyHeterosexual:
				return 1;
			case Orientation.LeaningHeterosexual:
				return 2;
			case Orientation.Bisexual:
			case Orientation.Pansexual:
				return 3;
			case Orientation.LeaningHomosexual:
				return 4;
			case Orientation.MostlyHomosexual:
				return 5;
			case Orientation.Homosexual:
				return 6;
			case Orientation.Asexual:
			case Orientation.None:
			default:
				return null;
		}
	}

	/// <summary>
	/// 检查该性向是否位于标准金赛量表（0-6）上。
	/// </summary>
	public static bool IsOnKinseyScale(this Orientation orientation)
	{
		return orientation != Orientation.None &&
			   orientation != Orientation.Asexual &&
			   orientation != Orientation.Pansexual;
	}
}
