namespace DiplomaVerificationApp.Services;

public sealed class DuplicateDiplomaException(string message) : Exception(message);
