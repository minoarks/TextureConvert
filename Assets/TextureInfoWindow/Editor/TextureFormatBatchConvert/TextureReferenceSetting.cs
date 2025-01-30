using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ARK.EditorTools.Image
{
    public class TextureReferenceSetting : ScriptableObject
    {

        [Button("更新所有目標圖片")]
        public void UpdateGroup()
        {
            foreach(var group in groupPair)
            {
                group.ChangeTextureSetting();
            }
        }

        [LabelText("目標列表")]
        public List<GroupAsset> groupPair;

    }

    [Serializable]
    public class GroupAsset
    {

        [LabelText("備註")]
        [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
        public string memo;

        public string[] ToPaths;


        [LabelText("子資料夾")]
        public bool subFolder;

        [HorizontalGroup("按鈕"), LabelWidth(150), LabelText("參照圖片路徑")]
        public string textureSettingPath;

        [FormerlySerializedAs("REF_TextureData"), LabelText("參照圖片資訊"), HorizontalGroup("texture")]
        public TextureInfo refTextureInfo;

        [Button("更新參考圖片", ButtonSizes.Small), HorizontalGroup("按鈕")]
        public void CreateTextureData()
        {
            refTextureInfo = new TextureInfo(textureSettingPath);
        }

        public void ChangeTextureSetting()
        {
            var ref_Android = TextureSetting.GetTexturePlatformSettings(textureSettingPath, "Android");
            var ref_IOS     = TextureSetting.GetTexturePlatformSettings(textureSettingPath, "IOS");
            var ref_Default = TextureSetting.GetTexturePlatformSettings(textureSettingPath, "Default");

            TextureImporter         ref_textureImporter     = (TextureImporter)TextureImporter.GetAtPath(textureSettingPath);
            TextureImporterSettings ref_texImporterSettings = new TextureImporterSettings();
            ref_textureImporter.ReadTextureSettings(ref_texImporterSettings);
            var searchOption = subFolder ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach(var path in ToPaths)
            {
                var searchFiles = Directory.GetFiles(path, "*.*", searchOption)
                   .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToList();


                foreach(var filePath in searchFiles)
                {
                    var Android = TextureSetting.GetTexturePlatformSettings(filePath, "Android");
                    var IOS     = TextureSetting.GetTexturePlatformSettings(filePath, "IOS");
                    var Default = TextureSetting.GetTexturePlatformSettings(filePath, "Default");

                    TextureImporter         textureImporter     = (TextureImporter)TextureImporter.GetAtPath(filePath);
                    TextureImporterSettings texImporterSettings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(texImporterSettings);
                    var diff =
                        Android.overridden         != ref_Android.overridden         ||
                        Android.format             != ref_Android.format             ||
                        Android.compressionQuality != ref_Android.compressionQuality ||
                        IOS.overridden             != ref_IOS.overridden             ||
                        IOS.format                 != ref_IOS.format                 ||
                        IOS.compressionQuality     != ref_IOS.compressionQuality     ||
                        Default.overridden         != ref_Default.overridden         ||
                        Default.format             != ref_Default.format             ||
                        Default.compressionQuality != ref_Default.compressionQuality;

                    if(!diff)
                    {
                        Debug.LogError($"<color=#33FF49>not change file : {filePath}</color>");
                        continue;
                    }
                    else
                    {
                        Debug.Log($"<color=#33FF49>change file : {filePath}</color>");
                    }

                    Android.format             = ref_Android.format;
                    Android.overridden         = ref_Android.overridden;
                    Android.compressionQuality = ref_Android.compressionQuality;

                    IOS.format             = ref_IOS.format;
                    IOS.overridden         = ref_IOS.overridden;
                    IOS.compressionQuality = ref_IOS.compressionQuality;

                    Default.format             = ref_Default.format;
                    Default.overridden         = ref_Default.overridden;
                    Default.compressionQuality = ref_Default.compressionQuality;

                    textureImporter.SetPlatformTextureSettings(Android);
                    textureImporter.SetPlatformTextureSettings(IOS);
                    textureImporter.SetPlatformTextureSettings(Default);

                    textureImporter.SaveAndReimport();


                    bool ChangeAndroid()
                    {
                        return Android.format != ref_Android.format;
                    }
                }
            }
        }

    }
}