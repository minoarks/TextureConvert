using UnityEditor;

namespace ARK.EditorTools.Image
{
    public class TextureSetting
    {

        public static void GetTextureSettings(string path, string platform, out int platformMaxTextureSize, out TextureImporterFormat platformTextureFmt)
        {
            var platformString             = platform;
            var platformCompressionQuality = 0;
            var platformAllowsAlphaSplit   = false;


            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.GetPlatformTextureSettings(platformString, out platformMaxTextureSize, out platformTextureFmt, out platformCompressionQuality, out platformAllowsAlphaSplit);
        }

        public static TextureImporter GetTextureImporter(string path)
        {
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }

        public static TextureImporterPlatformSettings GetTexturePlatformSettings(string path, string platform)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            return ti.GetPlatformTextureSettings(platform);
        }


    }
}