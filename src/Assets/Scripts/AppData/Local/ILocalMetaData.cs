﻿namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ILocalMetaData
    {
        string[] GetFileNames();

        void AddOrUpdateFile(string fileName, int versionId);

        void RemoveFile(string fileName);

        bool FileExists(string fileName);

        int GetFileVersion(string fileName);
    }
}