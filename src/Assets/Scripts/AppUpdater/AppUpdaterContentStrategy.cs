﻿using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdaterContentStrategy : IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterContentStrategy));

        private readonly AppUpdaterContext _context;
        private bool _patchCalled;

        public AppUpdaterContentStrategy(AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _patchCalled, "Patch");

            DebugLogger.Log("Patching with content strategy.");

            var commandFactory = new Commands.AppUpdaterCommandFactory();

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Prepare(_context.StatusMonitor);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, _context);
            downloadContentPackage.Prepare(_context.StatusMonitor);

            var installContent = commandFactory.CreateInstallContentCommand(latestVersionId, _context);
            installContent.Prepare(_context.StatusMonitor);

            validateLicense.Execute(cancellationToken);

            uninstall.Execute(cancellationToken);

            downloadContentPackage.SetKeySecret(validateLicense.KeySecret);
            downloadContentPackage.Execute(cancellationToken);

            installContent.SetPackagePath(downloadContentPackage.PackagePath);
            installContent.Execute(cancellationToken);
        }
    }
}