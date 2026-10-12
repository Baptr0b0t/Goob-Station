using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.Interaction;

/// <summary>
/// Prevents interaction particles from copying his sprite.
/// Particles show <see cref="Sprite"/> instead, or a generic hand sprite if it's not set.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HideInteractionParticleSpriteComponent : Component
{
    /// <summary>
    /// Sprite shown by interaction particles.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;
}
