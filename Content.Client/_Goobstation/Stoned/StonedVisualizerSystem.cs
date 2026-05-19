// SPDX-FileCopyrightText: 2025 GoobStation <goobstation@example.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Stoned;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Goobstation.Stoned;

public sealed class StonedVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderId = "Stoned";
    private ShaderPrototype? _shaderProto;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StonedComponent, AfterAutoHandleStateEvent>(OnStateHandled);
        SubscribeLocalEvent<StonedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStateHandled(Entity<StonedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Apply procedural stone PostShader
        _shaderProto ??= _protoMan.Index(ShaderId);
        var shader = _shaderProto.InstanceUnique();
        shader.SetParameter("tileScale", ent.Comp.GrainScale);
        sprite.PostShader = shader;

        // Pause all layer animations
        var i = 0;
        foreach (var _ in sprite.AllLayers)
            _sprite.LayerSetAutoAnimated((ent.Owner, sprite), i++, false);
    }

    private void OnShutdown(EntityUid uid, StonedComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.PostShader = null;

        // Restore all layer animations
        var i = 0;
        foreach (var _ in sprite.AllLayers)
            _sprite.LayerSetAutoAnimated((uid, sprite), i++, true);
    }
}
