namespace NuGone.Cli.Shared.Constants;

/// <summary>
/// Standard exit codes for NuGone CLI commands.
/// Follows RFC-0001: CLI Architecture And Command Design.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int InvalidArgument = 1;
    public const int FileNotFound = 2;
    public const int DirectoryNotFound = 3;
    public const int AccessDenied = 4;
    public const int OperationFailed = 5;
    public const int UnexpectedError = 99;
}
