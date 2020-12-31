using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetTools
{
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
}
