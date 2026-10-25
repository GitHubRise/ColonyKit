#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using rjw.Modules.Shared.Extensions;
using rjw.Modules.Interactions.Preferences;

namespace rjw.Modules.Interactions
{
	public static class SexInteractionFinder
	{
		public static List<SexInteractionResolved> FindInteractions(SexProps props)
		{
			var cache = new Internal.FinderCache(props);

			var interactionType = Internal.FindInteractionType(props);

			var interactions = SexUtility.SexInteractions
				.Select(i => new SexInteraction(i))
				.Where(i => i.Extension.tags.Contains(interactionType.AsInteractionTag()));

			
			var available = new List<SexInteractionResolved>();
			foreach (var interaction in interactions)
			{
				foreach (var resolved in Internal.ResolveInteraction(interaction, props, cache))
				{
					available.Add(resolved);
				}
			}

			return available;
		}

		public static class Internal
		{
			public class FinderCache
			{
				public LewdParts initiatorParts;
				public LewdParts recipientParts;

				public FinderCache(SexProps props)
				{
					initiatorParts = new LewdParts(props.initiator, populateAll: true);
					recipientParts = new LewdParts(props.recipient, populateAll: true);
				}
			}

			static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
			{
				if (length == 1) return list.Select(t => new T[] { t });
				return GetPermutations(list, length - 1).SelectMany(t => list.Where(o => !t.Contains(o)),	(t1, t2) => t1.Concat(new T[] { t2 }));
			}

			public static IEnumerable<SexInteractionResolved> ResolveInteraction(SexInteraction interaction, SexProps props, FinderCache cache)
			{
				if (!TrySatisfyRequirement(props.initiator, cache.initiatorParts, interaction.Extension.initiatorRequirement, props, out var initiatorValidParts))
				{
					yield break;
				}
				if (!TrySatisfyRequirement(props.recipient, cache.recipientParts, interaction.Extension.recipientRequirement, props, out var recipientValidParts))
				{
					yield break;
				}

				// 对于非特定互动，可能有多种解析方式。
				// 例如：在互相自慰、六九、双重插入、兽交口交中，可能选中肛门或阴道

				foreach(var initiatorPermutation in GetPermutations(initiatorValidParts, interaction.Extension.initiatorRequirement.minimumCount ?? 1))
				{
					foreach(var recipientPermutation in GetPermutations(recipientValidParts, interaction.Extension.recipientRequirement.minimumCount ?? 1))
					{
						var resolved = new SexInteractionResolved
						{
							Initiator = props.initiator,
							Recipient = props.recipient,
							Interaction = interaction,
							InitiatorParts = initiatorPermutation.ToList(),
							RecipientParts = recipientPermutation.ToList(),
							Penetrations = FindPenetrations(interaction, initiatorPermutation.ToList(), recipientPermutation.ToList(), cache)
						};

						yield return resolved;
					}
				}
			}

