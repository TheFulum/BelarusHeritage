namespace BelarusHeritage;

/// <summary>
/// UI feature toggles. Backend routes may remain enabled when false.
/// </summary>
public static class FeatureFlags
{
    /// <summary>Password reset via email (SMTP not configured yet).</summary>
    public const bool PasswordRecovery = false;
}
