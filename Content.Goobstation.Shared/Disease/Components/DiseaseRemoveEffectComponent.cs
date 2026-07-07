using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Disease.Components;

/// <summary>
/// Disease effect that removes the disease from the carrier.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseRemoveEffectComponent : Component;
