using System;
using System.IO;

namespace MHLab.Patch.Core.IO
{
    public class LocalFileInfo
    {
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public DateTime LastWriting { get; set; }
        public FileAttributes Attributes { get; set; }

        public LocalFileInfo()
        {
        }

        public LocalFileInfo(LocalFileInfo source)
        {
            RelativePath = source.RelativePath;
            Size         = source.Size;
            LastWriting  = source.LastWriting;
            Attributes   = source.Attributes;
        }
    }
}
