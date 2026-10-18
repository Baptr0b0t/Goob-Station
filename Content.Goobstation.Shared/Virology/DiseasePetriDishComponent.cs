using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Virology;

/// <summary>
/// Holds one disease alive outside of a host, so it can be sampled again later.
/// </summary>
[RegisterComponent]
public sealed partial class DiseasePetriDishComponent : Component
{

    [ViewVariables]
    public EntityUid? DiseaseUid;

    [ViewVariables]
    public TimeSpan SampleReadyAt;

    /// <summary>
    /// How long the culture needs to regrow between samples.
    /// </summary>
    [DataField]
    public TimeSpan CultureDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public SoundSpecifier SampleSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}
