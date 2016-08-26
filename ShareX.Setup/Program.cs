﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2016 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace ShareX.Setup
{
    internal class Program
    {
        private enum SetupType
        {
            Stable, // Build setup & create portable zip file
            BuildSetup, // Build setup
            CreatePortable, // Create portable zip file
            PortableApps, // Create PortableApps folder
            Beta, // Build setup & upload it using "Debug/ShareX.exe"
            Steam, // Create Steam folder
            AppVeyor // -appveyor
        }

        private static SetupType Setup = SetupType.Stable;

        private static string ParentDir => Setup == SetupType.AppVeyor ? "" : @"..\..\..\";
        private static string BinDir => Path.Combine(ParentDir, "ShareX", "bin");
        private static string ReleaseDir => Path.Combine(BinDir, "Release");
        private static string DebugDir => Path.Combine(BinDir, "Debug");
        private static string SteamDir => Path.Combine(BinDir, "Steam");
        private static string ReleaseDirectory => Setup == SetupType.Steam ? SteamDir : ReleaseDir;
        private static string DebugPath => Path.Combine(DebugDir, "ShareX.exe");
        private static string InnoSetupDir => Path.Combine(ParentDir, @"ShareX.Setup\InnoSetup");
        private static string OutputDir => Path.Combine(InnoSetupDir, "Output");
        private static string PortableDir => Path.Combine(OutputDir, "ShareX-portable");
        private static string SteamOutputDir => Path.Combine(OutputDir, "ShareX");
        private static string PortableAppsDir => Path.Combine(ParentDir, @"..\PortableApps\ShareXPortable\App\ShareX");
        private static string SteamLauncherDir => Path.Combine(ParentDir, @"..\ShareX_Steam\ShareX_Steam\bin\Release");
        private static string SteamUpdatesDir => Path.Combine(SteamOutputDir, "Updates");
        private static string ChromeReleaseDir => Path.Combine(ParentDir, @"..\ShareX_Chrome\ShareX_Chrome\bin\Release");
        private static string InnoSetupCompilerPath => @"C:\Program Files (x86)\Inno Setup 5\ISCC.exe";
        private static string ZipPath => Setup == SetupType.AppVeyor ? "7z" : @"C:\Program Files\7-Zip\7z.exe";

        private static void Main(string[] args)
        {
            Console.WriteLine("ShareX.Setup started.");

            if (CheckArgs(args, "-appveyor"))
            {
                Setup = SetupType.AppVeyor;
            }

            Console.WriteLine("Setup type: " + Setup);

            switch (Setup)
            {
                case SetupType.Stable:
                    CompileSetup();
                    CreatePortable(PortableDir);
                    OpenOutputDirectory();
                    break;
                case SetupType.BuildSetup:
                    CompileSetup();
                    OpenOutputDirectory();
                    break;
                case SetupType.CreatePortable:
                    CreatePortable(PortableDir);
                    OpenOutputDirectory();
                    break;
                case SetupType.PortableApps:
                    CreatePortable(PortableAppsDir);
                    OpenOutputDirectory();
                    break;
                case SetupType.Beta:
                    CompileSetup();
                    UploadLatestFile();
                    break;
                case SetupType.Steam:
                    CreateSteamFolder();
                    OpenOutputDirectory();
                    break;
                case SetupType.AppVeyor:
                    CompileSetup();
                    CreatePortable(PortableDir);
                    break;
            }

            Console.WriteLine("ShareX.Setup successfully completed.");
        }

        private static bool CheckArgs(string[] args, string check)
        {
            if (!string.IsNullOrEmpty(check))
            {
                foreach (string arg in args)
                {
                    if (!string.IsNullOrEmpty(arg) && arg.Equals(check, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void OpenOutputDirectory()
        {
            Process.Start("explorer.exe", OutputDir);
        }

        private static void UploadLatestFile()
        {
            FileInfo fileInfo = new DirectoryInfo(OutputDir).GetFiles("*.exe").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (fileInfo != null)
            {
                Console.WriteLine("Uploading setup file.");
                Process.Start(DebugPath, fileInfo.FullName);
            }
        }

        private static void CompileSetup()
        {
            if (Setup == SetupType.AppVeyor && !File.Exists(InnoSetupCompilerPath))
            {
                InstallInnoSetup();
            }

            if (File.Exists(InnoSetupCompilerPath))
            {
                CompileISSFile("Recorder-devices-setup.iss");
                CompileISSFile("ShareX-setup.iss");
            }
            else
            {
                Console.WriteLine("InnoSetup compiler is missing: " + InnoSetupCompilerPath);
            }
        }

        private static void InstallInnoSetup()
        {
            Console.WriteLine("Downloading InnoSetup.");

            string innoSetupURL = "http://files.jrsoftware.org/is/5/innosetup-5.5.9-unicode.exe";
            string innoSetupFilename = "innosetup-5.5.9-unicode.exe";

            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile(innoSetupURL, innoSetupFilename);
            }

            Console.WriteLine("Installing InnoSetup.");

            Process.Start(innoSetupFilename, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-").WaitForExit();

            Console.WriteLine("InnoSetup installed.");
        }

        private static void CompileISSFile(string filename)
        {
            Console.WriteLine("Compiling setup file: " + filename);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(InnoSetupCompilerPath, $"\"{filename}\"");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetFullPath(InnoSetupDir);
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            Console.WriteLine("Setup file is created.");
        }

        private static void CreateSteamFolder()
        {
            if (Directory.Exists(SteamOutputDir))
            {
                Directory.Delete(SteamOutputDir, true);
            }

            Directory.CreateDirectory(SteamOutputDir);

            CopyFile(Path.Combine(SteamLauncherDir, "ShareX_Launcher.exe"), SteamOutputDir);
            CopyFile(Path.Combine(SteamLauncherDir, "steam_appid.txt"), SteamOutputDir);
            CopyFile(Path.Combine(SteamLauncherDir, "installscript.vdf"), SteamOutputDir);
            CopyFiles(SteamLauncherDir, "*.dll", SteamOutputDir);

            CreatePortable(SteamUpdatesDir);
        }

        private static void CreatePortable(string destination)
        {
            Console.WriteLine("Creating portable.");

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            Directory.CreateDirectory(destination);

            CopyFile(Path.Combine(ReleaseDirectory, "ShareX.exe"), destination);
            CopyFile(Path.Combine(ReleaseDirectory, "ShareX.exe.config"), destination);
            CopyFiles(ReleaseDirectory, "*.dll", destination);
            CopyFiles(Path.Combine(ParentDir, "Licenses"), "*.txt", Path.Combine(destination, "Licenses"));

            if (Setup != SetupType.AppVeyor)
            {
                CopyFile(Path.Combine(OutputDir, "Recorder-devices-setup.exe"), destination);
                CopyFile(Path.Combine(ChromeReleaseDir, "ShareX_Chrome.exe"), destination);
            }

            string[] languages = new string[] { "de", "es", "fr", "hu", "ko-KR", "nl-NL", "pt-BR", "ru", "tr", "vi-VN", "zh-CN" };

            foreach (string language in languages)
            {
                CopyFiles(Path.Combine(ReleaseDirectory, language), "*.resources.dll", Path.Combine(destination, "Languages", language));
            }

            if (Setup == SetupType.Steam)
            {
                // These git ignored
                CopyFile(Path.Combine(ParentDir, "Lib", "ffmpeg.exe"), destination);
                CopyFile(Path.Combine(ParentDir, "Lib", "ffmpeg-x64.exe"), destination);
            }
            else if (Setup == SetupType.PortableApps)
            {
                File.Create(Path.Combine(destination, "PortableApps")).Dispose();
            }
            else
            {
                File.Create(Path.Combine(destination, "Portable")).Dispose();

                //FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(releaseDir, "ShareX.exe"));
                //string zipFilename = string.Format("ShareX-{0}.{1}.{2}-portable.zip", versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart);
                string zipPath = Path.Combine(OutputDir, "ShareX-portable.zip");

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                Zip(destination + "\\*", zipPath);

                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }
            }

            Console.WriteLine("Portable created.");
        }

        private static void CopyFiles(string[] files, string toFolder)
        {
            if (!Directory.Exists(toFolder))
            {
                Directory.CreateDirectory(toFolder);
            }

            foreach (string filepath in files)
            {
                string filename = Path.GetFileName(filepath);
                string dest = Path.Combine(toFolder, filename);
                File.Copy(filepath, dest);
            }
        }

        private static void CopyFile(string path, string toFolder)
        {
            CopyFiles(new string[] { path }, toFolder);
        }

        private static void CopyFiles(string directory, string searchPattern, string toFolder)
        {
            CopyFiles(Directory.GetFiles(directory, searchPattern), toFolder);
        }

        private static void Zip(string source, string target)
        {
            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = ZipPath;
            p.Arguments = string.Format("a -tzip \"{0}\" \"{1}\" -r -mx=9", target, source);
            p.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = Process.Start(p);
            process.WaitForExit();
        }
    }
}