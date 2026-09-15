using System.Security.Cryptography;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>
///     AES-128-CBC/PKCS7 — the cipher GitLab's license encryption uses, matching OpenSSL's
///     <c>AES128</c> in <c>:CBC</c> mode (the .NET default padding is already PKCS7, byte-compatible
///     with OpenSSL).
/// </summary>
internal static class Aes128Cbc
{
    public static byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
    {
        using var aes = Create(key, iv);
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    public static byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
    {
        using var aes = Create(key, iv);
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private static Aes Create(byte[] key, byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }
}