using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace DiplomaVerificationApp.Services;

[Function("registerDiploma")]
public sealed class RegisterDiplomaFunction : FunctionMessage
{
    [Parameter("bytes32", "pdfHash", 1)]
    public byte[] PdfHash { get; init; } = [];
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
}

[Event("DiplomaRegistered")]
public sealed class DiplomaRegisteredEventDto : IEventDTO
{
    [Parameter("bytes32", "pdfHash", 1, true)]
    public byte[] PdfHash { get; set; } = [];

    [Parameter("uint256", "timestamp", 2, false)]
    public BigInteger Timestamp { get; set; }
}
