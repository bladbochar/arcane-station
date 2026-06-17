using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared._Arcane.SiliconStanding;

public sealed class SharedSiliconStandingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, ToggleSiliconRestingEvent>(OnToggleAction);
        SubscribeLocalEvent<BorgChassisComponent, UpdateCanMoveEvent>(OnCanMove);
    }

    private void OnToggleAction(Entity<BorgChassisComponent> ent, ref ToggleSiliconRestingEvent args)
    {
        if (!CanToggleResting(ent))
            return;

        SetResting(ent, !IsResting(ent));
        args.Handled = true;
    }

    private void OnCanMove(Entity<BorgChassisComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (IsResting(ent))
            args.Cancel();
    }

    public bool GetEffectiveResting(EntityUid uid) => IsResting(uid);

    public bool IsResting(EntityUid uid) => HasComp<SiliconRestingComponent>(uid);

    public bool CanToggleResting(EntityUid uid) =>
        HasComp<SiliconStandingComponent>(uid) &&
        HasComp<BorgChassisComponent>(uid);

    public void SetResting(EntityUid uid, bool resting)
    {
        if (resting)
            EnsureComp<SiliconRestingComponent>(uid);
        else
            RemComp<SiliconRestingComponent>(uid);
    }
}
