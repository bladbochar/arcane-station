using Content.Server.Chat.Systems;
using Content.Shared._Arcane.ERP;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Arcane.ERP;

public sealed class OrgasmSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId HeartsProto = new("EffectHearts");
    private static readonly EntProtoId SemenPuddleProto = new("PuddleSemen");

    private static readonly string[] OrgasmMessages =
    [
        "orgasm-message-1",
        "orgasm-message-2",
        "orgasm-message-3",
        "orgasm-message-4",
        "orgasm-message-5",
        "orgasm-message-6",
    ];

    private static readonly Dictionary<Gender, ProtoId<SoundCollectionPrototype>> MoanSounds = new()
    {
        { Gender.Male, new ProtoId<SoundCollectionPrototype>("MoansMale") },
        { Gender.Female, new ProtoId<SoundCollectionPrototype>("MoansFemale") },
    };

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
        _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(OrgasmMessages)), InGameICChatType.Emote, true);
        _popup.PopupEntity(Loc.GetString("orgasm-popup-self"), uid, uid, PopupType.MediumCaution);

        if (gender == Gender.Male)
            SpawnEjaculation(uid);
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
        var collection = MoanSounds.GetValueOrDefault(gender, MoanSounds[Gender.Female]);

        if (!_prototype.TryIndex(collection, out var soundCollection))
            return;

        if (soundCollection.PickFiles.Count == 0)
            return;

        // Pick from the last 2-3 files — the most intense moans in the collection.
        var count = soundCollection.PickFiles.Count;
        var index = count - 1 + _random.Next(-2, 1);
        index = Math.Clamp(index, 0, count - 1);

        _audio.PlayPvs(new ResolvedCollectionSpecifier(collection, index), uid, new AudioParams
        {
            Variation = 0.1f,
            MaxDistance = 6f,
            Volume = 3f,
        });
    }
}
