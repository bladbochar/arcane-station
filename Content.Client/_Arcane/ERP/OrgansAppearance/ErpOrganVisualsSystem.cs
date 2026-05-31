using Content.Client._Arcane.ERP.Preferences;
using Content.Client.Lobby;
using Content.Shared._Arcane.ERP.Organs;
using Content.Shared._Arcane.ERP.OrgansAppearance;
using Content.Shared._Arcane.ERP.Preferences;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Arcane.ERP.OrgansAppearance;

public sealed class ErpOrganVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ClientErpOrganPreferencesManager _erpPrefs = default!;
    [Dependency] private readonly IClientPreferencesManager _prefs = default!;

    private static readonly Dictionary<string, string> SlotToLayer = new()
    {
        [ErpOrganSlots.Penis]     = "erp_penis",
        [ErpOrganSlots.Vagina]    = "erp_vagina",
        [ErpOrganSlots.Breasts]   = "erp_breasts",
        [ErpOrganSlots.Testicles] = "erp_testicles",
        [ErpOrganSlots.Anus]      = "erp_anus",
        [ErpOrganSlots.Butt]      = "erp_butt",
    };

    private static readonly string[] GroinSlots = ["underwear", "jumpsuit", "outerClothing"];
    private static readonly string[] ChestSlots = ["undershirt", "jumpsuit", "outerClothing"];

    private static readonly Dictionary<string, string[]> OrganCoveringSlots = new()
    {
        [ErpOrganSlots.Penis]     = GroinSlots,
        [ErpOrganSlots.Vagina]    = GroinSlots,
        [ErpOrganSlots.Testicles] = GroinSlots,
        [ErpOrganSlots.Anus]      = GroinSlots,
        [ErpOrganSlots.Butt]      = GroinSlots,
        [ErpOrganSlots.Breasts]   = ChestSlots,
    };

    private static readonly Dictionary<string, string> OrganRsiPath = new()
    {
        [ErpOrganSlots.Penis]     = "/Textures/_Arcane/ERP/Mobs/penis_onmob.rsi",
        [ErpOrganSlots.Vagina]    = "/Textures/_Arcane/ERP/Mobs/vagina_onmob.rsi",
        [ErpOrganSlots.Testicles] = "/Textures/_Arcane/ERP/Mobs/testicles_onmob.rsi",
        [ErpOrganSlots.Butt]      = "/Textures/_Arcane/ERP/Mobs/butt_onmob.rsi",
        [ErpOrganSlots.Anus]      = "/Textures/_Arcane/ERP/Mobs/anus_onmob.rsi",
    };

    private const string BreastsRsiBase = "/Textures/_Arcane/ERP/Mobs/Breasts/";
    private const string BreastsRsiFallback = BreastsRsiBase + "human.rsi";

    private static readonly Dictionary<string, string> SpeciesBreastRsi = new()
    {
        ["Human"]       = BreastsRsiBase + "human.rsi",
        ["Dwarf"]       = BreastsRsiBase + "human.rsi",
        ["Reptilian"]   = BreastsRsiBase + "lizard.rsi",
        ["Moth"]        = BreastsRsiBase + "moth.rsi",
        ["Tajaran"]     = BreastsRsiBase + "tajaran.rsi",
        ["Arachnid"]    = BreastsRsiBase + "arachnid.rsi",
        ["Demon"]       = BreastsRsiBase + "demon.rsi",
        ["HumanoidXeno"] = BreastsRsiBase + "xenos.rsi",
    };

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("erp.visuals.cl");

        SubscribeLocalEvent<ErpOrganVisualsComponent, AfterAutoHandleStateEvent>(OnOrganState);
        SubscribeLocalEvent<ErpOrganVisualsComponent, ComponentShutdown>(OnOrganShutdown);

        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidVisualStateUpdatedEvent>(OnHumanoidState);
        SubscribeLocalEvent<ErpOrganVisualsComponent, DidEquipEvent>(OnInventoryChanged);
        SubscribeLocalEvent<ErpOrganVisualsComponent, DidUnequipEvent>(OnInventoryChanged);

        // Editor preview: client-side dummy entity, no server state
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnPreviewProfileLoaded);
    }

    public void RefreshPreview(EntityUid uid, ErpOrganPreferences prefs)
    {
        if (!IsClientSide(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var humanoid = CompOrNull<HumanoidAppearanceComponent>(uid);
        var visuals = EnsureComp<ErpOrganVisualsComponent>(uid);
        visuals.Organs = FilterOrgansBySex(prefs.Organs, humanoid?.Sex ?? Sex.Male);

        ApplyOrganLayers((uid, visuals), humanoid, sprite);
    }

    private void OnPreviewProfileLoaded(Entity<HumanoidAppearanceComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        if (!IsClientSide(ent))
            return;

        if (!HasComp<EroticOrgansComponent>(ent))
            return;

        var slot = _prefs.Preferences?.SelectedCharacterIndex ?? 0;
        var organPrefs = _erpPrefs.GetSlot(slot);

        var visuals = EnsureComp<ErpOrganVisualsComponent>(ent);
        visuals.Organs = FilterOrgansBySex(organPrefs.Organs, ent.Comp.Sex);

        if (TryComp<SpriteComponent>(ent, out var sprite))
            ApplyOrganLayers((ent, visuals), ent.Comp, sprite);
    }

    private static Dictionary<string, ErpOrganConfig> FilterOrgansBySex(
        Dictionary<string, ErpOrganConfig> organs, Sex sex)
    {
        var result = new Dictionary<string, ErpOrganConfig>();
        foreach (var (slotId, cfg) in organs)
        {
            if (ErpOrganSlots.SexFilter.TryGetValue(slotId, out var allowed) && Array.IndexOf(allowed, sex) < 0)
                continue;
            result[slotId] = cfg;
        }
        return result;
    }

    private void OnOrganState(Entity<ErpOrganVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _log.Debug($"OnOrganState {ent}, organs={ent.Comp.Organs.Count}");
        if (!TryComp<SpriteComponent>(ent, out var sprite))
        {
            _log.Debug($"{ent} — no SpriteComponent");
            return;
        }

        ApplyOrganLayers(ent, CompOrNull<HumanoidAppearanceComponent>(ent), sprite);
    }

    private void OnHumanoidState(Entity<HumanoidAppearanceComponent> ent, ref HumanoidVisualStateUpdatedEvent args)
    {
        if (!HasComp<ErpOrganVisualsComponent>(ent))
            return;

        UpdateOrganVisibility(ent);
    }

    private void OnInventoryChanged(Entity<ErpOrganVisualsComponent> ent, ref DidEquipEvent args)
        => UpdateOrganVisibility(ent);

    private void OnInventoryChanged(Entity<ErpOrganVisualsComponent> ent, ref DidUnequipEvent args)
        => UpdateOrganVisibility(ent);

    private void UpdateOrganVisibility(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var slotId in ErpOrganSlots.All)
        {
            if (!SlotToLayer.TryGetValue(slotId, out var layerKey))
                continue;

            if (!_sprite.LayerMapTryGet((uid, sprite), layerKey, out var index, false))
                continue;

            _sprite.LayerSetVisible((uid, sprite), index, IsOrganVisible(slotId, uid));
        }
    }

    private void OnOrganShutdown(Entity<ErpOrganVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var layerKey in SlotToLayer.Values)
        {
            if (_sprite.LayerMapTryGet((ent, sprite), layerKey, out var index, false))
                _sprite.LayerSetVisible((ent, sprite), index, false);
        }
    }

    private void ApplyOrganLayers(Entity<ErpOrganVisualsComponent> ent, HumanoidAppearanceComponent? humanoid, SpriteComponent sprite)
    {
        foreach (var slotId in ErpOrganSlots.All)
        {
            if (slotId == ErpOrganSlots.Butt) // Disabled for now since it needs better icons.
                continue;

            if (!SlotToLayer.TryGetValue(slotId, out var layerKey))
                continue;

            string rsiPath;
            if (slotId == ErpOrganSlots.Breasts)
            {
                var species = humanoid?.Species ?? string.Empty;
                rsiPath = SpeciesBreastRsi.TryGetValue(species, out var r) ? r : BreastsRsiFallback;
            }
            else if (!OrganRsiPath.TryGetValue(slotId, out rsiPath!))
                continue;

            if (!ent.Comp.Organs.TryGetValue(slotId, out var cfg))
            {
                if (_sprite.LayerMapTryGet((ent, sprite), layerKey, out var hiddenIdx, false))
                    _sprite.LayerSetVisible((ent, sprite), hiddenIdx, false);
                continue;
            }

            var stateName = BuildStateName(slotId, cfg, humanoid?.Species);
            var visible = IsOrganVisible(slotId, ent.Owner);
            _log.Debug($"layer {slotId} state={stateName} visible={visible}");

            if (!_sprite.LayerMapTryGet((ent, sprite), layerKey, out var index, false))
                continue;

            _sprite.LayerSetRsi((ent, sprite), index, new ResPath(rsiPath), stateName);
            _sprite.LayerSetColor((ent, sprite), index, cfg.Color ?? humanoid?.SkinColor ?? Color.FromHex("#C0967F"));
            _sprite.LayerSetVisible((ent, sprite), index, visible);
        }
    }

    private bool IsOrganVisible(string slotId, EntityUid uid)
    {
        if (!OrganCoveringSlots.TryGetValue(slotId, out var slots))
            return true;

        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                return false;
        }

        return true;
    }

    private static string BuildStateName(string slotId, ErpOrganConfig cfg, string? species = null)
    {
        switch (slotId)
        {
            case ErpOrganSlots.Breasts:
                if (species == "HumanoidXeno")
                    return cfg.Size switch { 1 => "a", 2 => "b", _ => "c" };
                return cfg.Size switch { 1 => "aa", 2 => "b", 3 => "c", _ => "d" };
            case ErpOrganSlots.Butt:
                return $"butt_pair_{Math.Clamp(cfg.Size, 1, 5)}_0_FRONT";
            case ErpOrganSlots.Testicles:
                return "testicles_single_2_0_FRONT";
            case ErpOrganSlots.Anus:
                var aVariant = cfg.Variant is "" or "human" ? "donut" : cfg.Variant; // "human" = legacy default, map to first real variant
                return $"anus_{aVariant}_3_0_FRONT";
            case ErpOrganSlots.Vagina:
                return $"vagina_{cfg.Variant}_1_0_FRONT";
            default:
                return $"{slotId}_{cfg.Variant}_3_0_FRONT";
        }
    }

}
