using Content.Goobstation.Shared.Disease.Components;

namespace Content.Goobstation.Shared.Disease.Systems;

public sealed partial class DiseaseRemoveEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRemoveEffectComponent, DiseaseEffectEvent>(OnRemoveEffect);
    }

    private void OnRemoveEffect(Entity<DiseaseRemoveEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        _disease.TryCure(args.Ent.AsNullable(), args.Disease);
    }
}
