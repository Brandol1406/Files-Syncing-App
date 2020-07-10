using System;
using System.Collections.Generic;
using System.Linq;
using Synchronization;
using System.Xml;
using System.IO;

namespace Prueba_Files_Syncing_App
{
    class Program
    {
        static void Main(string[] args)
        {
            string MasterRepository = "";
            string BackUpRepository = "";
            SyncMethod Method = SyncMethod.single;

            if (File.Exists("Configs.xml"))
            {
                XmlReader xmlReader = XmlReader.Create("Configs.xml");
                while (xmlReader.Read())
                {
                    if ((xmlReader.NodeType == XmlNodeType.Element))
                    {
                        switch (xmlReader.Name)
                        {
                            case "MasterRepository":
                                if (xmlReader.HasAttributes)
                                    MasterRepository = xmlReader.GetAttribute("path");
                                break;
                            case "BackupRepository":
                                if (xmlReader.HasAttributes)
                                    BackUpRepository = xmlReader.GetAttribute("path");
                                break;
                            case "Method":
                                if (xmlReader.HasAttributes)
                                    switch (xmlReader.GetAttribute("method"))
                                    {
                                        case "1":
                                            Method = SyncMethod.single;
                                            break;
                                        case "2":
                                            Method = SyncMethod.mirror;
                                            break;

                                    }
                                break;
                        }
                    }
                }

                FilesSync filesSync = new FilesSync(MasterRepository, BackUpRepository, Method);

                filesSync.beforeCopyEachFile = (f, i, t, s) => Console.Write(f.FullName + " " + i.ToString() + " of " + t.ToString() + " (" + calculatePercent(i, t) + ")");
                filesSync.afterCopyEachFile = (f, i, t, s) => Console.WriteLine((s? " - Copied!": " - Error"));

                filesSync.beforeDeleteEachFile = (f, i, t, s) => Console.Write(f.FullName + " " + i.ToString() + " of " + t.ToString() + " (" + calculatePercent(i, t) + ")");
                filesSync.afterDeleteEachFile = (f, i, t, s) => Console.WriteLine((s ? " - Deleted!" : " - Error"));

                Console.WriteLine("Working:");

                SFile[] filesToCopy = filesSync.GetFilesToCopy();
                SFile[] filesToDelete = filesSync.GetFilesToDelete();

                string answer = "y";
                if (filesToCopy.Length > 0)
                {
                    Console.WriteLine(filesToCopy.Length + " Files will be copied!");
                }
                if (filesToDelete.Length > 0)
                {
                    Console.WriteLine(filesToDelete.Length + " Files will be deleted!");
                }

                if ((filesToCopy.Length + filesToDelete.Length) > 0)
                {
                    Console.WriteLine("Do you want to continue y/n");
                    answer = Console.ReadLine();
                }
                else if (!filesSync.isMasterRepValid)
                {
                    Console.WriteLine("Master repository is not valid!");
                    answer = "n";
                }
                else if (!filesSync.isBackUpRepValid)
                {
                    Console.WriteLine("Backup repository is not valid!");
                    answer = "n";
                }
                else
                {
                    Console.WriteLine("Backup repository is up to date!");
                    answer = "n";
                }

                if (answer.ToLower() == "y")
                {
                    SyncResult result = filesSync.StartSync();
                    Console.WriteLine((result.success ? "Success!" : "Error!"));
                    Console.WriteLine(result.message);
                }
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("*Configs.xml* Not found!");
                Console.ReadKey();
            }
        }

        static string calculatePercent(int i, int t)
        {
            double part = i;
            double total = t;
            double result = part / total;
            double percent = result * 100;
            return percent.ToString("N", new System.Globalization.CultureInfo("es-DO")) + "%";
        }
    }
}
