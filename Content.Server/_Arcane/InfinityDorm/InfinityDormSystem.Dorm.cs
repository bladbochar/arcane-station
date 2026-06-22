using System.Numerics;
using Content.Shared._Arcane.InfinityDorm;
using Content.Shared.Chat;
using Robust.Shared.Map;

namespace Content.Server._Arcane.InfinityDorm;

public sealed partial class InfinityDormSystem
{
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

            EnsureComp<InfinityDormVisitorComponent>(uid);
            _transform.SetMapCoordinates(uid, targetCoords);
            return;
        }
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
