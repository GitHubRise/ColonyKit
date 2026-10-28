using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// 温控器（DBH CompThermostat 的移植）：贴在墙上、接在热水管网上，
	/// 房间比目标温度低就向锅炉「要热」，高了就停。只有 ThermostatControl=true 的锅炉听它的。
	/// </summary>
	public class CompThermostat : CompTempControl
	{
		public bool RequestHeating = true;

		public override void CompTickRare()
		{
			base.CompTickRare();
			RequestHeating = parent.AmbientTemperature < targetTemperature;
		}

		public override string CompInspectStringExtra()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.CompInspectStringExtra();
			}
			string text = "CK.Hygiene.ThermostatTarget".Translate() + ": " + targetTemperature.ToStringTemperature("F0");
			text += "\n" + (RequestHeating
				? "CK.Hygiene.ThermostatCalling".Translate()
				: "CK.Hygiene.ThermostatIdle".Translate());
			return text;
		}
	}
}
