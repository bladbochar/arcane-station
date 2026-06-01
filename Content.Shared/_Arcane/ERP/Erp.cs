namespace Content.Shared._Arcane.ERP;

public enum ErpPreference : byte
{
    Yes,
    Ask,
    No,
}

public sealed class ErpPreferenceChangedEvent(ErpPreference oldPreference, ErpPreference newPreference) : EntityEventArgs
{
    public ErpPreference OldPreference = oldPreference;
    public ErpPreference NewPreference = newPreference;
}