			public static bool TrySatisfyRequirement(Pawn pawn, LewdParts parts, SexInteractionRequirement requirement, SexProps props, out List<ILewdablePart> validParts)
			{
				// 这里的 SexProps props 用于子模组打补丁，供依赖双方小人等的条件使用。不要移除。

				validParts = null;

				// 如果指定了 pawnStates，则小人必须至少匹配一个状态
				if (!requirement.pawnStates.NullOrEmpty())
				{
					if (!requirement.pawnStates.Any(s => pawn.GetPawnState() == s))
					{
						return false;
					}
				}

				// 现在处理部位，根据条件筛选可用部位

				// 首先过滤掉不可用的部位（缺失或被阻挡）
				validParts = parts.AllParts.Where(p => LewdPartHelper.IsAvailable(pawn, p)).ToList();

				// 如果指定了 genitalFamilies，则每个部位必须至少匹配一个
				if(!requirement.genitalFamilies.NullOrEmpty())
				{
					validParts = validParts.Where(p => requirement.genitalFamilies.Any(f => p.Family == f.AsLewdPartFamily())).ToList();
				}

				// 如果指定了 partFamilies，则每个部位必须至少匹配一个
				if(!requirement.partFamilies.NullOrEmpty())
				{
					validParts = validParts.Where(p => requirement.partFamilies.Any(f => p.Family == f.AsLewdPartFamily())).ToList();
				}

				// 如果指定了 genitalTags，则每个部位必须至少匹配一个
				if(!requirement.genitalTags.NullOrEmpty())
				{
					validParts = validParts.Where(p => requirement.genitalTags.Any(t => p.HasGenitalTag(t))).ToList();
				}

				// 如果指定了 partTags，则每个部位必须至少匹配一个
				if(!requirement.partTags.NullOrEmpty())
				{
					validParts = validParts.Where(p => p is RJWLewdablePart part && requirement.partTags.Any(t => part.SexPart.Def.partTags.Contains(t))).ToList();
				}

				// 如果指定了 minimumSeverity，则每个部位必须至少达到该严重度
				if(requirement.minimumSeverity.HasValue)
				{
					validParts = validParts.Where(p => p is RJWLewdablePart part && part.SexPart.AsHediff.Severity >= requirement.minimumSeverity).ToList();
				}

				// 如果指定了 minimumCount，则我们至少要有这么多部位
				if(requirement.minimumCount.HasValue)
				{
					if(validParts.Count < requirement.minimumCount)
					{
						return false;
					}
				}

				if(validParts.Count == 0)
				{
					return false;
				}

				// 如果手/脚数量超出所需，则移除多余的
				var dedupeParts = new List<ILewdablePart>();
				foreach(var part in validParts)
				{
					if (part is VanillaLewdablePart)
					{
						if(dedupeParts.Count(p => p.Family == part.Family) < (requirement.minimumCount ?? 1))
						{
							dedupeParts.Add(part);
						}
					}
					else
					{
						dedupeParts.Add(part);
					}
				}
				validParts = dedupeParts;

				return true;
			}
			public static List<Tuple<ILewdablePart, ILewdablePart>> FindPenetrations(SexInteraction interaction, List<ILewdablePart> initiatorParts, List<ILewdablePart> recipientParts, FinderCache cache)
			{
				var found = new List<Tuple<ILewdablePart, ILewdablePart>>();

				// 处理非插入式互动，适用于看似插入式但实际并非如此的情况
				if (interaction.Extension.tags.Contains(SexInteractionTag.NonPenetrative))
				{
					return found;
				}

				// 处理六九，它无法指定嘴部，因此需要手动处理。
				if (interaction.Def.defName == "Sex_Sixtynine")
				{
					var initiatorMouth = cache.initiatorParts.Mouths
						.Where(p => LewdPartHelper.IsAvailable(cache.initiatorParts.Owner, p)).ToList();

					var recipientMouth = cache.recipientParts.Mouths
						.Where(p => LewdPartHelper.IsAvailable(cache.initiatorParts.Owner, p)).ToList();

					var initiatorPart = initiatorParts.First();
					var recipientPart = recipientParts.First();

					if(initiatorMouth.Count > 0 && recipientPart.CouldBePenetrator())
					{
						found.Add(new(recipientPart, initiatorMouth.First()));
					}

					if(recipientMouth.Count > 0 && initiatorPart.CouldBePenetrator())
					{
						found.Add(new(initiatorPart, recipientMouth.First()));
					}
					return found;
				}

				// 带有 Multiple 标签的标签按两个部位计算
				foreach(var p in initiatorParts.Where(p => p.HasPartTag("Multiple")).ToList())
				{
					initiatorParts.Add(p);
				}
				foreach(var p in recipientParts.Where(p => p.HasPartTag("Multiple")).ToList())
				{
					recipientParts.Add(p);
				}

				// 尝试把插入方映射到腔口。多个插入方可以映射到同一个腔口。
				if(initiatorParts.Count > 0 && recipientParts.Count > 0)
				{
					var initiatorPenetrators = initiatorParts.Where(p => p.CouldBePenetrator()).ToList();
					var initiatorOrifices = initiatorParts.Where(p => p.CouldBeOrifice()).ToList();
					
					var recipientPenetrators = recipientParts.Where(p => p.CouldBePenetrator()).ToList();
					var recipientOrifices = recipientParts.Where(p => p.CouldBeOrifice()).ToList();

					for(var i = 0; i < initiatorPenetrators.Count; i++)
					{
						if(recipientOrifices.Count == 0)
							break;
						found.Add(new(initiatorPenetrators[i], recipientOrifices[i%recipientOrifices.Count]));
					}

					for(var i = 0; i < recipientPenetrators.Count; i++)
					{
						if(initiatorOrifices.Count == 0)
							break;
						found.Add(new(recipientPenetrators[i], initiatorOrifices[i%initiatorOrifices.Count]));
					}
				}

				return found;
			}

			public static SexInteractionType FindInteractionType(SexProps props)
			{
				if (props.initiator == props.recipient || props.recipient == null)
				{
					return SexInteractionType.Masturbation;
				}

				if (props.recipient.health.Dead == true)
				{
					return SexInteractionType.Necrophilia;
				}

				if (xxx.is_mechanoid(props.initiator))
				{
					return SexInteractionType.Mechanoid;
				}

				// 要么这个，要么那个，但不能两者兼有
				if (xxx.is_animal(props.initiator) ^ xxx.is_animal(props.recipient))
				{
					return SexInteractionType.Bestiality;
				}

				if (xxx.is_animal(props.initiator) && xxx.is_animal(props.recipient))
				{
					return SexInteractionType.Animal;
				}

				if (props.isWhoring)
				{
					return SexInteractionType.Whoring;
				}

				if (props.isRape)
				{
					return SexInteractionType.Rape;
				}

				return SexInteractionType.Consensual;
			}
		}
	}
}
