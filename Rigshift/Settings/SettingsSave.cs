using Rigshift.Config;

namespace Rigshift.Settings;

/// <summary>
/// The outcome of one settings save: either the written config with the
/// reconciled session intent, or the reason the save did not produce a file.
/// </summary>
public sealed record SettingsSaveResult(
    bool Saved,
    RigshiftConfig? Config,
    IReadOnlyList<string> Enabled,
    string? LastEnabled,
    string? Error);

/// <summary>
/// Validate a draft, write it to the per-user path, and reconcile the session
/// intent against the saved config. A failure returns a result and never lets
/// the exception escape, and a failed save never passes a config back or
/// changes the input enabled list.
/// </summary>
public static class SettingsSave
{
    public static SettingsSaveResult TrySave(
        SettingsDraft draft,
        string userPath,
        IReadOnlyList<string> enabled,
        string? lastEnabled)
    {
        var errors = SettingsValidator.Validate(draft);
        if (errors.Count > 0)
        {
            return new SettingsSaveResult(
                false,
                null,
                new List<string>(enabled),
                lastEnabled,
                string.Join(Environment.NewLine, errors));
        }

        try
        {
            var config = SettingsMapper.ToConfig(draft);
            ConfigFileStore.Save(userPath, config);
            var intent = IntentReconciler.Reconcile(enabled, lastEnabled, config);
            return new SettingsSaveResult(true, config, intent.Enabled, intent.LastEnabled, null);
        }
        catch (Exception ex)
        {
            return new SettingsSaveResult(false, null, new List<string>(enabled), lastEnabled, ex.Message);
        }
    }
}
