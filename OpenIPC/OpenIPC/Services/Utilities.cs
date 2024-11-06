using System.Text.RegularExpressions;

namespace OpenIPC.Services;

public static class Utilities
{
    public static string RemoveSpecialCharacters(string input)
    {
        string pattern = @"[^a-zA-Z0-9\-]"; // Matches any character that is not a letter, number, or dash
        string output = Regex.Replace(input, pattern, "");
        return output;
    }
}