// SPDX-FileCopyrightText: 2025 GoobStation <goobstation@example.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Ghost.Components;
using Content.Shared._Goobstation.Stoned;
using Content.Shared.ActionBlocker;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Goobstation.Stoned;

public sealed class StonedSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StonedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StonedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, StonedComponent component, ComponentStartup args)
    {
        _blocker.UpdateCanMove(uid);

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
            _physics.SetAngularVelocity(uid, 0f, body: physics);
        }

        //EnsureComp<GhostOnMoveComponent>(uid);
    }

    private void OnShutdown(EntityUid uid, StonedComponent component, ComponentShutdown args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        //RemComp<GhostOnMoveComponent>(uid);
        _blocker.UpdateCanMove(uid);
    }
}
