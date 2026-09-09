using System.Globalization;

namespace Lib_System.Extensions;

/// <summary>
/// The app only ever deals in USD. decimal.ToString("C") depends on
/// whatever culture the server happens to be running under, which is
/// exactly how a fine ended up labeled "10.00 EGP" while its sibling used
/// the system's default currency symbol - two different amounts, two
/// different implied currencies, same decimal field. Money is always
/// formatted through here instead, so it can never silently drift again.
/// </summary>
public static class MoneyExtensions
{
    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>e.g. 10 -> "$10.00"</summary>
    public static string ToUsd(this decimal amount) => amount.ToString("C", UsdCulture);
}
