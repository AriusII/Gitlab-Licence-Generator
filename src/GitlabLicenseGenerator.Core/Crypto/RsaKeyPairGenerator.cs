using System.Security.Cryptography;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>Generates the RSA key pair GitLab uses to encrypt/decrypt license files.</summary>
public static class RsaKeyPairGenerator
{
    public const int KeySizeBits = 2048;

    /// <summary>Generates a fresh RSA-2048 key pair and writes it as PEM. Refuses to overwrite existing files.</summary>
    public static void GenerateAndSave(string publicKeyPath, string privateKeyPath)
    {
        if (File.Exists(publicKeyPath) || File.Exists(privateKeyPath))
            throw new InvalidOperationException("key pair already exists");

        using var rsa = RSA.Create(KeySizeBits);
        File.WriteAllText(privateKeyPath, rsa.ExportRSAPrivateKeyPem() + "\n");
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem() + "\n");
    }
}