using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace ARK.EditorTools.Image
{
    [Serializable, Searchable]
    public class TextureInfo
    {

        [TableColumnWidth(100, false), PreviewField(100f, ObjectFieldAlignment.Center), Searchable]
        public Texture2D Texture2D;

        public string TexturePath { get; private set; }

        [TableColumnWidth(100), ReadOnly]
        public string Name;

        [TableColumnWidth(150, false)]
        public PlatformData Android;
        [TableColumnWidth(150, false)]
        public PlatformData IOS;
        [TableColumnWidth(150, false)]
        public PlatformData Default;


        public Texture2D GetTexture()
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        public TextureInfo(string path)
        {
            TexturePath = path;
            Android     = new PlatformData(TextureSetting.GetTexturePlatformSettings(path, "Android"));
            IOS         = new PlatformData(TextureSetting.GetTexturePlatformSettings(path, "IOS"));
            Default     = new PlatformData(TextureSetting.GetTexturePlatformSettings(path, "Default"));
            Texture2D   = GetTexture();

        }

    }
}