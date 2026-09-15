using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace GitlabLicenseGenerator.Core.Crypto;

/// <summary>
///     Bridges BCL <see cref="RSA" /> keys (used for PEM I/O) to the BouncyCastle
///     key types needed for the raw PKCS#1 v1.5 type-1 RSA primitive.
///     <see cref="RSAParameters" /> byte arrays are already big-endian, matching
///     BouncyCastle's <see cref="BigInteger(int, byte[])" /> magnitude format.
/// </summary>
internal static class RsaKeyConversion
{
    public static RsaKeyParameters ToBouncyCastlePublicKey(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        return new RsaKeyParameters(
            false,
            new BigInteger(1, parameters.Modulus!),
            new BigInteger(1, parameters.Exponent!));
    }

    public static RsaPrivateCrtKeyParameters ToBouncyCastlePrivateKey(RSA rsa)
    {
        RSAParameters parameters;
        try
        {
            parameters = rsa.ExportParameters(true);
        }
        catch (CryptographicException ex)
        {
            throw new LicenseKeyException("Provided key is not a private key.", ex);
        }

        return new RsaPrivateCrtKeyParameters(
            new BigInteger(1, parameters.Modulus!),
            new BigInteger(1, parameters.Exponent!),
            new BigInteger(1, parameters.D!),
            new BigInteger(1, parameters.P!),
            new BigInteger(1, parameters.Q!),
            new BigInteger(1, parameters.DP!),
            new BigInteger(1, parameters.DQ!),
            new BigInteger(1, parameters.InverseQ!));
    }
}