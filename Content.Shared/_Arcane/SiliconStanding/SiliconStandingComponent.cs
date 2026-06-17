using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.SiliconStanding;

/// <summary>
/// Allows a silicon entity to transition between standing and resting states.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconStandingComponent : Component
{
    [DataField]
    public float LieDownDelay = 1.0f;

    [DataField]
    public float StandUpDelay = 0.5f;
}
