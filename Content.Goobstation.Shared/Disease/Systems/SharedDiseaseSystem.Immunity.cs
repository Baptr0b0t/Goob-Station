using Content.Goobstation.Shared.Disease.Components;

namespace Content.Goobstation.Shared.Disease.Systems;

public partial class SharedDiseaseSystem
{
    private void InitializeImmunity()
    {
        SubscribeLocalEvent<ImmunityComponent, GetImmunityEvent>(OnGetImmunity);
        SubscribeLocalEvent<ImmunityComponent, DiseaseGainedEvent>(OnImmunityDiseaseGained);
        SubscribeLocalEvent<ImmunityComponent, DiseaseInfectAttemptEvent>(OnImmunityInfectAttempt);
    }

    private void OnGetImmunity(Entity<ImmunityComponent> ent, ref GetImmunityEvent args)
    {
        args.ImmunityGainRate += ent.Comp.ImmunityGainRate;
        args.ImmunityStrength += ent.Comp.ImmunityStrength;
    }

    private void OnImmunityDiseaseGained(Entity<ImmunityComponent> ent, ref DiseaseGainedEvent args)
    {
        if (!args.Disease.Comp.CanGainImmunity)
            return;

        TryAddImmunity((ent, ent.Comp), (args.Disease, args.Disease.Comp));
    }

    private void OnImmunityInfectAttempt(Entity<ImmunityComponent> ent, ref DiseaseInfectAttemptEvent args)
    {
        if (HasImmunity((ent, ent.Comp), (args.Disease, args.Disease.Comp)))
            args.CanInfect = false;
    }

    #region public API

    /// <summary>
    /// Checks whether the entity has developed an immunity to this genotype
    /// </summary>
    public bool HasImmunity(Entity<ImmunityComponent?> ent, int genotype)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var immunity in ent.Comp.ImmuneTo)
        {
            if (immunity.Covers(genotype))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether the entity has developed an immunity to this disease
    /// </summary>
    public bool HasImmunity(Entity<ImmunityComponent?> ent, Entity<DiseaseComponent?> disease)
    {
        if (!Resolve(disease, ref disease.Comp))
            return false;

        return HasImmunity(ent, disease.Comp.Genotype);
    }

    /// <summary>
    /// Checks whether this entity can be infected by diseases of this genotype
    /// </summary>
    public bool CanInfect(Entity<DiseaseCarrierComponent?, ImmunityComponent?> ent, int genotype)
    {
        return !HasDisease((ent, ent.Comp1), genotype) && !HasImmunity((ent, ent.Comp2), genotype);
    }

    /// <summary>
    /// Grants immunity against a genotype and those within the given Hamming distance.
    /// </summary>
    public bool TryAddImmunity(Entity<ImmunityComponent?> ent, int genotype, int tolerance = 0)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var immunity in ent.Comp.ImmuneTo)
        {
            // Covering the new center alone does not cover its entire tolerance radius.
            if (DiseaseGenotype.Distance(immunity.Genotype, genotype) + tolerance <= immunity.Tolerance)
                return true;
        }

        ent.Comp.ImmuneTo.Add(new DiseaseImmunitySpecifier(genotype, tolerance));
        return true;
    }

    public bool TryAddImmunity(Entity<ImmunityComponent?> ent, Entity<DiseaseComponent?> disease, int tolerance = 0)
    {
        if (!Resolve(disease, ref disease.Comp))
            return false;

        return TryAddImmunity(ent, disease.Comp.Genotype, tolerance);
    }

    public bool CanImmunityAffect(Entity<ImmunityComponent?> ent, DiseaseComponent disease)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (_mobState.IsDead(ent.Owner) && !ent.Comp.InDead)
            return false;

        return ent.Comp.AffectedTypes.Contains(disease.DiseaseType);
    }

    #endregion
}
