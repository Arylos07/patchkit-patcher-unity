using System.Threading;
using JetBrains.Annotations;
using PatchKit.Api.Models;

public class PatcherUpdateAppState
{
    public bool IsConnecting { get; set; }

    public long InstalledBytes { get; set; }

    public long TotalBytes { get; set; }

    public double Progress { get; set; }

    public double BytesPerSecond { get; set; }

    public CancellationTokenSource CancellationTokenSource { get; set; }

    public override string ToString()
    {
        return $"{{ \"IsConnecting\": {IsConnecting.ToString().ToLower()}, " +
            $"\"InstalledBytes\": {InstalledBytes}, " +
            $"\"TotalBytes\": {TotalBytes}, " +
            $"\"Progress\": {Progress}, " +
            $"\"BytesPerSecond\": {BytesPerSecond}, " +
            $"\"CancellationTokenSource\": {CancellationTokenSource?.ToString().SurroundWithQuotes() ?? "null"} }}";
    }
}

public enum PatcherAppLicenseKeyIssue
{
    None,
    Invalid,
    Blocked
}

public class PatcherAppState
{
    public PatcherAppState(
        [NotNull] string secret,
        [NotNull] string path,
        int? overrideLatestVersionId)
    {
        Secret = secret;
        Path = path;
        OverrideLatestVersionId = overrideLatestVersionId;

        UpdateState = new PatcherUpdateAppState();
    }

    [NotNull]
    public string Secret { get; }

    [NotNull]
    public string Path { get; }

    public int? OverrideLatestVersionId { get; }

    public string LicenseKey { get; set; }

    public PatcherAppLicenseKeyIssue LicenseKeyIssue { get; set; }

    public int? InstalledVersionId { get; set; }

    public int? LatestVersionId { get; set; }

    public App? Info { get; set; }

    public AppVersion[] Versions { get; set; }

    [NotNull]
    public PatcherUpdateAppState UpdateState { get; }

    public override string ToString()
    {
        return $"{{ \"Secret\": \"{Secret}\", " +
            $"\"Path\": \"{Path}\", " +
            $"\"OverrideLatestVersionId\": {OverrideLatestVersionId?.ToString() ?? "null"}, " +
            $"\"LicenseKey\": {LicenseKey?.SurroundWithQuotes() ?? "null"}, " +
            $"\"LicenseKeyIssue\": {LicenseKeyIssue.ToString().SurroundWithQuotes() ?? "null"}, " +
            $"\"InstalledVersionId\": {InstalledVersionId?.ToString() ?? "null"}, " +
            $"\"LatestVersionId\": {LatestVersionId?.ToString() ?? "null"}, " +
            $"\"Info\": {Info?.ToString().SurroundWithQuotes() ?? "null"}, " +
            $"\"Versions\": {Versions?.ToString().SurroundWithQuotes() ?? "null"}, " +
            $"\"UpdateState\": {UpdateState} }}";
    }
}

public enum PatcherStateKind
{
    Initializing,
    Idle,
    AskingForAppLicenseKey,
    UpdatingApp,
    StartingApp,
    DisplayingError,
    Quitting
}

public enum PatcherError
{
    NoLauncherError,
    MultipleInstancesError,
    OutOfDiskSpaceError,
    InternalError,
    UnauthorizedAccessError
}

public class PatcherState
{
    public PatcherState()
    {
    }

    public PatcherState(
        [NotNull] string appSecret,
        [NotNull] string appPath,
        int? overrideAppLatestVersionId,
        string lockFilePath)
    {
        AppState = new PatcherAppState(
            secret: appSecret,
            path: appPath,
            overrideLatestVersionId: overrideAppLatestVersionId);

        LockFilePath = lockFilePath;
    }

    public PatcherAppState AppState { get; }

    public string LockFilePath { get; }

    public LibPatchKitAppsFileLock FileLock { get; set; }

    public PatcherStateKind Kind { get; set; }

    public PatcherError? Error { get; set; }

    public bool IsOnline { get; set; }

    public bool HasChanged { get; set; }

    public override string ToString()
    {
        return $"{{ \"AppState\": {AppState?.ToString() ?? "null"}, " +
            $"\"LockFilePath\": {LockFilePath?.SurroundWithQuotes() ?? "null"}, " +
            $"\"FileLock\": {FileLock?.ToString().SurroundWithQuotes() ?? "null"}, " +
            $"\"Kind\": \"{Kind}\", " +
            $"\"Error\": {Error?.ToString().SurroundWithQuotes() ?? "null"}, " +
            $"\"IsOnline\": {IsOnline.ToString().ToLower()} }}";
    }
}