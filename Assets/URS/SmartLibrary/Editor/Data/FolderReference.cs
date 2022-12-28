using System;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public enum FolderMatchOption { AnyDepth, TopOnly }

    [Serializable]
    public class FolderReference
    {
        [SerializeField] private FolderMatchOption _matchOption;
        //[SerializeField] private string _folderGuid = string.Empty;
        [SerializeField] private bool _doInclude = true;
        [SerializeField] private string  _path = "";
        public FolderMatchOption MatchOption
        {
            get { return _matchOption; }
            set { _matchOption = value; }
        }

       //public string FolderGuid
       //{
       //    get { return _folderGuid; }
       //    set { _folderGuid = value; }
       //}
        public void SetPath(string result)
        {
            _path = result;
        }

        public string Path
        {
            get { return _path; }
        }

        public bool DoInclude
        {
            get { return _doInclude; }
            set { _doInclude = value; }
        }

        public bool IsValidPath(string path)
        {
            var folderPath = Path;
            if (string.IsNullOrEmpty(folderPath))
                return false;

            if (path.Equals(folderPath, StringComparison.InvariantCultureIgnoreCase))
                return false;

            bool contains = path.Contains(folderPath);

            if (_matchOption == FolderMatchOption.TopOnly)
            {
                // Insure that the path isn't longer than the folder path while excluding the file name.
                contains &= path.LastIndexOf('/') + 1 == folderPath.Length;
            }

            return _doInclude ? contains : !contains;
        }
    }
}
