namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// 风泵：不耗电、不烧燃料，靠风力把井水抽进水箱。
	/// 出力固定为 Def 里的 Capacity（2000 L/天），断电/缺燃料判断由基类的空引用检查自动跳过，
	/// 单独建类是为了让 XML 能区分风泵与电动泵，后续要接风速/扇叶动画也在这里扩展。
	/// </summary>
	public class CompWindPump : CompWaterPumpingStation
	{
	}
}
