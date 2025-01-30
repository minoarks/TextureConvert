using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace ARK.EditorTools.Image
{
    public class TextureCompressEditorWindow : OdinMenuEditorWindow
    {

        private const string SETTINGS_PATH   = "Assets/TextureInfoWindow/FolderSettings.asset";
        private       string targetDirectory = "Assets";

        private TextureFolderData folderSettings;


        [MenuItem("ARK_Tools/TextureViewer")]
        private static void OpenWindow()
        {
            var window = GetWindow<TextureCompressEditorWindow>();
            window.titleContent.text = "圖片檢視";
            window.Show();
        }


        protected override void OnImGUI()
        {
            SirenixEditorGUI.Title("圖片檢視器", "單純看雙平台指定路徑底下圖片的格式", TextAlignment.Center, true);
            EditorGUILayout.Space();

            SirenixEditorGUI.InfoMessageBox("Android從RGB(A) Compressed ASTC 5X5 block開始調整，往下會變比較清晰往後會比較模糊");
            EditorGUILayout.BeginHorizontal();

            folderSettings.IsIncludeChild = EditorGUILayout.Toggle("包含次資料夾內圖片", folderSettings.IsIncludeChild); 
            if(GUILayout.Button("加入資料夾", GUILayout.Width(100)))
            {
                SelectDirectory();
                ForceMenuTreeRebuild();
            }

            if(GUILayout.Button("存檔", GUILayout.Width(100)))
            {
                EditorUtility.SetDirty(folderSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if(GUILayout.Button("更新頁面", GUILayout.Width(100)))
            {
                ForceMenuTreeRebuild();
            }
            GUILayout.FlexibleSpace();
            folderSettings = EditorGUILayout.ObjectField(folderSettings, typeof(TextureFolderData), false, GUILayout.MaxWidth(300)) as TextureFolderData;

            EditorGUILayout.EndHorizontal();
            base.OnImGUI();
        }


        protected override OdinMenuTree BuildMenuTree()
        {
            var config = new OdinMenuTreeDrawingConfig()
            {
                DrawSearchToolbar = true
            };

            var tree = new OdinMenuTree(false, config);

            if(folderSettings != null)
                tree.Add("搜尋的資料夾", folderSettings);
            if(folderSettings != null && folderSettings.FolderPath != null)
            {
                foreach(var path in folderSettings.FolderPath)
                {
                    if(Directory.Exists(path))
                    {
                        var filterTexture = GetFilterTexture(path);
                        foreach(var textureList in filterTexture)
                        {
                            var key           = textureList.Key;
                            var folderContent = new FolderContent(textureList.Value);
                            tree.AddObjectAtPath(key, folderContent);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Path Not Exist : {path}");
                    }
                }
            }


            tree.EnumerateTree().AddThumbnailIcons();

            return tree;
        }

        private string[] GetSubFolder(string path)
        {
            return Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
        }

        private Dictionary<string, List<TextureInfo>> GetFilterTexture(string path)
        {
            var fileDirection = new Dictionary<string, List<TextureInfo>>();
            var folderPaths   = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            for(var i = 0; i < folderPaths.Length; i++)
            {
                var folderPath    = folderPaths[i];
                var textureList   = FindTextureAssetByDirectory(folderPath);
                var fixFolderPath = folderPath.Replace('\\', '/');
                if(textureList.Count > 0)
                {
                    if(fileDirection.ContainsKey(fixFolderPath))
                    {
                        fileDirection[fixFolderPath].AddRange(textureList);
                    }
                    else
                        fileDirection.Add(fixFolderPath, textureList);
                }
            }
            return fileDirection;
        }

        private List<TextureInfo> FindTextureAssetByDirectory(string path)
        {
            var searchOption = SearchOption.TopDirectoryOnly;
            if(folderSettings.IsIncludeChild)
            {
                searchOption = SearchOption.AllDirectories;
            }
            var searchFiles = Directory.GetFiles(path, "*.*", searchOption)
               .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToList();

            var textureAssets = new List<TextureInfo>();


            for(var i = 0; i < searchFiles.Count; i++)
            {
                var newTextureData = new TextureInfo(searchFiles[i]);

                var textureName = searchFiles[i].Split('\\').Last();

                newTextureData.Name = textureName;


                textureAssets.Add(newTextureData);
            }

            return textureAssets;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            folderSettings = AssetDatabase.LoadAssetAtPath(SETTINGS_PATH, typeof(TextureFolderData)) as TextureFolderData;
            if(folderSettings == null)
            {
                CreateNewSettings();
            }
        }

        private void OnDisable()
        {
            EditorUtility.UnloadUnusedAssetsImmediate(true);
        }

        public void CreateNewSettings()
        {
            folderSettings = CreateInstance<TextureFolderData>();
            AssetDatabase.CreateAsset(folderSettings, SETTINGS_PATH);
        }

        private void SelectDirectory()
        {
            var selectDirectory = EditorUtility.OpenFolderPanel("Select Target Directory", targetDirectory, string.Empty);

            if(selectDirectory == string.Empty)
            {
                return;
            }

            // // ClearEditorCache();
            try
            {
                var dir = selectDirectory.Substring(selectDirectory.IndexOf("Assets"));
                folderSettings.AddPath(dir);

                // EditorPrefs.SetString(TARGET_DIRECTORY_KEY, targetDirectory);
            }
            catch
            {
                ShowNotification(new GUIContent("Invalid selection directory."));
            }
        }

    }
}