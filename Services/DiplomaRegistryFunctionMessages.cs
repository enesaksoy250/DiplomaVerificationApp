using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace DiplomaVerificationApp.Services;

[Function("registerDiploma")]
public sealed class RegisterDiplomaFunction : FunctionMessage
{
    [Parameter("bytes32", "pdfHash", 1)]
    public byte[] PdfHash { get; init; } = [];

    [Parameter("bytes32", "issuerId", 2)]
    public byte[] IssuerId { get; init; } = [];

    [Parameter("bytes32", "signatureHash", 3)]
    public byte[] SignatureHash { get; init; } = [];
}

[Function("verifyDiploma", typeof(VerifyDiplomaOutput))]
public sealed class VerifyDiplomaFunction : FunctionMessage
{
    [Parameter("bytes32", "pdfHash", 1)]
    public byte[] PdfHash { get; init; } = [];
}

[FunctionOutput]
public sealed class VerifyDiplomaOutput : IFunctionOutputDTO
{
    [Parameter("bool", "exists", 1)]
    public bool Exists { get; set; }

    [Parameter("uint256", "timestamp", 2)]
    public BigInteger Timestamp { get; set; }

    [Parameter("bytes32", "issuerId", 3)]
    public byte[] IssuerId { get; set; } = [];

    [Parameter("bytes32", "signatureHash", 4)]
    public byte[] SignatureHash { get; set; } = [];

    [Parameter("address", "registeredBy", 5)]
    public string RegisteredBy { get; set; } = string.Empty;
}

[Event("DiplomaRegistered")]
public sealed class DiplomaRegisteredEventDto : IEventDTO
{
    [Parameter("bytes32", "pdfHash", 1, true)]
    public byte[] PdfHash { get; set; } = [];

    [Parameter("bytes32", "issuerId", 2, true)]
    public byte[] IssuerId { get; set; } = [];

    [Parameter("bytes32", "signatureHash", 3, false)]
    public byte[] SignatureHash { get; set; } = [];

    [Parameter("address", "registeredBy", 4, true)]
    public string RegisteredBy { get; set; } = string.Empty;

    [Parameter("uint256", "timestamp", 5, false)]
    public BigInteger Timestamp { get; set; }
}
