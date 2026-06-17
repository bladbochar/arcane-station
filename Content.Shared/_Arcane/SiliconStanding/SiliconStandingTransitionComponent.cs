using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Arcane.SiliconStanding;

/// <summary>
/// Temporary component attached to a silicon entity while a stand/rest transition DoAfter is in progress.
/// Removed when the DoAfter completes or is cancelled.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SiliconStandingTransitionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TargetResting;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan EndTime;
}
