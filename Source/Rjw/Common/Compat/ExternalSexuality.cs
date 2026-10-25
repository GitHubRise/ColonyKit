using System;
using System.Reflection;
using Verse;

namespace rjw;

/// <summary>
/// 对其他模组所属性取向数据的软依赖桥接。
/// </summary>
/// <remarks>
/// <para>
/// RJW 只需要从可选模组获取两条信息：Psychology 的
/// <c>CompPsychology</c> 写入的金赛评分，以及 [SYR] Individuality 的
/// <c>CompIndividuality</c> 添加的性取向。
/// </para>
/// <para>
/// 在编译期引用这些程序集会让每次安装都携带它们
/// （并把 RJW 固定在其编译时所针对的非常古老的 RimWorld 版本上），
/// 因此改为通过反射读取数据。当另一个模组不存在时，
/// 查找就会直接失败，调用方则回退到自己的数值。
/// </para>
/// </remarks>
internal static class ExternalSexuality
{
	private const string PsychologyCompTypeName = "Psychology.CompPsychology";
	private const string IndividualityCompTypeName = "SyrTraits.CompIndividuality";

	private const BindingFlags MemberFlags =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	private static bool psychologyInitialized;
	private static Type? psychologyCompType;
	private static MemberInfo? psychologySexualityMember;
	private static MemberInfo? psychologyKinseyMember;

	private static bool individualityInitialized;
	private static Type? individualityCompType;
	private static MemberInfo? individualitySexualityMember;

	/// <summary>
	/// 提供 <c>CompPsychology</c> 的模组当前是否已加载。
	/// </summary>
	public static bool PsychologyAvailable
	{
		get
		{
			EnsurePsychologyResolved();
			return psychologyCompType != null;
		}
	}

	/// <summary>
	/// 提供 <c>CompIndividuality</c> 的模组当前是否已加载。
	/// </summary>
	public static bool IndividualityAvailable
	{
		get
		{
			EnsureIndividualityResolved();
			return individualityCompType != null;
		}
	}

	/// <summary>
	/// 读取 Psychology 为 <paramref name="pawn"/> 分配的金赛评分。
	/// </summary>
	/// <param name="pawn">要检查的小人。</param>
	/// <param name="rating">金赛评分，取值 0 到 6。</param>
	/// <returns>是否成功读取到评分。</returns>
	public static bool TryGetKinseyRating(Pawn pawn, out int rating)
	{
		rating = 0;

		EnsurePsychologyResolved();

		if (psychologyKinseyMember is null)
			return false;

		object? comp = FindComp(pawn, psychologyCompType);
		if (comp is null)
			return false;

		object? sexuality = GetMemberValue(comp, psychologySexualityMember);
		if (sexuality is null)
			return false;

		object? value = GetMemberValue(sexuality, psychologyKinseyMember);
		if (value is int kinsey)
		{
			rating = kinsey;
			return true;
		}

		return false;
	}

	/// <summary>
	/// 读取 [SYR] Individuality 为 <paramref name="pawn"/> 分配的性取向。
	/// </summary>
	/// <param name="pawn">要检查的小人。</param>
	/// <param name="sexuality">枚举名称，例如 <c>Straight</c> 或 <c>Gay</c>。</param>
	/// <returns>是否成功读取到性取向。</returns>
	public static bool TryGetIndividualitySexuality(Pawn pawn, out string sexuality)
	{
		sexuality = string.Empty;

		EnsureIndividualityResolved();

		if (individualitySexualityMember is null)
			return false;

		object? comp = FindComp(pawn, individualityCompType);
		if (comp is null)
			return false;

		object? value = GetMemberValue(comp, individualitySexualityMember);
		if (value is null)
			return false;

		sexuality = value.ToString();
		return true;
	}

	/// <summary>
	/// 将金赛评分转换为对应的 RJW 性向。
	/// </summary>
	/// <param name="kinsey">金赛评分，取值 0 到 6。</param>
	/// <returns>对应的性向；未知值则返回 <see cref="Orientation.Asexual"/>。</returns>
	public static Orientation MapKinsey(int kinsey)
	{
		switch (kinsey)
		{
			case 0: return Orientation.Heterosexual;
			case 1: return Orientation.MostlyHeterosexual;
			case 2: return Orientation.LeaningHeterosexual;
			case 3: return Orientation.Bisexual;
			case 4: return Orientation.LeaningHomosexual;
			case 5: return Orientation.MostlyHomosexual;
			case 6: return Orientation.Homosexual;
			default: return Orientation.Asexual;
		}
	}

	/// <summary>
	/// 将 [SYR] Individuality 的性取向名称转换为对应的 RJW 性向。
	/// </summary>
	/// <param name="sexuality">枚举名称，例如 <c>Straight</c> 或 <c>Gay</c>。</param>
	/// <returns>对应的性向；未知值则返回 <see cref="Orientation.Asexual"/>。</returns>
	public static Orientation MapIndividuality(string sexuality)
	{
		switch (sexuality)
		{
			case "Straight": return Orientation.Heterosexual;
			case "Bisexual": return Orientation.Bisexual;
			case "Gay": return Orientation.Homosexual;
			default: return Orientation.Asexual;
		}
	}

	private static void EnsurePsychologyResolved()
	{
		if (psychologyInitialized)
			return;

		psychologyInitialized = true;
		psychologyCompType = ResolveType(PsychologyCompTypeName);
		psychologySexualityMember = FindMember(psychologyCompType, "Sexuality");
		psychologyKinseyMember = FindMember(GetMemberType(psychologySexualityMember), "kinseyRating");
	}

	private static void EnsureIndividualityResolved()
	{
		if (individualityInitialized)
			return;

		individualityInitialized = true;
		individualityCompType = ResolveType(IndividualityCompTypeName);
		individualitySexualityMember = FindMember(individualityCompType, "sexuality");
	}

	private static Type? ResolveType(string typeName)
	{
		try
		{
			// 当没有已加载的程序集声明该类型时返回 null，这一情形
			// 在未安装可选模组时是正常情况。
			return GenTypes.GetTypeInAnyAssembly(typeName);
		}
		catch (Exception exception)
		{
			ModLog.Warning($"ExternalSexuality could not resolve {typeName}: {exception.Message}");
			return null;
		}
	}

	private static MemberInfo? FindMember(Type? type, string name)
	{
		if (type is null)
			return null;

		return (MemberInfo?)type.GetProperty(name, MemberFlags) ?? type.GetField(name, MemberFlags);
	}

	private static Type? GetMemberType(MemberInfo? member)
	{
		return member switch
		{
			PropertyInfo property => property.PropertyType,
			FieldInfo field => field.FieldType,
			_ => null
		};
	}

	private static object? GetMemberValue(object instance, MemberInfo? member)
	{
		return member switch
		{
			PropertyInfo property => property.GetValue(instance),
			FieldInfo field => field.GetValue(instance),
			_ => null
		};
	}

	private static object? FindComp(Pawn pawn, Type? compType)
	{
		if (compType is null)
			return null;

		foreach (ThingComp comp in pawn.AllComps)
		{
			if (compType.IsInstanceOfType(comp))
				return comp;
		}

		return null;
	}
}
