using System.Collections.Generic;
using ARK.EditorTools.CustomAttribute;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ARK.EditorTools.Image
{
    public class FolderContent
    {

        [TableListSelector("Click"), PropertyOrder(1), ListDrawerSettings(Expanded = true)]
        public List<TextureInfo> TextureDatas;

        private List<int> textureIndexList = new List<int>();

        public FolderContent(List<TextureInfo> textureData)
        {
            TextureDatas = textureData;
        }


        public void Click(List<int> index)
        {
            var objs = new List<Texture2D>();

            textureIndexList.Clear();
            foreach(var id in index)
            {
                objs.Add(TextureDatas[id].GetTexture());
                textureIndexList.Add(id);
            }

            Selection.objects = objs.ToArray();
        }

    }
}