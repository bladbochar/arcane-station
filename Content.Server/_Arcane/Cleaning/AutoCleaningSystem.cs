using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Cleaning;

public sealed partial class AutoCleaningSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private bool _isActive = false;
    private bool _isWarned = false;
    private static TimeSpan _nextUpdate = TimeSpan.MaxValue;
    private static TimeSpan _updateInterval = TimeSpan.FromMinutes(30);
    private static TimeSpan _warningWaiting = TimeSpan.FromSeconds(30);
    private static HashSet<ProtoId<TagPrototype>> _cleaningTags = ["Trash", "Cartridge"];
    private static HashSet<ProtoId<TagPrototype>> _disallowedTags = ["Cigarette", "CigPack", "Syringe"];


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundStarted(RoundStartedEvent args)
    {
        _nextUpdate = _timing.CurTime + _updateInterval;
        _isActive = true;
    }

    private void OnRoundEnded(RoundEndedEvent args)
    {
        _isActive = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_isActive)
            return;

        if (_nextUpdate < _timing.CurTime)
        {
            if (_isWarned)
            {
                ProccessCleaning();
                return;
            }

            _nextUpdate = _timing.CurTime + _warningWaiting;
            _isWarned = true;

            _chat.DispatchGlobalAnnouncement(Loc.GetString("cent-com-cleaning-warning", ("seconds", _warningWaiting.Seconds)), colorOverride: Color.Aqua);
        }
    }

    private void ProccessCleaning()
    {
        _nextUpdate = _timing.CurTime + _updateInterval;
        _isWarned = false;

        _chat.DispatchGlobalAnnouncement(Loc.GetString("cent-com-cleaning-announce"), colorOverride: Color.Aqua);

        var query = EntityQueryEnumerator<TagComponent>();

        while (query.MoveNext(out var uid, out var tag))
        {
            if (tag.Tags.Intersect(_cleaningTags).Any() && !tag.Tags.Intersect(_disallowedTags).Any())
            {
                QueueDel(uid);
            }
        }
    }
}
