using Content.Shared._Arcane.SiliconStanding;
using Content.Shared.ActionBlocker;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Arcane.SiliconStanding;

public sealed class SiliconRestingVisualizerSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedSiliconStandingSystem _standing = default!;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("silicon.resting");

        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestingShutdown);
    }

    private void OnRestingStartup(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        _log.Debug($"OnRestingStartup: {ToPrettyString(ent.Owner)}");
        _actionBlocker.UpdateCanMove(ent.Owner);
        Refresh(ent.Owner, overrideResting: true);
    }

    private void OnRestingShutdown(Entity<SiliconRestingComponent> ent, ref ComponentShutdown args)
    {
        _log.Debug($"OnRestingShutdown: {ToPrettyString(ent.Owner)}");
        _actionBlocker.UpdateCanMove(ent.Owner);
        // ComponentShutdown fires before the component is actually removed,
        // so HasComp still returns true here. Override explicitly.
        Refresh(ent.Owner, overrideResting: false);
    }

    public void Refresh(EntityUid uid, SpriteComponent? sprite = null, bool? overrideResting = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        if (!TryComp<BorgChassisComponent>(uid, out var borg))
            return;

        var isResting = overrideResting ?? _standing.GetEffectiveResting(uid);

        _log.Debug($"Refresh: {ToPrettyString(uid)} isResting={isResting} override={overrideResting}");

        UpdateBorgBodyState((uid, sprite), isResting);

        if (!_appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        var lightVisible = !isResting && (borg.BrainEntity != null || hasPlayer);
        _sprite.LayerSetVisible((uid, sprite), BorgVisualLayers.Light, lightVisible);
        if (_sprite.LayerMapTryGet((uid, sprite), BorgVisualLayers.LightStatus, out _, false))
            _sprite.LayerSetVisible((uid, sprite), BorgVisualLayers.LightStatus, lightVisible);
        _sprite.LayerSetRsiState((uid, sprite), BorgVisualLayers.Light, hasPlayer ? borg.HasMindState : borg.NoMindState);
    }

    private void UpdateBorgBodyState(Entity<SpriteComponent?> ent, bool isResting)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<SiliconRestingVisualsComponent>(ent, out var visuals))
        {
            _log.Debug($"UpdateBorgBodyState: no SiliconRestingVisualsComponent on {ToPrettyString(ent.Owner)}");
            return;
        }

        if (!_sprite.LayerMapTryGet(ent, BorgVisualLayers.Body, out var layer, false))
        {
            _log.Debug($"UpdateBorgBodyState: no Body layer on {ToPrettyString(ent.Owner)}");
            return;
        }

        var state = isResting ? visuals.RestBodyState : visuals.NormalBodyState;
        _log.Debug($"UpdateBorgBodyState: {ToPrettyString(ent.Owner)} state={state} (rest={visuals.RestBodyState} normal={visuals.NormalBodyState})");
        _sprite.LayerSetRsiState(ent, layer, state);
    }
}
