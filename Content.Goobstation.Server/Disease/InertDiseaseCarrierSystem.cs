using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Goobstation.Shared.Disease.Systems;
using Content.Server.Chemistry.TileReactions;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Events;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Disease;

/// <summary>
/// Carries diseases between people and the objects they touch, fading them once off a host.
/// </summary>
public sealed partial class InertDiseaseCarrierSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDiseaseSystem _disease = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DiseaseSpreadPrototype> TouchRoute = "Contact";
    private static readonly ProtoId<DiseaseSpreadPrototype> NeedleRoute = "Blood";

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private EntityQuery<DiseaseComponent> _diseaseQuery;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        _diseaseQuery = GetEntityQuery<DiseaseComponent>();

        SubscribeLocalEvent<InertDiseaseCarrierComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, InjectorDoAfterEvent>(OnInjectorUsed);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, DiseaseContaminateAttemptEvent>(OnContaminateAttempt);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, DiseaseContaminationCleanAttemptEvent>(OnCleanAttempt);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, DiseaseContaminationCleanedEvent>(OnClean);
        SubscribeLocalEvent<InertDiseaseCarrierComponent, ReactionEntityEvent>(OnReaction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate += UpdateInterval;

        var seconds = (float) UpdateInterval.TotalSeconds;
        var query = EntityQueryEnumerator<InertDiseaseCarrierComponent>();
        while (query.MoveNext(out var uid, out var carrier))
        {
            // Decay of diseases
            foreach (var held in carrier.Diseases?.ContainedEntities.ToArray() ?? [])
            {
                if (!_diseaseQuery.TryComp(held, out var disease))
                    continue;

                var resistance = MathF.Max(disease.EnvironmentalResistance, 0.01f);
                _disease.ChangeInfectionProgress((held, disease), -seconds * carrier.DecayRate / resistance);

                if (disease.InfectionProgress > 0f)
                    continue;

                // Remove immediately so neither transmission nor a new deposit can reuse a dead disease.
                _container.Remove(held, carrier.Diseases!, reparent: false, force: true);
                QueueDel(held);
            }

            // Shedding of the routes this object cannot hand over by interaction
            if (carrier.Diseases is { Count: > 0 } && TryPickDisease((uid, carrier), out var shed))
            {
                MapCoordinates? coords = null;
                foreach (var spread in carrier.SpreadParams)
                {
                    if (spread.Type == TouchRoute || spread.Type == NeedleRoute)
                        continue;

                    coords ??= _transform.GetMapCoordinates(uid);
                    foreach (var target in _lookup.GetEntitiesInRange<DiseaseCarrierComponent>(coords.Value, carrier.EmitRange))
                    {
                        _disease.DoInfectionAttempt(target.Owner,
                            shed.Value.Owner,
                            spread.Power,
                            spread.Chance * shed.Value.Comp.InfectionProgress * seconds,
                            spread.Type);
                    }
                }
            }

            // Expiry of the per person contact cooldowns
            foreach (var (other, next) in carrier.NextContact.ToArray())
            {
                if (next <= _timing.CurTime)
                    carrier.NextContact.Remove(other);
            }
        }
    }

    private void OnStartup(Entity<InertDiseaseCarrierComponent> ent, ref ComponentStartup args)
    {
        if (_container.TryGetContainer(ent.Owner, InertDiseaseCarrierComponent.DiseaseContainerId, out var container))
            ent.Comp.Diseases = container as Container;
    }

    private void OnShutdown(Entity<InertDiseaseCarrierComponent> ent, ref ComponentShutdown args)
    {
        ClearDiseases(ent);
    }

    private void OnActivateInWorld(Entity<InertDiseaseCarrierComponent> ent, ref ActivateInWorldEvent args)
    {
        Exchange(ent, args.User, TouchRoute);
    }

    private void OnStartCollide(Entity<InertDiseaseCarrierComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.ExchangeOnCollide || !HasComp<DiseaseCarrierComponent>(args.OtherEntity))
            return;

        Exchange(ent, args.OtherEntity, TouchRoute);
    }

    private void OnInjectorUsed(Entity<InertDiseaseCarrierComponent> ent, ref InjectorDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target is not { } target || !HasComp<DiseaseCarrierComponent>(target))
            return;

        Exchange(ent, target, NeedleRoute);
    }

    private void OnCleanAttempt(Entity<InertDiseaseCarrierComponent> ent, ref DiseaseContaminationCleanAttemptEvent args)
    {
        args.CanClean |= ent.Comp.Diseases is { Count: > 0 };
    }

    private void OnClean(Entity<InertDiseaseCarrierComponent> ent, ref DiseaseContaminationCleanedEvent args)
    {
        ClearDiseases(ent);
    }

    private void OnReaction(Entity<InertDiseaseCarrierComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        foreach (var reaction in args.Reagent.TileReactions)
        {
            if (reaction is CleanDecalsReaction cleaner && args.ReagentQuantity.Quantity >= cleaner.CleanCost) //TODO : Make a distinct reaction
            {
                ClearDiseases(ent);
                return;
            }
        }
    }

    private void OnContaminateAttempt(Entity<InertDiseaseCarrierComponent> ent, ref DiseaseContaminateAttemptEvent args)
    {
        TryContaminate(ent, args.Disease, ent.Comp.PickupChance(args.Route) * args.Chance);
    }

    private void Exchange(Entity<InertDiseaseCarrierComponent> ent, EntityUid user, ProtoId<DiseaseSpreadPrototype> route)
    {
        if (!HasComp<DiseaseCarrierComponent>(user))
            return;

        if (route == TouchRoute)
        {
            if (ent.Comp.NextContact.TryGetValue(user, out var next) && _timing.CurTime < next)
                return;

            ent.Comp.NextContact[user] = _timing.CurTime + ent.Comp.ExchangeCooldown;
        }

        TryTransmit(ent, user, route);
        TryPickUp(ent, user, route);
    }

    public bool TryTransmit(Entity<InertDiseaseCarrierComponent> ent, EntityUid target, ProtoId<DiseaseSpreadPrototype> route)
    {
        if (ent.Comp.Spread(route) is not { } spread || !TryPickDisease(ent, out var disease))
            return false;

        return _disease.DoInfectionAttempt(target,
            disease.Value.Owner,
            spread.Power,
            spread.Chance * disease.Value.Comp.InfectionProgress,
            spread.Type);
    }

    public bool TryPickUp(Entity<InertDiseaseCarrierComponent> ent, EntityUid source, ProtoId<DiseaseSpreadPrototype> route)
    {
        if (!TryComp<DiseaseCarrierComponent>(source, out var carrier))
            return false;

        var candidates = new List<Entity<DiseaseComponent>>();
        foreach (var uid in carrier.Diseases.ContainedEntities)
        {
            if (!_diseaseQuery.TryComp(uid, out var disease) || !_proto.Index(disease.DiseaseType).CanContaminate)
                continue;

            // A needle moves material on its own, but being left by touch means the disease sheds.
            if (route == TouchRoute && !disease.Effects.ContainedEntities.Any(effect =>
                    TryComp<DiseaseSpreadEffectComponent>(effect, out var spread) &&
                    spread.SpreadParams.Power > 0f && spread.SpreadParams.Chance > 0f))
                continue;

            candidates.Add((uid, disease));
        }

        if (candidates.Count == 0)
            return false;

        var carried = _random.Pick(candidates);

        // Power only gates here, gear that nullifies it blocks the deposit. Chance alone is the odds.
        var ev = new DiseaseOutgoingSpreadAttemptEvent(1f, ent.Comp.PickupChance(route), route);
        RaiseLocalEvent(source, ref ev);
        return ev.Power > 0f && TryContaminate(ent, carried, ev.Chance * carried.Comp.InfectionProgress);
    }

    public bool TryContaminate(Entity<InertDiseaseCarrierComponent> ent, Entity<DiseaseComponent> source, float chance)
    {
        if (chance <= 0f
            || !_proto.Index(source.Comp.DiseaseType).CanContaminate
            || !_random.Prob(Math.Clamp(chance, 0f, 1f)))
            return false;

        // Top up a disease already here instead of taking a second slot for the same lineage.
        foreach (var held in ent.Comp.Diseases?.ContainedEntities ?? [])
        {
            if (!_diseaseQuery.TryComp(held, out var existing)
                || DiseaseGenotype.Distance(source.Comp.Genotype, existing.Genotype) > ent.Comp.DiseaseTolerance)
                continue;

            _disease.ChangeInfectionProgress((held, existing),
                MathF.Max(0f, ent.Comp.ProgressOnPickup - existing.InfectionProgress));
            return true;
        }

        if ((ent.Comp.Diseases?.Count ?? 0) >= ent.Comp.MaxDiseases)
            return false;

        var clone = _disease.TryClone((source.Owner, source.Comp));
        if (clone == null)
            return false;

        ent.Comp.Diseases ??= _container.EnsureContainer<Container>(ent.Owner, InertDiseaseCarrierComponent.DiseaseContainerId);

        _transform.SetCoordinates(clone.Value, new EntityCoordinates(ent, Vector2.Zero));
        if (!_container.Insert(clone.Value, ent.Comp.Diseases))
        {
            QueueDel(clone);
            return false;
        }

        _disease.ChangeInfectionProgress(clone.Value, ent.Comp.ProgressOnPickup);
        return true;
    }

    public void ClearDiseases(Entity<InertDiseaseCarrierComponent> ent)
    {
        if (ent.Comp.Diseases == null)
            return;

        foreach (var uid in ent.Comp.Diseases.ContainedEntities.ToArray())
        {
            // Remove immediately so neither transmission nor a new deposit can reuse a dead disease.
            _container.Remove(uid, ent.Comp.Diseases, reparent: false, force: true);
            QueueDel(uid);
        }
    }

    /// <summary>
    /// Picks one of the held diseases at random, weighted by how present each one is.
    /// </summary>
    private bool TryPickDisease(Entity<InertDiseaseCarrierComponent> ent, [NotNullWhen(true)] out Entity<DiseaseComponent>? picked)
    {
        picked = null;
        if (ent.Comp.Diseases == null)
            return false;

        var total = 0f;
        foreach (var uid in ent.Comp.Diseases.ContainedEntities)
        {
            if (_diseaseQuery.TryComp(uid, out var disease))
                total += MathF.Max(disease.InfectionProgress, 0f);
        }

        if (total <= 0f)
            return false;

        var roll = _random.NextFloat() * total;
        foreach (var uid in ent.Comp.Diseases.ContainedEntities)
        {
            if (!_diseaseQuery.TryComp(uid, out var disease) || disease.InfectionProgress <= 0f)
                continue;

            roll -= MathF.Max(disease.InfectionProgress, 0f);
            if (roll > 0f)
                continue;

            picked = (uid, disease);
            return true;
        }

        return false;
    }
}
