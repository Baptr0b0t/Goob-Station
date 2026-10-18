using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Disease.Systems;

public sealed partial class DiseaseProtectionSystem : EntitySystem
{

    private const float InternalsChanceMultiplier = 0.1f; //Todo: Make it a value in internals component.

    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseProtectionComponent, InventoryRelayedEvent<DiseaseIncomingSpreadAttemptEvent>>(OnIncomingSpread);
        SubscribeLocalEvent<DiseaseProtectionComponent, InventoryRelayedEvent<DiseaseOutgoingSpreadAttemptEvent>>(OnOutgoingSpread);
        SubscribeLocalEvent<InternalsComponent, DiseaseIncomingSpreadAttemptEvent>(OnInternalsIncomingSpread);
    }

    private void OnInternalsIncomingSpread(Entity<InternalsComponent> ent, ref DiseaseIncomingSpreadAttemptEvent args)
    {
        if (!_proto.Index(args.Type).BlockedByInternals
            || !_internals.AreInternalsWorking(ent.Owner, ent.Comp))
            return;

        args.Chance *= InternalsChanceMultiplier;
    }

    private void OnIncomingSpread(Entity<DiseaseProtectionComponent> ent, ref InventoryRelayedEvent<DiseaseIncomingSpreadAttemptEvent> args)
    {
        args.Args.ApplyModifier(ent.Comp.Incoming);
    }

    private void OnOutgoingSpread(Entity<DiseaseProtectionComponent> ent, ref InventoryRelayedEvent<DiseaseOutgoingSpreadAttemptEvent> args)
    {
        args.Args.ApplyModifier(ent.Comp.Outgoing);
    }
}
