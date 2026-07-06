using Content.Goobstation.Common.CCVar; // Goob
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Configuration; // Goob
using Robust.Shared.Prototypes;
using Robust.Shared.Timing; // Goob

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public static EntProtoId Drunk = "StatusEffectDrunk";

    [Dependency] protected readonly StatusEffectsSystem Status = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // Goob - needed to calculate remaining status time.
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Goob - used to get the CVar setting.

    public override void Initialize()
    {
        SubscribeLocalEvent<LightweightDrunkComponent, DrunkEvent>(OnLightweightDrinking);
    }

    public void TryApplyDrunkenness(EntityUid uid, TimeSpan boozePower)
    {
        var ev = new DrunkEvent(boozePower);
        RaiseLocalEvent(uid, ref ev);

        Status.TryAddStatusEffectDuration(uid, Drunk, ev.Duration);

        // Goob modification starts
        if (Status.TryGetTime(uid, Drunk, out var time) && time.EndEffectTime is { } endTime)
        {
            var maxDrunkTime = TimeSpan.FromSeconds(_cfg.GetCVar(GoobCVars.MaxDrunkTime));

            if (endTime - _timing.CurTime > maxDrunkTime)
                Status.TrySetTime(uid, Drunk, _timing.CurTime + maxDrunkTime);
        }
        // Goob modification ends
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        Status.TryRemoveStatusEffect(uid, Drunk);
    }

    public void TryRemoveDrunkennessTime(EntityUid uid, TimeSpan boozePower)
    {
        Status.TryAddTime(uid, Drunk, - boozePower);
    }

    private void OnLightweightDrinking(Entity<LightweightDrunkComponent> entity, ref DrunkEvent args)
    {
        args.Duration *= entity.Comp.BoozeStrengthMultiplier;
    }

    [ByRefEvent]
    public record struct DrunkEvent(TimeSpan Duration);
}
