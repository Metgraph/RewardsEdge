﻿using System;
using System.Linq;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace RewardsEdge
{

    /**
     * <summary> Exception to raise in case the program can't find the selected Microsoft Edge progile. </summary>
     */
    class ProfileNotFound : Exception
    {
        public ProfileNotFound(string message) : base(message) { }
    }

    /**
     * <summary> Exception to raise in case the os is not supported. </summary>
     */
    class InvalidPlatform : Exception
    {
        public InvalidPlatform(string message) : base(message) { }
    }

    class EdgeManagment
    {
        /** <summary>Elaborates the given arguments. </summary>
         * <param name="args"> The array of arguments, it must have the same structure of the args passed in <see cref="Main(string[])">Main</see>. </param>
         */
        public static Tuple<string, string> Arguments(string[] args)
        {
            string edgeUser = "Default";
            string path = @".\";
            bool _w = true;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-w":
                        if (_w)
                        {
                            Console.WriteLine("Press a button to start the program");
                            Console.ReadKey();
                            _w = false;
                        }
                        break;
                    case "-p":
                        if (args.Length - 1 > i && args[i + 1][0] != '-')
                        {
                            edgeUser = args[++i];
                            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\Edge\\User Data" + edgeUser))
                            {
                                Console.WriteLine("The selected profile doesn't exist, insert a valid profile or leave it empty");
                                Console.ReadKey();
                                throw new ProfileNotFound("Profile " + edgeUser + " not found");
                            }
                        }
                        break;

                    case "-f":
                        if (args.Length - 1 > i && args[i + 1][0] != '-')
                        {
                            path = args[++i];
                        }
                        break;
                }

            }
            if (path.Last() != '\\')
                path += "\\";
            return Tuple.Create(edgeUser, path);
        }

        /**
         * <summary> Download the right driver version for Edge.</summary>
         * To get the current Edge version it is used the function <see cref="GetEdgeVersion">GetEdgeVersion</see>.
         * If in the folder there is a "edgedriver_win64.zip" file the program will ends, it is necessary to remove that file.
         * If there is already a "msedgedriver.exe" file and the program can't remove it the program will ends, it is necessary to remove that file.
         * <param name="path"> The path where download the driver</param>
         */
        public static void DownloadDriver(string path)
        {
            string version = GetEdgeVersion();
            string req = "https://msedgedriver.azureedge.net/" + version + "/edgedriver_" + GetOSArch() + ".zip";
            string zipPath = Path.GetFullPath(path + "edgedriver_win64.zip");
            string exePath = Path.GetFullPath(path + "msedgedriver.exe");


            if (File.Exists(zipPath))
            {
                Console.WriteLine(zipPath + " already exists, please remove it and run again the program");
                Application.Exit();
            }

            if (File.Exists(exePath))
            {
                Console.WriteLine("Removing old driver version" + exePath);
                try
                {
                    File.Delete(exePath);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Impossible to remove the driver, please remove it manually and retry");
                    return;
                }

            }

            Console.WriteLine("Downloading zip in " + zipPath);
            using (var client = new System.Net.WebClient())
            {
                client.DownloadFile(req, zipPath);
            }


            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                Console.WriteLine("Unzipping new driver");

                foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName == "msedgedriver.exe"))
                {
                    entry.ExtractToFile(exePath);
                }
            }
            Console.WriteLine("Removing the zip");
            File.Delete(zipPath);
            Console.WriteLine("Download completed");
        }


        /**
         * <summary> Get the os and architecture used, necessary to download the correct driver. </summary>
         * <returns> The driver platform.</returns>
         */
        private static string GetOSArch()
        {
            string toRet;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                switch (arch)
                {
                    case "x86":
                        toRet = "win32";
                        break;

                    case "AMD64":
                        toRet = "win64";
                        break;

                    case "ARM64":
                        toRet = "arm64";
                        break;

                    default:
                        throw new InvalidPlatform("This Windows version is not supported");

                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                toRet = "mac64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // TODO test if it works and if return "linux64" if the architecture is arm64
                if (Environment.Is64BitOperatingSystem)
                {
                    toRet = "linux64";
                }
                else
                {
                    throw new InvalidPlatform("This Linux version is not supported");
                }

            }
            else
            {
                throw new InvalidPlatform("Platform not recognized");
            }

            return toRet;
        }


        /**
         * <summary> Gets the Edge version.</summary>
         * It uses powershell.exe to get the edge version.
         * <returns> Edge version.</returns>
         */
        private static string GetEdgeVersion()
        {
            // get version using powershell
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "(Get-AppxPackage -Name \"Microsoft.MicrosoftEdge.Stable\").Version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };
            proc.Start();
            // get return value
            string ret = proc.StandardOutput.ReadToEnd();
            // remove \n\r
            return ret.Substring(0, ret.Length - 2);
        }
    }
}