namespace CleanPulse.Core.Services;

public sealed record RecycleBinInfo(long SizeBytes, long ItemCount);

public interface IShellRecycleBin
{
    RecycleBinInfo Query();
    bool Empty(out string? error);
}

public interface IDnsCacheFlusher
{
    bool Flush(out string? error);
}

public interface IPrivilegeService
{
    bool IsAdministrator();
    bool RestartAsAdministrator();
}
