using System.Globalization;
using System.Text;

namespace FinanceApp.ViewModels;

/// <summary>
/// Tells whether a short description names part of a longer one, ignoring case, accents and linking words: every
/// word of <c>part</c> is a word of <c>whole</c> ("Seguro" in "Seguro do carro", "Plano saúde" in "Plano de saúde").
/// Whole words are compared, so "Net" isn't in "Internet", and only one way, so "Celular novo" (a new phone) isn't
/// in "Celular" (the phone bill).
/// </summary>
public static class DescriptionMatcher
{
    private static readonly HashSet<string> LinkingWords = ["a", "o", "as", "os", "e", "de", "da", "do", "das", "dos", "em", "no", "na"];

    public static bool IsPartOf(string part, string whole)
    {
        var partWords = Words(part);
        return partWords.Count > 0 && partWords.IsSubsetOf(Words(whole));
    }

    private static HashSet<string> Words(string text)
    {
        var withoutAccents = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                withoutAccents.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');

        return [.. withoutAccents.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !LinkingWords.Contains(w))];
    }
}
