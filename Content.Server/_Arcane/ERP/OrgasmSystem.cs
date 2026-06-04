using Content.Server.Chat.Systems;
using Content.Shared._Arcane.ERP;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.ERP;

public sealed class OrgasmSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId HeartsProto = "EffectHearts";
    private static readonly EntProtoId SemenSplatProto = "EffectSemenSplat";

    private static readonly EntProtoId[] SemenPuddleProtos =
    [
        "PuddleSemen1", "PuddleSemen2", "PuddleSemen3", "PuddleSemen4",
    ];

    private static readonly EntProtoId[] FemCumPuddleProtos =
    [
        "PuddleFemCum1", "PuddleFemCum2", "PuddleFemCum3", "PuddleFemCum4",
    ];
    private static readonly ProtoId<LocalizedDatasetPrototype> OrgasmMessagesDataset = "OrgasmMessages";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArousalComponent, ArousalOrgasmEvent>(OnOrgasm);
    }

    private void OnOrgasm(Entity<ArousalComponent> ent, ref ArousalOrgasmEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        DoOrgasmEffects(ent, humanoid);
    }

    public void DoOrgasmEffects(EntityUid uid, HumanoidAppearanceComponent? humanoid = null)
    {
        Resolve(uid, ref humanoid, false);

        Spawn(HeartsProto, _transform.GetMapCoordinates(uid));
        PlayOrgasmSound(uid, humanoid?.Gender ?? Gender.Female);

        if (_prototype.TryIndex(OrgasmMessagesDataset, out var dataset))
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(dataset.Values)), InGameICChatType.Emote, false);

        _popup.PopupEntity(Loc.GetString("orgasm-popup-self"), uid, uid, PopupType.MediumCaution);

        if (humanoid != null)
            SpawnEjaculation(uid, humanoid.Sex);

        var weakness = EnsureComp<OrgasmWeaknessComponent>(uid);
        weakness.ExpiresAt = _timing.CurTime + weakness.WeaknessDuration;
        Dirty(uid, weakness);
    }

    private void SpawnEjaculation(EntityUid uid, Sex sex)
    {
        var puddleProtos = sex is Sex.Female ? FemCumPuddleProtos : SemenPuddleProtos;

        var xform = Transform(uid);
        var forward = xform.LocalRotation.ToVec();
        var coords = xform.Coordinates.Offset(forward * 0.6f);
        Spawn(SemenSplatProto, coords);
        Spawn(_random.Pick(puddleProtos), coords);

        AddCumOverlay(uid);

        var mapCoords = _transform.ToMapCoordinates(coords);
        foreach (var target in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(mapCoords, 0.8f))
        {
            if (target.Owner == uid)
                continue;
            AddCumOverlay(target.Owner);
        }
    }

    public void AddCumOverlay(EntityUid uid)
    {
        var overlay = EnsureComp<CumOverlayComponent>(uid);
        overlay.Count++;
        Dirty(uid, overlay);
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
