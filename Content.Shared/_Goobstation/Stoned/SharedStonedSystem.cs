// SPDX-FileCopyrightText: 2025 GoobStation <goobstation@example.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Speech;
using Content.Shared.Throwing;

namespace Content.Shared._Goobstation.Stoned;

public sealed class SharedStonedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StonedComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<StonedComponent, ChangeDirectionAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, UseAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, PickupAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, ThrowAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, AttackAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<StonedComponent, EmoteAttemptEvent>(OnCancel);
        SubscribeLocalEvent<StonedComponent, SpeakAttemptEvent>(OnCancel);
    }

    private void OnUpdateCanMove(EntityUid uid, StonedComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;
        args.Cancel();
    }

    private void OnCancel(EntityUid uid, StonedComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnInteractAttempt(Entity<StonedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
