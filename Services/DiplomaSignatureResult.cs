namespace DiplomaVerificationApp.Services;

public sealed record DiplomaSignatureResult(
    string IssuerId,
    byte[] IssuerIdBytes32,
    string Signature,
    string SignatureHash,
    byte[] SignatureHashBytes32,
    DateTimeOffset IssuedAtUtc);
