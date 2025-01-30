using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace ARK.EditorTools.Image
{
    public class TextureFormatSettingWindow : OdinMenuEditorWindow
    {

        private const string SETTINGS_PATH   = "Assets/TextureInfoWindow/TextureImportSetting.asset";

        private TextureReferenceSetting referenceSettings;

        [MenuItem("ARK_Tools/TextureBatchConvert")]
        private static void OpenWindow()
        {
            var window = GetWindow<TextureFormatSettingWindow>();
            window.titleContent.text = "圖片轉換";
            window.Show();
        }


        private void OnDisable()
        {
            EditorUtility.UnloadUnusedAssetsImmediate(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            referenceSettings = AssetDatabase.LoadAssetAtPath(SETTINGS_PATH, typeof(TextureReferenceSetting)) as TextureReferenceSetting;
            if(referenceSettings == null)
            {
                CreateNewSettings();
            }
        }

        public void CreateNewSettings()
        {
            referenceSettings = CreateInstance<TextureReferenceSetting>();
            AssetDatabase.CreateAsset(referenceSettings, SETTINGS_PATH);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(supportsMultiSelect : true);

            // tree.AddAllAssetsAtPath("Battle場景數值設定", "Assets/Game/AddressableData/GameSettings", typeof(ScriptableObject), true)
            //    .AddThumbnailIcons();
            tree.AddAssetAtPath("圖片匯入設定", SETTINGS_PATH).AddThumbnailIcons();

            return tree;
        }

    }
}