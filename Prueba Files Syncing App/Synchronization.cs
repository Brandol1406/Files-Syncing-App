using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Synchronization
{
    /// <summary>
    /// Files synchronization class. This class can synchronize files of two repositories of files.
    /// </summary>
    public class FilesSync
    {
        bool success = true;
        /// <summary>
        /// Master repository path
        /// </summary>
        private string MasterRepository { get; set; }
        /// <summary>
        /// Backup repository path
        /// </summary>
        private string BackUpRepository { get; set; }
        public bool isMasterRepValid { get; }
        public bool isBackUpRepValid { get; }
        /// <summary>
        /// synchronization method to use
        /// </summary>
        public SyncMethod Method { get; set; }
        /// <summary>
        /// Master repository DirectoryInfo
        /// </summary>
        private DirectoryInfo diMasterRep;
        /// <summary>
        /// Backup repository DirectoryInfo
        /// </summary>
        private DirectoryInfo diBackUpRep;

        /// <summary>
        /// Method to execute before coping each file
        /// </summary>
        public Delegated beforeCopyEachFile;
        /// <summary>
        /// Method to execute after coping each file
        /// </summary>
        public Delegated afterCopyEachFile;
        /// <summary>
        /// Method to execute before deleting each file
        /// </summary>
        public Delegated beforeDeleteEachFile;
        /// <summary>
        /// Method to execute after deleting each file
        /// </summary>
        public Delegated afterDeleteEachFile;
        /// <summary>
        /// New FileSync Object
        /// </summary>
        /// <param name="MasterRepositoryPath">Master repository path</param>
        /// <param name="BackUpRepositoryPath">Backup repository path</param>
        public FilesSync(string MasterRepositoryPath, string BackUpRepositoryPath)
        {
            MasterRepository = MasterRepositoryPath;
            BackUpRepository = BackUpRepositoryPath;
            Method = SyncMethod.single;
            diMasterRep = new DirectoryInfo(MasterRepository);
            diBackUpRep = new DirectoryInfo(BackUpRepository);
            isMasterRepValid = diMasterRep.Exists;
            isBackUpRepValid = diBackUpRep.Exists;
        }
        /// <summary>
        /// New FileSync Object, Choosing synchronization method
        /// </summary>
        /// <param name="MasterRepositoryPath">Master repository path</param>
        /// <param name="BackUpRepositoryPath">Backup repository path</param>
        /// <param name="method">synchronization method</param>
        public FilesSync(string MasterRepositoryPath, string BackUpRepositoryPath, SyncMethod method)
        {
            MasterRepository = MasterRepositoryPath;
            BackUpRepository = BackUpRepositoryPath;
            Method = method;
            diMasterRep = new DirectoryInfo(MasterRepository);
            diBackUpRep = new DirectoryInfo(BackUpRepository);
            isMasterRepValid = diMasterRep.Exists;
            isBackUpRepValid = diBackUpRep.Exists;
        }
        /// <summary>
        /// Start synchronizing files
        /// </summary>
        /// <returns></returns>
        public SyncResult StartSync()
        {
            //Validating if directories exists
            if (!diMasterRep.Exists)
            {
                return new SyncResult {
                    success = false,
                    message = "MasterRepository does not exist!"
                };
            }
            if (!diBackUpRep.Exists)
            {
                return new SyncResult {
                    success = false,
                    message = "BackUpRepository does not exist!!"
                };
            }

            SFile[] filesToDelete = GetFilesToDelete();
            SFile[] filesToCopy = GetFilesToCopy();

            //Checking if there are enough free space on destination disk
            long totalFilesSize = filesToCopy.Sum(f => f.Size);
            if (!thereareEnoughSpace(totalFilesSize))
            {
                return new SyncResult() { success = false, message = "There are not enough free space on destination disk!" };
            }

            //Choosing method 
            if (Method == SyncMethod.single)
            {
                return SingleSync(filesToCopy);
            }
            else if (Method == SyncMethod.mirror)
            {
                return MirrorSync(filesToCopy, filesToDelete);
            }
            else
            {
                return new SyncResult { success = false, message = "Invalid Method!" };
            }
        }
        /// <summary>
        /// Get a collection of files that will be copied
        /// </summary>
        /// <returns>FileInfo Array</returns>
        public SFile[] GetFilesToCopy()
        {
            SFile[] filesToCopy = new SFile[0];

            if (!isBackUpRepValid || !isMasterRepValid)
            {
                return filesToCopy;
            }

            //Listing files contained in master repository
            FileInfo[] masterRepFiles = diMasterRep.GetFiles("*", SearchOption.AllDirectories);
            List<SFile> masterRepList = (from fn in masterRepFiles
                select new SFile()
                {
                    FullName = fn.FullName,
                    FullRelativeName = fn.FullName.Replace(diMasterRep.FullName, ""),
                    DirectoryName = fn.DirectoryName,
                    LastWriteTime = fn.LastWriteTime,
                    Size = fn.Length
                }).ToList();
            //Listing files contained in backup repository
            FileInfo[] backUpRepFiles = diBackUpRep.GetFiles("*", SearchOption.AllDirectories);
            List<SFile> backUpRepList = (from fn in backUpRepFiles
                select new SFile()
                {
                    FullName = fn.FullName,
                    FullRelativeName = fn.FullName.Replace(diBackUpRep.FullName, ""),
                    DirectoryName = fn.DirectoryName,
                    LastWriteTime = fn.LastWriteTime,
                    Size = fn.Length
                }).ToList();
            /*Listing files that will be copied: 
                First Condition - Files that are contained in master repository but not in backup repository
                OR
                Second condition - Files that are in both directories but files in master repository that has a bigger last write time 
            */
            filesToCopy = masterRepList.Where(
                // First condition
                f => !backUpRepList.Any(bf => bf.FullRelativeName == f.FullRelativeName)
                     ||
                     //Second condition
                     (
                         backUpRepList.Any(
                             bf => bf.FullRelativeName == f.FullRelativeName
                                   &&
                                   f.LastWriteTime > bf.LastWriteTime
                         )
                     )
            ).ToArray();

            return filesToCopy;
        }
        /// <summary>
        /// Get a collection of files that will be deleted
        /// </summary>
        /// <returns>FileInfo Array</returns>
        public SFile[] GetFilesToDelete()
        {
            SFile[] filesToDelete = new SFile[0];

            if (!isBackUpRepValid || !isMasterRepValid)
            {
                return filesToDelete;
            }

            //Listing files contained in master repository
            FileInfo[] masterRepFiles = diMasterRep.GetFiles("*", SearchOption.AllDirectories);
            List<SFile> masterRepList = (from fn in masterRepFiles
                select new SFile()
                {
                    FullName = fn.FullName,
                    FullRelativeName = fn.FullName.Replace(diMasterRep.FullName, ""),
                    DirectoryName = fn.DirectoryName,
                    LastWriteTime = fn.LastWriteTime,
                    Size = fn.Length
                }).ToList();
            //Listing files contained in backup repository
            FileInfo[] backUpRepFiles = diBackUpRep.GetFiles("*", SearchOption.AllDirectories);
            List<SFile> backUpRepList = (from fn in backUpRepFiles
                select new SFile()
                {
                    FullName = fn.FullName,
                    FullRelativeName = fn.FullName.Replace(diBackUpRep.FullName, ""),
                    DirectoryName = fn.DirectoryName,
                    LastWriteTime = fn.LastWriteTime,
                    Size = fn.Length
                }).ToList();
            /*Listing files that will be deleted: 
                Condition - Files that are contained in backup repository but not in master repository
            */
            filesToDelete = backUpRepList.Where(
                // First condition
                bf => !masterRepList.Any(f => f.FullRelativeName == bf.FullRelativeName)
                ).ToArray();
            return filesToDelete;
        }
        /// <summary>
        /// It just copy files from master repository to backup repository. This just copy files that are new or have a newer modification date.
        /// </summary>
        /// <returns>SyncResult</returns>
        private SyncResult SingleSync(SFile[] filesToCopy) 
        {
            string message = "";
            int total = filesToCopy.Length;
            int index = 0;

            //Coping files
            foreach (var file in filesToCopy)
            {
                index++;
                //Calling beforeCopyEachFile method
                if (beforeCopyEachFile != null)
                {
                    beforeCopyEachFile(file, index, total, true);
                }

                //Getting repositories names
                string bRepPath = diBackUpRep.FullName;
                string mRepPath = diMasterRep.FullName;
                //New path for new file
                string newFilePath = file.FullName.Replace(mRepPath, bRepPath);
                //New file directoryinfo
                DirectoryInfo newFileDi = new DirectoryInfo(file.DirectoryName.Replace(mRepPath, bRepPath));

                //If file's directory does not exists we create the file's directory and copy the file
                if (!newFileDi.Exists)
                {
                    newFileDi.Create();
                }

                bool copyed = true;
                try
                { File.Copy(file.FullName, newFilePath, true); }
                catch (Exception ex)
                {
                    message += $"Error copying { file.FullRelativeName }, reason: {ex.Message} \r\n";
                    copyed = false;
                    success = false;
                    index--;
                }

                //Calling afterCopyEachFile method
                if (afterCopyEachFile != null)
                {
                    afterCopyEachFile(file, index, total, copyed);
                }
            }

            return new SyncResult() { success = success, message = message += index.ToString() + " Files copied!" };
        }
        /// <summary>
        /// This maintain both repositories equal by coping new files to backup repository and deleting files from backup repository that are not in master repository.
        /// </summary>
        /// <returns>SyncResult</returns>
        private SyncResult MirrorSync(SFile[] filesToCopy, SFile[] filesToDelete)
        {
            int total = filesToDelete.Length;
            int index = 0;
            string message = "";
            string directory = diBackUpRep.FullName;

            //Performing a single sync
            SyncResult singleSyncRes = SingleSync(filesToCopy);

            foreach (var file in filesToDelete)
            {
                index++;
                //Calling beforeCopyEachFile method
                if (beforeDeleteEachFile != null)
                {
                    beforeDeleteEachFile(file, index, total, true);
                }

                //Deleting file
                bool deleted = true;
                try
                { File.Delete(file.FullName); }
                catch (Exception ex)
                {
                    message += $"Error deletting { file.FullRelativeName }, reason: {ex.Message} \r\n";
                    deleted = false;
                    success = false;
                    index--;
                }

                //Calling afterCopyEachFile method
                if (afterDeleteEachFile != null)
                {
                    afterDeleteEachFile(file, index, total, deleted);
                }
            }

            //Deleting empty directories
            DeleteEmptySubFolders(directory);

            return new SyncResult() {success = success, message = singleSyncRes.message + message + "\r\n" + index.ToString()  + " Files deleted!" };
        }
        /// <summary>
        /// Get's total free space from destination disk
        /// </summary>
        /// <param name="driveName">Name of destination drive</param>
        /// <returns></returns>
        private long GetTotalFreeSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.TotalFreeSpace;
                }
            }
            return -1;
        }
        /// <summary>
        /// Validates if there are enough space in destination disk
        /// </summary>
        /// <returns></returns>
        public bool thereareEnoughSpace(long totalFilesSize)
        {
            string drivename = diBackUpRep.Root.FullName;
            long totalFreeSpace = GetTotalFreeSpace(drivename);
            if (totalFreeSpace < (totalFilesSize - (1024 * 1024 * 10)))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Cleans from empty folders a directory
        /// </summary>
        /// <param name="startLocation">Directory to clean</param>
        private void DeleteEmptySubFolders(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteEmptySubFolders(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
    /// <summary>
    /// Sincrhonization method: Single or mirror
    /// </summary>
    public enum SyncMethod
    {
        single = 1,
        mirror = 2
    }
    /// <summary>
    /// Result of a synchronization.
    /// </summary>
    public class SyncResult
    {
        public bool success { get; set; }
        public string message { get; set; }
    }
    /// <summary>
    /// A file basic info for 
    /// </summary>
    public class SFile
    {
        public string FullName { get; set; }
        public string FullRelativeName { get; set; }
        public string DirectoryName { get; set; }
        public DateTime LastWriteTime { get; set; }
        public long Size { get; set; }
    }
    /// <summary>
    /// Delegated method to do something when the event happens
    /// </summary>
    /// <param name="file">Current file that is processed</param>
    /// <param name="index">Index of current File</param>
    /// <param name="total">Count of files</param>
    public delegate void Delegated (SFile file, int index, int total, bool success);
}
