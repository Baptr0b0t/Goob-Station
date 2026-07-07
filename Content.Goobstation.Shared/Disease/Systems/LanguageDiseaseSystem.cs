using Content.Goobstation.Shared.Disease.Components;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Disease.Systems;

/// <summary>
/// Relays <see cref="EntitySpokeEvent"/> to diseases and handles vocal parasite activation.
/// </summary>
public sealed partial class LanguageDiseaseSystem : EntitySystem
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseCarrierComponent, EntitySpokeEvent>(OnCarrierSpoke);

        SubscribeLocalEvent<LanguageDiseaseComponent, MapInitEvent>(OnDiseaseInit);
        SubscribeLocalEvent<LanguageDiseaseComponent, EntitySpokeEvent>(OnDiseaseSpoke);

        // Goob raises DiseaseCloneEvent on the freshly spawned clone (with the original as Source),
        // unlike Trauma which raises it on the source with a Cloned field. So hook the generic
        // DiseaseComponent event and copy the language data from the source onto the clone.
        SubscribeLocalEvent<DiseaseComponent, DiseaseCloneEvent>(OnClonedInto);
    }

    private void OnCarrierSpoke(Entity<DiseaseCarrierComponent> ent, ref EntitySpokeEvent args)
    {
        foreach (var disease in ent.Comp.Diseases.ContainedEntities)
        {
            RaiseLocalEvent(disease, args);
        }
    }

    private void OnDiseaseInit(Entity<LanguageDiseaseComponent> ent, ref MapInitEvent args)
    {
        // start off dormant
        _disease.SetInfectionRate(ent.Owner, 0f);
    }

    private void OnDiseaseSpoke(Entity<LanguageDiseaseComponent> ent, ref EntitySpokeEvent args)
    {
        // ignore non-target languages
        var id = new ProtoId<LanguagePrototype>(args.Language.ID);
        if (ent.Comp.Languages.Contains(id) == ent.Comp.Inverted)
            return;

        // trigger the disease
        _disease.SetInfectionRate(ent.Owner, ent.Comp.TriggerInfectionRate);
    }

    private void OnClonedInto(Entity<DiseaseComponent> ent, ref DiseaseCloneEvent args)
    {
        if (!TryComp<LanguageDiseaseComponent>(args.Source, out var source))
            return;

        var comp = EnsureComp<LanguageDiseaseComponent>(ent);
        comp.Languages = new(source.Languages);
        comp.TriggerInfectionRate = source.TriggerInfectionRate;
        comp.Inverted = source.Inverted;
        Dirty(ent.Owner, comp);
    }

    // TODO: have some way with surgery to do the devils house and add to comp.languages
}
