#nullable enable

using Verse;
using RimWorld;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using rjw.Modules.Shared.Extensions;

namespace rjw
{
	/// <summary>
	/// 与性相关事物/结果的数据
	/// </summary>
	public class SexProps : IExposable
	{
		public Pawn pawn;
		public Pawn partner;
		public bool hasPartner() => partner != null && partner != pawn;
		public InteractionDef? dictionaryKey = null;
		public bool usedCondom = false;
		public bool isRape = false;
		public bool isReceiver = false;// 参照 JobDriver_SexBaseReciever
		public bool isRevese = false;// 谁操谁这套乱七八糟的，标签之类的东西看 impregnation 会更清楚。     interaction.HasInteractionTag(InteractionTag.Reverse)
		public bool isRapist = false;
		public bool isCoreLovin = false;//原版做爱，跳过一些机制。  真该找时间清理一下这段
		public bool isWhoring = false;
		public bool isMechImplant = false;
		public bool canBeGuilty = true;// 发起方小人是否会因感知而被计为有罪；玩家发起/右键操作 = false

		public int orgasms = 0; // 该小人经历的高潮

		[System.Obsolete("This constructor is only for the Scribe. Use SexProps(pawn, partner) instead")]
		public SexProps()
		{
			pawn = null!;
			partner = null!;
		}

		public SexProps(Pawn pawn, Pawn partner)
		{
			this.pawn = pawn;
			this.partner = partner;
		}

		public Pawn initiator => isReceiver ? partner : pawn;
		public Pawn recipient => isReceiver ? pawn : partner;

		// TODO: 替换 dictionaryKey（破坏性变更）
		private InteractionDef? interactionDef
		{
			get { return dictionaryKey; }
			set { dictionaryKey = value; }
		}
		private SexInteraction? _interaction;
		public SexInteraction? interaction {
			get
			{
				if (_interaction == null)
					_interaction = new SexInteraction(interactionDef);
				return _interaction;
			}
			set
			{
				_interaction = value;
				if (value != null)
				{
					interactionDef = value.Def;
				}
			}
		}

		public SexInteractionResolved? _resolved;
		public SexInteractionResolved? resolved {
			get
			{
				if (_resolved == null)
				{
					if (this.interactionDef == null || this.pawn == null || this.partner == null)
						ModLog.Error($"SexProps could not resolve interaction! Missing data. {this.interactionDef}, {this.pawn}, {this.partner}");
					else
					{
						ModLog.Warning("SexProps forced to try resolve interaction. This should only happen on save load.");
						_resolved = SexInteractionHelper.ResolveInteraction(this);
						if(_resolved == null)
						{
							ModLog.Warning($"SexProps failed to resolve {this.interaction?.Def} for {this.initiator.GetName()} -> {this.recipient.GetName()}, using empty interaction.");
							new SexInteractionResolved
							{
								Initiator = this.initiator,
								Recipient = this.recipient,
								Interaction = this.interaction,
								InitiatorParts = new(),
								RecipientParts = new(),
								Penetrations = new()
							};
						}
					}
				}
				return _resolved;
			}
			set
			{
				_resolved = value;
				if (value != null)
				{
					_interaction = value.Interaction;
					interactionDef = value.Interaction.Def;
				}
			}
		}

		private xxx.rjwSextype? _sexType;
		public xxx.rjwSextype sexType { get
			{
				if (_sexType == null)
					_sexType = interaction?.Sextype;
				return _sexType ?? xxx.rjwSextype.None;
			}
		}
		
		public bool IsInitiator() => !isReceiver;

		public bool IsSubmissive() => (isReceiver && !isRevese) || (!isReceiver && isRevese);

		public SexProps GetForPartner()
		{
			return new SexProps(partner, pawn)
			{
				usedCondom = usedCondom,
				isRape = isRape,
				isReceiver = !isReceiver,
				isRevese = isRevese,
				isRapist = isRapist,
				isMechImplant = isMechImplant,
				isCoreLovin = isCoreLovin,
				isWhoring = isWhoring,
				canBeGuilty = canBeGuilty,
				orgasms = orgasms,
				interaction = interaction,
				resolved = resolved
			};
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref pawn, "pawn");
			Scribe_References.Look(ref partner, "partner");
			Scribe_Defs.Look(ref dictionaryKey, "dictionaryKey");
			Scribe_Values.Look(ref usedCondom, "usedCondom");
			Scribe_Values.Look(ref isRape, "isRape");
			Scribe_Values.Look(ref isReceiver, "isReceiver");
			Scribe_Values.Look(ref isRapist, "isRapist");
			Scribe_Values.Look(ref isMechImplant, "isMechImplant");
			Scribe_Values.Look(ref isCoreLovin, "isCoreLovin");
			Scribe_Values.Look(ref isWhoring, "isWhoring");
			Scribe_Values.Look(ref canBeGuilty, "canBeGuilty");
			Scribe_Values.Look(ref orgasms, "orgasms");
		}
	}
}
