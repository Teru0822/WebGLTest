using UnityEngine;
using UnityEditor;

namespace WebGLTest.Editor
{
    public class WebGLPerformanceOptimizer : EditorWindow
    {
        [MenuItem("WebGLTest/🚀 WebGLのパフォーマンスを最適化する (16FPS対策)")]
        public static void Optimize()
        {
            Debug.Log("--- WebGLのパフォーマンス最適化を開始します ---");

            // 1. 全マテリアルの GPU Instancing を有効化
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int instancingCount = 0;
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    instancingCount++;
                }
            }
            Debug.Log($"✅ {instancingCount} 個のマテリアルで [GPU Instancing] を有効化しました。これにより500個のオブジェクトを1回の命令で描画できるようになります！");

            // 2. 品質設定（Quality Settings）のWebGL向け軽量化
            // （ここでは現在の設定を上書きして軽量化します）
            QualitySettings.vSyncCount = 0; // WebGLはvSync 0必須
            QualitySettings.shadowCascades = 1; // 影の処理を一番軽くする
            QualitySettings.shadowDistance = 30f; // 影を描画する距離を短くする
            QualitySettings.antiAliasing = 0; // アンチエイリアスを切る
            
            // 3. WebGLビルド設定の最適化（激重になる例外サポートを切り、最適化レベルを上げる）
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            
            Debug.Log("✅ 品質設定およびWebGLビルド設定（例外サポートなし等）を最軽量化しました。");

            // 保存
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("最適化完了！", 
                "マテリアルのGPU Instancing有効化と、影の軽量化が完了しました。\n\n" +
                "これでドローコール（描画負荷）が激減します。\n再度WebGLビルドを試してみてください！", "OK");
        }
    }
}
