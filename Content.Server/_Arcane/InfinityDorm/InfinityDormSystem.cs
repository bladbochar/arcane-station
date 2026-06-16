// MIT License

// Copyright (c) 2026 Vecortys (vecortys@gmail.com)

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Numerics;
using Content.Goobstation.Common.Effects;
using Content.Server.AlertLevel;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Arcane.CCVars;
using Content.Shared._Arcane.InfinityDorm;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.InfinityDorm;

public sealed class InfinityDormSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SparksSystem _sparks = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private int _maxUserDorms = 0;

    private int _step = 300;
    private float _lastPosition = -1000f;
    private MapId _dormsMapId = MapId.Nullspace;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InfinityDormTeleporterComponent, InfinityDormTeleportMessage>(HandleTeleporterMessage);
        SubscribeNetworkEvent<RequestDormsAmountEvent>(HandleDormsAmountRequest);

        Subs.CVar(_cfg, ACCVars.MaxUserInfinityDorms, SetMaxUserDorms, true);
    }

    private void SetMaxUserDorms(int value)
    {
        _maxUserDorms = value;
    }

    private void HandleTeleporterMessage(EntityUid uid, InfinityDormTeleporterComponent component, InfinityDormTeleportMessage args)
    {
        var station = _station.GetOwningStation(uid);
        if (station != null && _alertLevel.GetLevel(station.Value) != component.NeedAlertLevel)
            return;

        EnsureDormsMap();

        if (!TryCreateDorm(uid, args.Actor, args.Room, args.Number))
            return;

        var accessReader = EnsureComp<AccessReaderComponent>(uid);
        _accessReader.LogAccess((uid, accessReader), args.Actor);

        _sparks.DoSparks(Transform(args.Actor).Coordinates, 3, 10);

        TeleportToDorm(args.Actor, args.Number);
    }

    private void HandleDormsAmountRequest(RequestDormsAmountEvent args, EntitySessionEventArgs eventArgs)
    {
        var count = 0;
        var query = EntityQueryEnumerator<InfinityDormComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            if (comp.Creator == eventArgs.SenderSession.AttachedEntity)
                count++;
        }
        RaiseNetworkEvent(new UserDormCountMessage(count), eventArgs.SenderSession);
    }

    private bool TryCreateDorm(EntityUid teleporter, EntityUid creator, string room, int number)
    {
        if (IsDormExists(number))
            return true;

        if (!CheckUserDormsLimit(creator))
        {
            _chat.TrySendInGameICMessage(teleporter, Loc.GetString("infinity-dorm-warning-dorms-limit"), InGameICChatType.Speak, false);
            return false;
        }

        if (!_proto.TryIndex<InfinityDormPrototype>(room, out var dormProto))
            return false;

        var offset = new Vector2(_lastPosition + _step, 0);
        if (!_loader.TryLoadGrid(_dormsMapId, dormProto.GridPath, out var grid, offset: offset))
            return false;

        _lastPosition = offset.X;

        var dormComp = EnsureComp<InfinityDormComponent>(grid.Value);
        dormComp.ConnectedTeleporter = teleporter;
        dormComp.Number = number;
        dormComp.Creator = creator;

        return true;
    }

    private void TeleportToDorm(EntityUid uid, int number)
    {
        var query = EntityQueryEnumerator<InfinityDormSpawnMarkerComponent>();

        while (query.MoveNext(out var dormUid, out var _))
        {
            if (TryComp<InfinityDormComponent>(_transform.GetParentUid(dormUid), out var dormComp) && dormComp.Number != number)
                continue;

            var xform = Transform(dormUid);
            var targetCoords = new MapCoordinates(_transform.GetWorldPosition(dormUid), xform.MapID);

            _transform.SetMapCoordinates(uid, targetCoords);
            EnsureComp<InfinityDormVisitorComponent>(uid);
            return;
        }
    }

    private void EnsureDormsMap()
    {
        if (_map.TryGetMap(_dormsMapId, out _) || _dormsMapId != MapId.Nullspace)
            return;

        _dormsMapId = Transform(_map.CreateMap()).MapID;
    }

    private bool IsDormExists(int number)
    {
        var query = EntityQueryEnumerator<InfinityDormComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            if (comp.Number == number)
                return true;
        }

        return false;
    }

    private bool CheckUserDormsLimit(EntityUid uid)
    {
        var count = 0;
        var query = EntityQueryEnumerator<InfinityDormComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            if (comp.Creator == uid)
                count++;
        }

        return count < _maxUserDorms;
    }
}
