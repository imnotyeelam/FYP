
using System.Text.RegularExpressions;

public static class CodeValidator
{
    public static bool ValidateBaseCase(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return false;

        // Allow whitespace and an optional trailing colon
        string normalized = Regex.Replace(answer, @"\s+", "");

        if (normalized.EndsWith(":"))
            normalized = normalized.Substring(
                0, normalized.Length - 1
            );

        return normalized == "n==0" ||
               normalized == "0==n";
    }
}
