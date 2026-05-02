using UnityEngine;

namespace WebGLTest.Utils
{
    /// <summary>
    /// WebGLや他プラットフォームでのターゲットフレームレートを設定・安定化するスクリプト。
    /// 特にWebGLにおけるvSyncCountのハックを含みます。
    /// </summary>
    public class TargetFPS : MonoBehaviour
    {
        [Tooltip("目標とするフレームレート")]
        public int FrameRate = 60;
        
        private int _lastFrameRate = -1;

        private void Awake()
        {
            UpdateFPS();
        }

        private void Update()
        {
            // InspectorでFrameRateが変更されたら自動で再適用する（UniRxの代わり）
            if (_lastFrameRate != FrameRate)
            {
                UpdateFPS();
            }
        }

        private void UpdateFPS()
        {
            _lastFrameRate = FrameRate;

#if UNITY_WEBGL && !UNITY_EDITOR
            // 高解像度モニター（4K等）での描画負荷爆発を防ぐため、内部レンダリング解像度の上限をフルHD(1920x1080)に制限
            if (Screen.width > 1920 || Screen.height > 1080)
            {
                float aspect = (float)Screen.width / Screen.height;
                if (Screen.width > Screen.height) {
                    Screen.SetResolution(1920, Mathf.RoundToInt(1920 / aspect), false);
                } else {
                    Screen.SetResolution(Mathf.RoundToInt(1080 * aspect), 1080, false);
                }
            }

            // WebGLだとvSyncCountを0にし、60fpsではなく59fpsにしないと正常に動作しないケースへの対応
            QualitySettings.vSyncCount = 0;
            int targetRate = FrameRate;
            if (60 % FrameRate == 0)
            {
                --targetRate; // 割り切れる場合は、-1しておく (例: 60 -> 59)
            }
            Application.targetFrameRate = targetRate;
#else
            Application.targetFrameRate = FrameRate;
#endif
        }
    }
}
