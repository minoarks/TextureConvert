using System;
using Sirenix.OdinInspector;
using UnityEditor;

namespace ARK.EditorTools.Image
{
    [Serializable]
    public class PlatformData
    {

        // public string platform;
        [ReadOnly, LabelText("啟用")]
        public bool OverridePlatform;

        [ReadOnly, LabelText("解析度")]
        public int Size;
        [ReadOnly, LabelText("格式")]
        public string Format;
        [ReadOnly, LabelText("質量")]
        public string Quality;


        public PlatformData(TextureImporterPlatformSettings platformSettings)
        {
            
            Size                  = platformSettings.maxTextureSize;
            Format                = platformSettings.format.ToString();
            Quality               = platformSettings.compressionQuality.ToString();
            OverridePlatform      = platformSettings.overridden;
        }

 
    }
}