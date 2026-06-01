using Content.Server.Chat.Systems;
using Content.Shared._Arcane.ERP;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using System.Numerics;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.ERP;

public sealed class OrgasmSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId HeartsProto = new("EffectHearts");
    private static readonly EntProtoId SemenPuddleProto = new("PuddleSemen");
    private static readonly ProtoId<LocalizedDatasetPrototype> OrgasmMessagesDataset = new("OrgasmMessages");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArousalComponent, ArousalOrgasmEvent>(OnOrgasm);
    }

    private void OnOrgasm(Entity<ArousalComponent> ent, ref ArousalOrgasmEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        DoOrgasmEffects(ent, humanoid.Gender);
    }

    public void DoOrgasmEffects(EntityUid uid, Gender gender)
    {
        Spawn(HeartsProto, _transform.GetMapCoordinates(uid));
        PlayOrgasmSound(uid, gender);

        if (_prototype.TryIndex(OrgasmMessagesDataset, out var dataset))
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(dataset.Values)), InGameICChatType.Emote, false);

        _popup.PopupEntity(Loc.GetString("orgasm-popup-self"), uid, uid, PopupType.MediumCaution);

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            && humanoid.Sex is Sex.Male or Sex.Futanari)
            SpawnEjaculation(uid);

        var weakness = EnsureComp<OrgasmWeaknessComponent>(uid);
        weakness.ExpiresAt = _timing.CurTime + weakness.WeaknessDuration;
        Dirty(uid, weakness);
    }

    private void SpawnEjaculation(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;
        var count = _random.Next(2, 5);
        for (var i = 0; i < count; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-0.4f, 0.4f),
                _random.NextFloat(-0.4f, 0.4f));
            Spawn(SemenPuddleProto, coords.Offset(offset));
        }
    }

    private void PlayOrgasmSound(EntityUid uid, Gender gender)
    {
        var collection = ErpAudio.OrgasmSounds.GetValueOrDefault(gender, ErpAudio.OrgasmSounds[Gender.Female]);

        var audioParams = new AudioParams
        {
            Variation = 0.1f,
            MaxDistance = 6f,
            Volume = 3f,
        };

        _audio.PlayPvs(new SoundCollectionSpecifier(collection), uid, audioParams);
    }
}
