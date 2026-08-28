using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Runtime counterpart of hk4e ActorModifier. ModifierId is the 1-based index
/// into AbilityComp::applied_modifier_vec_; null vector slots are meaningful.
/// </summary>
public sealed class ActorModifier
{
	public ActorAbility ParentAbility { get; }
	public Entity Owner { get; }
	public AbilityModifier Config { get; }

	public uint ModifierId { get; internal set; }
	public int ParentOwnerIndex { get; internal set; } = -1;

	public ulong StartTimeMs { get; private set; }
	public ulong ExistDurationMs { get; private set; }

	public uint AttachedModifierOwnerEntityId { get; set; }
	public uint AttachedModifierId { get; set; }
	public bool IsMuteRemote { get; set; }
	public uint ApplyEntityId { get; set; }
	public bool IsAttachedParentAbility { get; set; }

	public ActorModifier? AttachedTo { get; private set; }
	public List<ActorModifier> AttachedModifiers { get; } = new();

	public ActorModifier(ActorAbility parentAbility, Entity owner, AbilityModifier config)
	{
		ParentAbility = parentAbility ?? throw new ArgumentNullException(nameof(parentAbility));
		Owner = owner ?? throw new ArgumentNullException(nameof(owner));
		Config = config ?? throw new ArgumentNullException(nameof(config));
		StartTimeMs = NowMs();
	}

	public void OnLogin(ulong nowMs)
	{
		StartTimeMs = nowMs >= ExistDurationMs ? nowMs - ExistDurationMs : 0;
	}

	public void OnDisconnect()
	{
		ulong nowMs = NowMs();
		if (StartTimeMs < nowMs)
			ExistDurationMs = nowMs - StartTimeMs;
	}

	internal void AttachToModifier(ActorModifier? parent)
	{
		if (ReferenceEquals(parent, this))
			return;

		if (AttachedTo != null)
			AttachedTo.AttachedModifiers.Remove(this);

		AttachedTo = parent;
		if (parent != null && !parent.AttachedModifiers.Contains(this))
			parent.AttachedModifiers.Add(this);
	}

	internal void DetachRuntimeLinks()
	{
		AttachToModifier(null);
		foreach (ActorModifier child in AttachedModifiers.ToArray())
			child.AttachToModifier(null);
		AttachedModifiers.Clear();
	}

	public AbilityAppliedModifier ToProtocol()
	{
		var result = new AbilityAppliedModifier
		{
			ModifierLocalId = Config.configLocalID,
			ParentAbilityName = new AbilityString { Hash = ParentAbility.AbilityNameHash },
			InstancedAbilityId = ParentAbility.AbilityId,
			InstancedModifierId = ModifierId,
			ApplyEntityId = ApplyEntityId,
			IsAttachedParentAbility = IsAttachedParentAbility
		};

		// ActorModifier::toClient only writes parent_ability_entity_id when the
		// parent ActorAbility caster is a different creature from this modifier's
		// owner. Cross-entity AbilityMetaModifierChange(target_id != 0) relies on it.
		if (!ReferenceEquals(ParentAbility.Caster, Owner))
			result.ParentAbilityEntityId = ParentAbility.Caster._EntityId;

		if (!string.IsNullOrEmpty(ParentAbility.OverrideName) &&
			!string.Equals(ParentAbility.OverrideName, "Default", StringComparison.Ordinal))
		{
			result.ParentAbilityOverride = new AbilityString { Hash = ParentAbility.OverrideNameHash };
		}

		ulong nowMs = NowMs();
		if (StartTimeMs < nowMs)
			result.ExistDuration = (nowMs - StartTimeMs) / 1000.0f;

		if (AttachedModifierId != 0)
		{
			bool isInvalid = false;
			if (AttachedModifierOwnerEntityId != 0)
			{
				var manager = Owner.session.player.Scene.EntityManager;
				isInvalid = !manager.TryGet(AttachedModifierOwnerEntityId, out Entity entity) || entity == null;
			}

			result.AttachedInstancedModifier = new AbilityAttachedModifier
			{
				IsInvalid = isInvalid,
				OwnerEntityId = AttachedModifierOwnerEntityId,
				InstancedModifierId = AttachedModifierId
			};
		}

		return result;
	}

	private static ulong NowMs() => unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
