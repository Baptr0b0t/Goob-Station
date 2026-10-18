using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Disease.Components;

/// <summary>
/// Lets a lifeless object hold diseases and pass them on. They only fade here, they never progress.
/// Presence is kept as the disease's <see cref="DiseaseComponent.InfectionProgress"/>, unused off a host.
/// </summary>
[RegisterComponent]
public sealed partial class InertDiseaseCarrierComponent : Component
{
    [ViewVariables]
    public const string DiseaseContainerId = "inertDiseaseContainer";

    /// <summary>
    /// Created on first use so untouched prototypes stay map saveable.
    /// </summary>
    [ViewVariables]
    public Container? Diseases;

    /// <summary>
    /// Routes this object can be contaminated through. An unlisted route cannot contaminate it.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DiseaseSpreadPrototype>, float> PickupParams = new();

    [DataField]
    public List<DiseaseSpreadSpecifier> SpreadParams = new();

    [DataField]
    public float ProgressOnPickup = 1f;

    /// <summary>
    /// Presence lost per second at environmental resistance 1. Lifetime is resistance divided by this.
    /// </summary>
    [DataField]
    public float DecayRate = 0.01f;

    [DataField]
    public float EmitRange = 2f;

    [DataField]
    public int MaxDiseases = 3;

    /// <summary>
    /// Drift needed before a disease takes its own slot, so one lineage cannot fill the object.
    /// </summary>
    [DataField]
    public int DiseaseTolerance = 3;

    [DataField]
    public bool ExchangeOnCollide;

    /// <summary>
    /// Minimum time between contact exchanges with the same person.
    /// </summary>
    [DataField]
    public TimeSpan ExchangeCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> NextContact = new();

    public float PickupChance(ProtoId<DiseaseSpreadPrototype> route)
    {
        return PickupParams.GetValueOrDefault(route, 0f);
    }

    public DiseaseSpreadSpecifier? Spread(ProtoId<DiseaseSpreadPrototype> route)
    {
        foreach (var spread in SpreadParams)
        {
            if (spread.Type == route)
                return spread;
        }

        return null;
    }
}
