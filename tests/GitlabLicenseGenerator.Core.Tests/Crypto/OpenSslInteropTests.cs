using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitlabLicenseGenerator.Core.Crypto;
using Xunit;

namespace GitlabLicenseGenerator.Core.Tests.Crypto;

/// <summary>
///     Independent oracle: encrypts with this code, then recovers the AES key and
///     plaintext using the literal `openssl` CLI — GitLab's license encryption format
///     is itself built on this exact OpenSSL primitive. Skips gracefully if `openssl`
///     isn't on PATH.
/// </summary>
public sealed class OpenSslInteropTests
{
    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }

    [Fact]
    public void Encrypt_WithCommittedPrivateKey_IsRecoverableByOpenSslCli()
    {
        if (!TryFindOpenSsl(out var opensslPath))
        {
            Assert.Skip("openssl CLI not found on PATH");
            return;
        }

        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(File.ReadAllText(FixturePath("committed-private.key")));

        var encryptor = new BouncyCastleLicenseEncryptor();
        const string plaintext = """{"openssl":"interop-check"}""";
        var envelopeBase64 = encryptor.Encrypt(plaintext, privateKey);

        var envelopeJson = Convert.FromBase64String(envelopeBase64);
        using var doc = JsonDocument.Parse(envelopeJson);
        var encryptedKey = Convert.FromBase64String(doc.RootElement.GetProperty("key").GetString()!);
        var iv = Convert.FromBase64String(doc.RootElement.GetProperty("iv").GetString()!);
        var ciphertext = Convert.FromBase64String(doc.RootElement.GetProperty("data").GetString()!);

        var tempDir = Directory.CreateTempSubdirectory("glgen-openssl-interop-").FullName;
        try
        {
            var publicKeyPath = Path.Combine(tempDir, "public.key");
            File.Copy(FixturePath("committed-public.key"), publicKeyPath);
            var encryptedKeyPath = Path.Combine(tempDir, "encrypted-key.bin");
            File.WriteAllBytes(encryptedKeyPath, encryptedKey);

            // `openssl rsautl -verify` performs modexp with the public key then strips PKCS#1 v1.5
            // block-type-1 padding — exactly OpenSSL's public_decrypt primitive that GitLab's
            // license encryption format is built on.
            var (exitCode, stdout, stderr) = RunOpenSsl(
                opensslPath,
                ["rsautl", "-verify", "-pubin", "-inkey", publicKeyPath, "-in", encryptedKeyPath]);
            Assert.True(exitCode == 0, $"openssl rsautl -verify failed: {stderr}");
            var recoveredAesKey = stdout;

            Assert.Equal(16, recoveredAesKey.Length);

            var ciphertextPath = Path.Combine(tempDir, "ciphertext.bin");
            File.WriteAllBytes(ciphertextPath, ciphertext);
            var keyHex = Convert.ToHexStringLower(recoveredAesKey);
            var ivHex = Convert.ToHexStringLower(iv);

            var (decExitCode, decStdout, decStderr) = RunOpenSsl(
                opensslPath,
                ["enc", "-d", "-aes-128-cbc", "-K", keyHex, "-iv", ivHex, "-in", ciphertextPath]);
            Assert.True(decExitCode == 0, $"openssl enc -d failed: {decStderr}");

            var recoveredPlaintext = Encoding.UTF8.GetString(decStdout);
            Assert.Equal(plaintext, recoveredPlaintext);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static bool TryFindOpenSsl(out string path)
    {
        try
        {
            var startInfo = new ProcessStartInfo("openssl", "version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000);
            if (process is { ExitCode: 0 })
            {
                path = "openssl";
                return true;
            }
        }
        catch
        {
            // fall through to not-found
        }

        path = string.Empty;
        return false;
    }

    private static (int ExitCode, byte[] Stdout, string Stderr) RunOpenSsl(string opensslPath, string[] args)
    {
        var startInfo = new ProcessStartInfo(opensslPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)!;
        using var stdoutStream = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(stdoutStream);
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdoutStream.ToArray(), stderr);
    }
}