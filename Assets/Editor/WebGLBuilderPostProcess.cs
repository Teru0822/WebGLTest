#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

namespace WebGLTest.Editor
{
    public class WebGLBuilderPostProcess
    {
        // ビルド完了直後に自動実行される処理
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.WebGL)
            {
                string indexPath = Path.Combine(pathToBuiltProject, "index.html");
                if (File.Exists(indexPath))
                {
                    string text = File.ReadAllText(indexPath);
                    
                    // まだ挿入されていなければ、devicePixelRatioの記述を自動で追加する
                    if (!text.Contains("devicePixelRatio: window.devicePixelRatio"))
                    {
                        // Unityが生成する config オブジェクトの先頭に設定を差し込む
                        text = text.Replace("var config = {", "var config = {\n        devicePixelRatio: window.devicePixelRatio,");
                        File.WriteAllText(indexPath, text);
                        Debug.Log("【自動処理】WebGLの高画質化（High DPI）設定を index.html に自動適用しました。");
                    }
                }
            }
        }
    }
}
#endif
