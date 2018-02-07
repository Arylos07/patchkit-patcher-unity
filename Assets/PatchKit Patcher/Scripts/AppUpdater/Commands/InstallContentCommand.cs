﻿using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    /*
     * TODO: Content installation is not registering directories because they are not listed in content summary.
     * The only solution would be to include directories in content summary. Waits for API update.
     */
    public class InstallContentCommand : BaseAppUpdaterCommand, IInstallContentCommand
    {
        private const string Suffix = "_"; // FIX: bug #714
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly string _packagePath;
        private readonly string _packageMetaPath;
        private readonly string _packagePassword;
        private readonly int _versionId;
        private readonly AppContentSummary _versionContentSummary;
        private readonly ILocalDirectory _localData;
        private readonly ILocalMetaData _localMetaData;

        private OperationStatus _copyFilesStatus;
        private OperationStatus _unarchivePackageStatus;
        private Pack1Meta _pack1Meta;

        public InstallContentCommand(string packagePath, string packageMetaPath, string packagePassword, int versionId,
            AppContentSummary versionContentSummary, ILocalDirectory localData, ILocalMetaData localMetaData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Validate the content summary.
            Checks.ArgumentNotNull(localData, "localData");
            Checks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(versionId, "versionId");

            _packagePath = packagePath;
            _packageMetaPath = packageMetaPath;
            _packagePassword = packagePassword;
            _versionId = versionId;
            _versionContentSummary = versionContentSummary;
            _localData = localData;
            _localMetaData = localMetaData;
        }

        public override void Prepare(UpdaterStatus status)
        {
            base.Prepare(status);

            Checks.ArgumentNotNull(status, "statusMonitor");

            DebugLogger.Log("Preparing content installation.");

            _localData.PrepareForWriting();

            _copyFilesStatus = new OperationStatus
            {
                Weight = {Value = StatusWeightHelper.GetCopyContentFilesWeight(_versionContentSummary)}
            };
            status.RegisterOperation(_copyFilesStatus);

            _unarchivePackageStatus = new OperationStatus
            {
                Weight = {Value = StatusWeightHelper.GetUnarchivePackageWeight(_versionContentSummary.Size)}
            };
            status.RegisterOperation(_unarchivePackageStatus);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            Checks.FileExists(_packagePath);
            Assert.IsTrue(_localMetaData.GetRegisteredEntries().Length == 0,
                "Cannot install content if previous version is still present.");

            if (_versionContentSummary.CompressionMethod == "pack1")
            {
                Assert.IsTrue(File.Exists(_packageMetaPath),
                    "Compression method is pack1, but meta file does not exist");

                DebugLogger.Log("Parsing package meta file");
                _pack1Meta = Pack1Meta.ParseFromFile(_packageMetaPath);
                DebugLogger.Log("Package meta file parsed succesfully");
            }

            DebugLogger.Log("Installing content.");
            
            using (var packageDir = new TemporaryDirectory(_packagePath + ".temp_unpack_" + Path.GetRandomFileName()))
            {
                DebugLogger.LogVariable(packageDir.Path, "packageDirPath");

                DebugLogger.Log("Unarchiving package.");

                string usedSuffix;
                IUnarchiver unarchiver = CreateUnrachiver(packageDir.Path, out usedSuffix);

                _unarchivePackageStatus.IsActive.Value = true;
                _unarchivePackageStatus.Description.Value = "Unarchiving package...";
                _unarchivePackageStatus.Progress.Value = 0.0;

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount, entryProgress) =>
                {
                    var entryMinProgress = Mathf.Max(0, entry - 1) / (double) amount; // entry could be zero
                    var entryMaxProgress = entry / (double) amount;

                    _unarchivePackageStatus.Progress.Value = entryMinProgress + (entryMaxProgress - entryMinProgress) * entryProgress;
                };

                unarchiver.Unarchive(cancellationToken);

                _unarchivePackageStatus.Progress.Value = 1.0;
                _unarchivePackageStatus.IsActive.Value = false;

                DebugLogger.Log("Copying files.");

                _copyFilesStatus.IsActive.Value = true;
                _copyFilesStatus.Description.Value = "Installing package...";
                _copyFilesStatus.Progress.Value = 0.0;

                for (int i = 0; i < _versionContentSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionContentSummary.Files[i].Path, packageDir.Path, usedSuffix);

                    _copyFilesStatus.Progress.Value = (i + 1) / (double) _versionContentSummary.Files.Length;
                }

                _copyFilesStatus.Progress.Value = 1.0;
                _copyFilesStatus.IsActive.Value = false;
            }
        }

        private IUnarchiver CreateUnrachiver(string destinationDir, out string usedSuffix)
        {
            switch (_versionContentSummary.CompressionMethod)
            {
                case "zip":
                    usedSuffix = string.Empty;
                    return new ZipUnarchiver(_packagePath, destinationDir, _packagePassword);
                case "pack1":
                    usedSuffix = Suffix;
                    return new Pack1Unarchiver(_packagePath, _pack1Meta, destinationDir, _packagePassword, Suffix);
                default:
                    throw new InstallerException(string.Format("Unknown compression method: {0}",
                        _versionContentSummary.CompressionMethod));
            }
        }

        private void InstallFile(string fileName, string packageDirPath, string suffix)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName+suffix));

            string sourceFilePath = Path.Combine(packageDirPath, fileName+suffix);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            string destinationFilePath = _localData.Path.PathCombine(fileName);
            DirectoryOperations.CreateParentDirectory(destinationFilePath);

            if (File.Exists(destinationFilePath))
            {
                DebugLogger.LogFormat("Destination file {0} already exists, removing it.", destinationFilePath);
                FileOperations.Delete(destinationFilePath);
            }
            
            FileOperations.Move(sourceFilePath, destinationFilePath);
            _localMetaData.RegisterEntry(fileName, _versionId);
        }
    }
}