using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ARK.EditorTools.Image
{
    public class TextureFolderData : ScriptableObject
    {

        [FolderPath]
        public List<string> FolderPath;

        public bool IsIncludeChild = false;

        public void AddPath(string path)
        {
            if(FolderPath == null)
                FolderPath = new List<string>();
            
            if(FolderPath.Contains(path))
                return;
            
            FolderPath.Add(path);
        }

        public void RemotePath()
        {
            
        }

    }
}