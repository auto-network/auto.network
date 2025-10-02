using System.Text.RegularExpressions;

namespace AutoWeb.Helpers;

public static class ValidationHelper
{
    // RFC 5322 compliant email regex (simplified version)
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates if the provided string is a valid email address
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Check length constraints
        if (email.Length > 254) // Max email length per RFC
            return false;

        try
        {
            return EmailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Minimum 8 characters for now
        // Could be enhanced with complexity requirements
        return password.Length >= 8;
    }

    /// <summary>
    /// Gets a user-friendly validation message for email
    /// </summary>
    public static string? GetEmailValidationMessage(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required";

        if (!IsValidEmail(email))
            return "Please enter a valid email address";

        return null; // Valid
    }

    /// <summary>
    /// Gets a user-friendly validation message for password
    /// </summary>
    public static string? GetPasswordValidationMessage(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required";

        if (password.Length < 8)
            return "Password must be at least 8 characters";

        return null; // Valid
    }
}