namespace DotnetTest.Mcp.Models;

public enum ErrorKind
{
    Unknown,
    ResultFileMissing,
    ReadFailed,
    TestHostCrashed,
    BuildFailed,
    DiscoveryFailed,
}