using System;
using System.Collections.Generic;
using System.IO;

namespace WSyncer
{
    class Utils
    {
        static public void GetFiles(string dir, List<string> files)
        {
            string[] tempFiles = Directory.GetFiles(dir);
            string[] tempDirs = Directory.GetDirectories(dir);

            for (int i = 0; i < tempFiles.Length; i++)
                files.Add(tempFiles[i]);

            for (int i = 0; i < tempDirs.Length; i++)
                GetFiles(tempDirs[i], files);
        }

        // Pass in sorted lists only.
        // Elements in both lists have some differing prefix which should be ignored during compare.
        // For instance, an elements in list1 could be "C:/Data/file.dat" and in list2 "D:/Data/file.dat".
        // We want to skip at least 1 char during compare in this case.
        static public void SplitLists(string[] list1, string[] list2, int list1_prefixCharsCountToIngore, int list2_prefixCharsCountToIngore, out List<string> list1Only, out List<string> list2Only, out List<string> list1Common, out List<string> list2Common)
        {
            list1Only = new List<string>();
            list2Only = new List<string>();
            list1Common = new List<string>();
            list2Common = new List<string>();

            int list1Counter = 0;
            int list2Counter = 0;
            int elementsCount = list1.Length + list2.Length;

            for (int i = 0; i < elementsCount; i++)
            {
                if (list1Counter >= list1.Length && list2Counter >= list2.Length)
                    break;

                int order = 0;
                if (list1Counter >= list1.Length)
                {
                    order = 1;
                }
                else if (list2Counter >= list2.Length)
                {
                    order = -1;
                }
                else
                {
                    string element1 = list1[list1Counter];
                    string element2 = list2[list2Counter];
                    element1 = element1.Substring(list1_prefixCharsCountToIngore);
                    element2 = element2.Substring(list2_prefixCharsCountToIngore);
                    order = string.Compare(element1, element2);
                }

                if (order == 0)
                {
                    list1Common.Add(list1[list1Counter]);
                    list2Common.Add(list2[list2Counter]);
                    list1Counter++;
                    list2Counter++;
                }
                else if (order == -1)
                {
                    list1Only.Add(list1[list1Counter]);
                    list1Counter++;
                }
                else if (order == 1)
                {
                    list2Only.Add(list2[list2Counter]);
                    list2Counter++;
                }
            }
        }

        static public void ReconcileFiles(
            string srcDir, string dstDir, int srcDirLength, int dstDirLength,
            ref List<string> out_srcFilesOnly, ref List<string> out_dstFilesOnly, ref List<string> out_dstDirsOnly, ref List<string> out_srcCommonFiles)
        {
            string[] srcFiles = Directory.GetFiles(srcDir);
            string[] srcDirs = Directory.GetDirectories(srcDir);

            string[] dstFiles = Directory.GetFiles(dstDir);
            string[] dstDirs = Directory.GetDirectories(dstDir);

            // exclude system files from srcFiles
            {
                List<string> srcFiles_noSystem = new List<string>();

                for (int i = 0; i < srcFiles.Length; i++)
                {
                    FileInfo fileInfo = new FileInfo(srcFiles[i]);

                    if (!fileInfo.Attributes.HasFlag(FileAttributes.System))
                        srcFiles_noSystem.Add(srcFiles[i]);
                }

                srcFiles = srcFiles_noSystem.ToArray();
            }

            // exclude system dirs from srcDirs
            {
                List<string> srcDirs_noSystem = new List<string>();

                for (int i = 0; i < srcDirs.Length; i++)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(srcDirs[i]);

                    if (!dirInfo.Attributes.HasFlag(FileAttributes.System))
                        srcDirs_noSystem.Add(srcDirs[i]);
                }

                srcDirs = srcDirs_noSystem.ToArray();
            }

            //

            Array.Sort(srcFiles);
            Array.Sort(srcDirs);

            Array.Sort(dstFiles);
            Array.Sort(dstDirs);

            //

            List<string> srcFilesOnly, dstFilesOnly, srcCommonFiles, dstCommonFiles;
            SplitLists(srcFiles, dstFiles, srcDirLength, dstDirLength, out srcFilesOnly, out dstFilesOnly, out srcCommonFiles, out dstCommonFiles);

            List<string> srcDirsOnly, dstDirsOnly, srcCommonDirs, dstCommonDirs;
            SplitLists(srcDirs, dstDirs, srcDirLength, dstDirLength, out srcDirsOnly, out dstDirsOnly, out srcCommonDirs, out dstCommonDirs);

