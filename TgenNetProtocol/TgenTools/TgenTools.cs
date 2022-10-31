using System;
using System.IO;
using System.Linq;
using TgenNetProtocol;

namespace TgenNetTools
{
    public static class Tools
    {
        public static NetworkFile PackFile(string path)
        {
            if (IsDirectory(path)) //Path is a folder
            {
                Console.WriteLine("Folder: " + Path.GetFileName(path));
                return PackFolder(path);
            }
            else //Path is a file
            {
                Console.WriteLine("File: " + Path.GetFileName(path));
                return PackFileData(path);
            }
        }

        private static NetworkFile PackFolder(string path)
        {
            try
            {
                NetworkFile folderData = new NetworkFile(Path.GetFileName(path), FileType.Directory);

                string[] files = Directory.GetFiles(path).Where(name => !name.EndsWith(".ini")).ToArray(); //exclude .ini files (these are invisible  system files) 
                foreach (var file in files)
                    folderData.FillData(PackFile(file));

                string[] folders = Directory.GetDirectories(path);
                foreach (var folder in folders)
                    folderData.FillData(PackFile(folder));

                return folderData;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static NetworkFile PackFileData(string path)
        {
            NetworkFile file = new NetworkFile(Path.GetFileName(path), FileType.File);
            file.FillData(File.ReadAllBytes(path));
            return file;
        }

        private static bool IsDirectory(string path) => (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;

        public static void UnpackFile(string header, NetworkFile file)
        {
            try
            {
                if (file.fileType == FileType.Directory) //Path is a folder
                {
                    UnpackFolder(header, file);
                    return;
                }
                else //Path is a file
                {
                    UnpackFileData(header, file);
                    return;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static void UnpackFolder(string header, NetworkFile file)
        {
            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(header, file.name));
            foreach (var fileData in file.files)
            {
                UnpackFile(directory.FullName, fileData);
            }
        }

        private static void UnpackFileData(string header, NetworkFile file)
        {
            FileStream stream = File.Create(Path.Combine(header, file.name));
            stream.Write(file.data, 0, file.data.Length);
        }
    }
}
