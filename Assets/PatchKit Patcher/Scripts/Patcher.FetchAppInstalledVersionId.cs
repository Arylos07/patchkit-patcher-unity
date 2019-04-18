using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppInstalledVersionId()
    {
        Assert.IsNotNull(value: State.AppState);

        // ReSharper disable once PossibleNullReferenceException
        int? installedVersionId =
            await LibPatchKitApps.GetAppInstalledVersionIdAsync(
                path: State.AppState.Path,
                cancellationToken: CancellationToken.None);

        if (installedVersionId <= 0)
        {
            installedVersionId = null;
        }

        ModifyState(
            x: () => State.AppState.InstalledVersionId = installedVersionId);
    }
}