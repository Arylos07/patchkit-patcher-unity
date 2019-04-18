using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public static class PatcherPlayerPrefs
{
    [NotNull]
    private static string GetHashedAppSecret([NotNull] string appSecret)
    {
        using (var md5 = MD5.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(s: appSecret);
            return string.Join(
                separator: string.Empty,
                value: md5.ComputeHash(buffer: bytes)
                    .Select(selector: b => b.ToString(format: "x2"))
                    .ToArray());
        }
    }

    [NotNull]
    private static string GetFormattedKey([NotNull] string key)
    {
        string appSecret =
            Patcher.Instance.State.AppState?.Secret ?? "emptySecret";

        return $"{GetHashedAppSecret(appSecret: appSecret)}-{key}";
    }

    public static void SetString(
        [NotNull] string key,
        string value)
    {
        PlayerPrefs.SetString(
            key: GetFormattedKey(key: key),
            value: value);
    }

    public static string GetString([NotNull] string key)
    {
        return PlayerPrefs.GetString(
            key: GetFormattedKey(key: key),
            defaultValue: null);
    }
}