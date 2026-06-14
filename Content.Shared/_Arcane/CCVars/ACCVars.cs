using Robust.Shared.Configuration;

namespace Content.Shared._Arcane.CCVars;

[CVarDefs]
public sealed partial class ACCVars
{
    /// <summary>
    /// Должен ли клиент использовать ТТС вместо барков.
    /// </summary>
    public static readonly CVarDef<bool> UseTTS =
        CVarDef.Create("tts.use_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// На каком расстоянии от игрока NPC будет замораживаться.
    /// </summary>
    public static readonly CVarDef<int> NpcSleepRange =
        CVarDef.Create("npc.sleep_range", 30, CVar.SERVERONLY);
}
