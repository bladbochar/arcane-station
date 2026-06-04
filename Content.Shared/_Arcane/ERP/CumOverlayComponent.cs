using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.ERP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CumOverlayComponent : Component
{
    /// <summary>
    /// How many times cum was applied. 1 = cum_normal, 2+ = cum_large.
    /// </summary>
    [AutoNetworkedField]
    public int Count;
}
