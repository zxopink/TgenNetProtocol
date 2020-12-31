using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TgenNetTools
{
    public static class Tools
    {
        /*
        [Serializable]
        public struct FileData
        {
            public string name;
            public byte[] data;
        }
        [Serializable]
        public class FolderData
        {
            public string name;
            public List<FileData> files = new List<FileData>();
            public List<FolderData> folders = new List<FolderData>();
        }
        public enum FileType
        {
            File,
            Directory
        }
        [Serializable]
        public class NetworkFile
        {
            public FileType fileType;
            public string name;

            public List<NetworkFile> files;
            //public List<NetworkFile> Files { get { return files; } set => FillData(value); }

            public byte[] data;
            //public byte[] Data { get { return data; } set => FillData(value); }
            public NetworkFile(string name, FileType fileType = FileType.File)
            {
                this.name = name;
                this.fileType = fileType;

                if (fileType == FileType.Directory)
                    files = new List<NetworkFile>();
            }

            public void FillData(List<NetworkFile> files)
            {
                if (fileType != FileType.Directory)
                    throw new Exception("This file is of type " + fileType);

                this.files.AddRange(files);
            }
            public void FillData(NetworkFile file)
            {
                if (fileType != FileType.Directory)
                    throw new Exception("This file is of type " + fileType);
                files.Add(file);
            }

            public void FillData(byte[] data)
            {
                if (fileType != FileType.File)
                    throw new Exception("This file is of type " + fileType);

                this.data = data;
            }
        }
        */
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
                TgenLog.Log(e.ToString());
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
        /*
        private static bool IsDirectory(string path)
        {
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(path);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                MessageBox.Show("Its a directory");
            else
                MessageBox.Show("Its a file");
        }
        */

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
                TgenLog.Log(e.ToString());
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
