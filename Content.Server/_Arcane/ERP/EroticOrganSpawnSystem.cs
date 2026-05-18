using Content.Shared._Arcane.ERP.Organs;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.ERP;

public sealed class EroticOrganSpawnSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnProfileLoaded);
        SubscribeLocalEvent<HumanoidAppearanceComponent, SexChangedEvent>(OnSexChanged);
    }

    private void OnMapInit(EntityUid uid, HumanoidAppearanceComponent humanoid, MapInitEvent args)
    {
        SpawnEroticOrgans(uid, humanoid.Sex);
    }

    private void OnProfileLoaded(EntityUid uid, HumanoidAppearanceComponent humanoid, ProfileLoadFinishedEvent args)
    {
        RemoveEroticOrgans(uid);
        SpawnEroticOrgans(uid, humanoid.Sex);
    }

    private void OnSexChanged(EntityUid uid, HumanoidAppearanceComponent humanoid, SexChangedEvent args)
    {
        RemoveEroticOrgans(uid);
        SpawnEroticOrgans(uid, args.NewSex);
    }

    private void SpawnEroticOrgans(EntityUid uid, Sex sex)
    {
        if (sex == Sex.Unsexed)
            return;

        var groin = GetBodyPartOfType(uid, BodyPartType.Groin);
        var chest = GetBodyPartOfType(uid, BodyPartType.Chest);

        if (groin.HasValue)
            TrySpawnOrgan(uid, groin.Value, "OrganAnus", "anus");

        switch (sex)
        {
            case Sex.Male:
                if (groin.HasValue)
                {
                    TrySpawnOrgan(uid, groin.Value, "OrganPenis", "penis");
                    TrySpawnOrgan(uid, groin.Value, "OrganTesticles", "testicles");
                }
                break;

            case Sex.Female:
                if (groin.HasValue)
                {
                    TrySpawnOrgan(uid, groin.Value, "OrganVagina", "vagina");
                    TrySpawnOrgan(uid, groin.Value, "OrganUterus", "uterus");
                }
                if (chest.HasValue)
                    TrySpawnOrgan(uid, chest.Value, "OrganBreasts", "breasts");
                break;

            case Sex.Futanari:
                if (groin.HasValue)
                {
                    TrySpawnOrgan(uid, groin.Value, "OrganPenis", "penis");
                    TrySpawnOrgan(uid, groin.Value, "OrganTesticles", "testicles");
                    TrySpawnOrgan(uid, groin.Value, "OrganVagina", "vagina");
                    TrySpawnOrgan(uid, groin.Value, "OrganUterus", "uterus");
                }
                if (chest.HasValue)
                    TrySpawnOrgan(uid, chest.Value, "OrganBreasts", "breasts");
                break;
        }

        RaiseLocalEvent(uid, new EroticOrgansSpawnedEvent());
    }

    private void RemoveEroticOrgans(EntityUid bodyUid)
    {
        var organs = _body.GetBodyOrganEntityComps<EroticOrganComponent>((bodyUid, null));
        foreach (var organ in organs)
        {
            _body.RemoveOrgan(organ.Owner, organ.Comp2);
            QueueDel(organ.Owner);
        }
    }

    private void TrySpawnOrgan(EntityUid bodyUid, EntityUid partUid, string protoId, string slotId)
    {
        if (!_proto.HasIndex<EntityPrototype>(protoId))
            return;

        // Create slot if it doesn't exist yet
        _body.TryCreateOrganSlot(partUid, slotId, out _);

        // Don't double-spawn
        var containerId = SharedBodySystem.GetOrganContainerId(slotId);
        if (_containers.TryGetContainer(partUid, containerId, out var container)
            && container.ContainedEntities.Count > 0)
            return;

        var organEnt = Spawn(protoId, Transform(partUid).Coordinates);
        if (!_body.InsertOrgan(partUid, organEnt, slotId))
            QueueDel(organEnt);
    }

    private EntityUid? GetBodyPartOfType(EntityUid bodyUid, BodyPartType partType)
    {
        foreach (var (partUid, _) in _body.GetBodyChildrenOfType(bodyUid, partType))
            return partUid;

        return null;
    }
}

public sealed class EroticOrgansSpawnedEvent : EntityEventArgs;
