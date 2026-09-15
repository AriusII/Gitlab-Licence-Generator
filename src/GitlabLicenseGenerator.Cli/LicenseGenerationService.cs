using System.Security.Cryptography;
using GitlabLicenseGenerator.Core.Crypto;
using GitlabLicenseGenerator.Core.Licensing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitlabLicenseGenerator.Cli;

/// <summary>Generates the license once on startup, then stops the host; this tool has no long-running work.</summary>
internal sealed class LicenseGenerationService(
    IOptions<LicenseGenerationOptions> options,
    IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Environment.ExitCode = Generate(options.Value);
        lifetime.StopApplication();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static int Generate(LicenseGenerationOptions options)
    {
        if (options.RegenerateKeys)
        {
            DeleteIfExists(options.PublicKeyPath);
            DeleteIfExists(options.PrivateKeyPath);
        }

        if (!File.Exists(options.PublicKeyPath) || !File.Exists(options.PrivateKeyPath))
        {
            Console.WriteLine($"[*] no key pair found, generating a new RSA-{RsaKeyPairGenerator.KeySizeBits} pair...");
            CreateParentDirectory(options.PublicKeyPath);
            CreateParentDirectory(options.PrivateKeyPath);
            RsaKeyPairGenerator.GenerateAndSave(options.PublicKeyPath, options.PrivateKeyPath);
            Console.WriteLine($"[*] public key: {Path.GetFullPath(options.PublicKeyPath)}");
            Console.WriteLine($"[*] private key: {Path.GetFullPath(options.PrivateKeyPath)}");
        }
        else
        {
            Console.WriteLine($"[*] reusing existing key pair at {Path.GetFullPath(options.PublicKeyPath)}");
        }

        using var privateKey = RSA.Create();
        try
        {
            privateKey.ImportFromPem(File.ReadAllText(options.PrivateKeyPath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!] failed to load private key from {options.PrivateKeyPath}: {ex.Message}");
            return 1;
        }

        CopyPublicKeyToOutput(options);

        Console.WriteLine(
            $"[*] building a {options.Plan} license for {options.Name} <{options.Email}> ({options.Company})...");
        var license = LicenseFactory.Create(options);

        Console.WriteLine("[*] validating license...");
        var errors = LicenseValidator.Validate(license);
        if (errors.Count > 0)
        {
            Console.Error.WriteLine("[!] license validation failed:");
            foreach (var error in errors) Console.Error.WriteLine($"[!]   - {error}");

            return 1;
        }

        if (!string.IsNullOrEmpty(options.PlainLicensePath))
        {
            CreateParentDirectory(options.PlainLicensePath);
            File.WriteAllText(options.PlainLicensePath, LicenseCodec.ToJson(license, true));
            Console.WriteLine($"[*] wrote plaintext license to {Path.GetFullPath(options.PlainLicensePath)}");
        }

        CreateParentDirectory(options.OutputPath);
        var exported = LicenseCodec.Export(license, privateKey);
        File.WriteAllText(options.OutputPath, exported);
        Console.WriteLine($"[*] wrote license to {Path.GetFullPath(options.OutputPath)}");

        Console.WriteLine();
        Console.WriteLine("[*] done. To activate this license on your own GitLab EE instance:");
        Console.WriteLine(
            $"[*]   1. Replace GitLab's license encryption public key with {Path.GetFullPath(options.PublicKeyPath)}");
        Console.WriteLine("[*]      (in your docker-compose/container setup, mount or copy it to");
        Console.WriteLine("[*]      /opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub,");
        Console.WriteLine("[*]      then run `gitlab-ctl reconfigure && gitlab-ctl restart`.)");
        Console.WriteLine(
            $"[*]   2. In GitLab, go to Admin Area > Settings > General > \"Add License\" and upload {Path.GetFullPath(options.OutputPath)}");

        return 0;
    }

    private static void CreateParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    ///     Copies only the public key into the output directory. That is the sole piece of key material
    ///     GitLab itself ever consumes (installed at .license_encryption_key.pub, e.g. via a read-only
    ///     docker-compose mount) — the private key stays under <see cref="LicenseGenerationOptions.PrivateKeyPath" />
    ///     only, since GitLab never needs it and it has no reason to leave the keys directory.
    /// </summary>
    private static void CopyPublicKeyToOutput(LicenseGenerationOptions options)
    {
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
        if (string.IsNullOrEmpty(outputDirectory)) return;

        Directory.CreateDirectory(outputDirectory);

        var publicKeyDestination = Path.Combine(outputDirectory, Path.GetFileName(options.PublicKeyPath));
        if (string.Equals(Path.GetFullPath(options.PublicKeyPath), Path.GetFullPath(publicKeyDestination),
                StringComparison.OrdinalIgnoreCase))
            return;

        File.Copy(options.PublicKeyPath, publicKeyDestination, true);
        Console.WriteLine($"[*] copied public key into output directory: {publicKeyDestination}");
    }
}