            for (int i = 0; i < srcDirsOnly.Count; i++)
                GetFiles(srcDirsOnly[i], srcFilesOnly);

            //

            for (int i = 0; i < srcFilesOnly.Count; i++)
                out_srcFilesOnly.Add(srcFilesOnly[i]);

            for (int i = 0; i < dstFilesOnly.Count; i++)
                out_dstFilesOnly.Add(dstFilesOnly[i]);

            for (int i = 0; i < dstDirsOnly.Count; i++)
                out_dstDirsOnly.Add(dstDirsOnly[i]);

            for (int i = 0; i < srcCommonFiles.Count; i++)
            {
                DateTime srcDateTime = File.GetLastWriteTime(srcCommonFiles[i]);
                DateTime dstDateTime = File.GetLastWriteTime(dstCommonFiles[i]);

                if (srcDateTime.Ticks != dstDateTime.Ticks)
                    out_srcCommonFiles.Add(srcCommonFiles[i]);
            }

            //

            for (int i = 0; i < srcCommonDirs.Count; i++)
                ReconcileFiles(srcCommonDirs[i], dstCommonDirs[i], srcDirLength, dstDirLength, ref out_srcFilesOnly, ref out_dstFilesOnly, ref out_dstDirsOnly, ref out_srcCommonFiles);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
                return;

            string srcDir = args[0];
            string dstDir = args[1];
            bool simulateOnly = (int.Parse(args[2]) == 0) ? false : true;

            if (!Directory.Exists(srcDir))
                return;
            if (!Directory.Exists(dstDir))
                return;

            DateTime timeBefore = DateTime.Now;

            List<string> srcFilesOnly = new List<string>();
            List<string> dstFilesOnly = new List<string>();
            List<string> dstDirsOnly = new List<string>();
            List<string> srcCommonFiles = new List<string>();

            Console.WriteLine("Reconcile Files");

            Utils.ReconcileFiles(srcDir, dstDir, srcDir.Length, dstDir.Length, ref srcFilesOnly, ref dstFilesOnly, ref dstDirsOnly, ref srcCommonFiles);

            Console.WriteLine("Copy Files: " + srcFilesOnly.Count);

            for (int i = 0; i < srcFilesOnly.Count; i++)
            {
                string srcPath = srcFilesOnly[i];
                string dstPath = dstDir + srcPath.Substring(srcDir.Length);

                if (!simulateOnly)
                {
                    // create directory if needed
                    string dstPathDir = Path.GetDirectoryName(dstPath);
                    if (!Directory.Exists(dstPathDir))
                        Directory.CreateDirectory(dstPathDir);

                    File.Copy(srcPath, dstPath, true);
                }

                int progress = (int)(100.0f * (i + 1) / srcFilesOnly.Count);
                Console.WriteLine("C " + srcPath + " (" + progress + "%)");
            }

            Console.WriteLine("Delete Files: " + dstFilesOnly.Count);

            for (int i = 0; i < dstFilesOnly.Count; i++)
            {
                if (!simulateOnly)
                    File.Delete(dstFilesOnly[i]);

                int progress = (int)(100.0f * (i + 1) / dstFilesOnly.Count);
                Console.WriteLine("D " + dstFilesOnly[i] + " (" + progress + "%)");
            }

            Console.WriteLine("Delete Dirs: " + dstDirsOnly.Count);

            for (int i = 0; i < dstDirsOnly.Count; i++)
            {
                if (!simulateOnly)
                    Directory.Delete(dstDirsOnly[i], true);

                int progress = (int)(100.0f * (i + 1) / dstDirsOnly.Count);
                Console.WriteLine("D " + dstDirsOnly[i] + " (" + progress + "%)");
            }

            Console.WriteLine("Replace Files: " + srcCommonFiles.Count);

            for (int i = 0; i < srcCommonFiles.Count; i++)
            {
                string srcPath = srcCommonFiles[i];
                string dstPath = dstDir + srcPath.Substring(srcDir.Length);

                if (!simulateOnly)
                    File.Copy(srcPath, dstPath, true);

                int progress = (int)(100.0f * (i + 1) / srcCommonFiles.Count);
                Console.WriteLine("R " + srcPath + " (" + progress + "%)");
            }

            DateTime timeAfter = DateTime.Now;
            int timeDiff = (int)timeAfter.Subtract(timeBefore).TotalSeconds;

            Console.WriteLine("Finish! Time: " + timeDiff + " s. Press Enter...");
            Console.ReadLine();
        }
    }
}
