using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler
{
    public interface IFileHandler
    {
        //internal FileInformation LocateFile(CAS cas, String path);
        //internal FileInformation DeleteFile(CAS cas, String path);
        //internal FileInformation GetFileInfo(CAS cas, String path);
        FileInformation[] ListDirectory(String path, bool includeDirectories);
        //internal byte[] GetBytesFromFile(CAS cas, String path, Int64 numberOfBytes);
        //internal bool TryReadRootFolder(CAS cas);
        //internal bool IsConnected(CAS cas);
        bool IsFileExclusive(String path);

        void CopyTo(String fromPath, String toPath);

        void MoveTo(String fromPath, String toPath);

        void DeleteFile(String path);
    }
}
