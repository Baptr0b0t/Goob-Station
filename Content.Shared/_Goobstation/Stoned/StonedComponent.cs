// SPDX-FileCopyrightText: 2025 GoobStation <goobstation@example.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Stoned;

/// <summary>
/// Turns the entity to stone: applies a procedural grey stone visual,
/// completely prevents movement, and pauses sprite animations.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class StonedComponent : Component
{
    /// <summary>
    /// Controls the stone grain size. Higher = coarser pattern.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GrainScale = 1.0f;
}
