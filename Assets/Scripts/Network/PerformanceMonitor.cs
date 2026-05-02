using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace WebGLTest.Network
{
    /// <summary>
    /// クライアントの描画FPSとサーバーの処理FPS(TickRate)を画面上に表示するモニター。
    /// ボトルネックの調査用。
    /// </summary>
    public class PerformanceMonitor : NetworkBehaviour
    {
        [Networked]
        public int ServerFPS { get; set; }

        private Text _fpsText;
        private float _clientDeltaTime;
        private int _clientFPS;

        // サーバー側のFPS計測用
        private float _serverTimer;
        private int _serverFrameCount;

        public override void Spawned()
        {
            CreateUI();
        }

        private void Update()
        {
            // --- クライアント（自身の端末）のFPS計測 ---
            _clientDeltaTime += (Time.unscaledDeltaTime - _clientDeltaTime) * 0.1f;
            _clientFPS = Mathf.RoundToInt(1.0f / _clientDeltaTime);

            // --- サーバー側のFPS計測 ---
            // HasStateAuthority は、このオブジェクトの状態の権限を持つピア（今回はサーバー）を示します
            if (Object != null && Object.HasStateAuthority)
            {
                _serverFrameCount++;
                _serverTimer += Time.unscaledDeltaTime;

                if (_serverTimer >= 1.0f)
                {
                    ServerFPS = _serverFrameCount;
                    _serverFrameCount = 0;
                    _serverTimer -= 1.0f;
                }
            }

            UpdateUI();
        }

        private void CreateUI()
        {
            // 動的にCanvasとTextを生成して画面左上に配置
            var canvasObj = new GameObject("PerformanceMonitorCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // 一番手前に表示

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            var textObj = new GameObject("FPSText");
            textObj.transform.SetParent(canvasObj.transform, false);
            
            _fpsText = textObj.AddComponent<Text>();
            _fpsText.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            _fpsText.fontSize = 32;
            _fpsText.color = Color.yellow;
            _fpsText.raycastTarget = false;
            
            // 見やすくするためのアウトライン
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            var rectTransform = _fpsText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1); // 左上
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(600, 200);

            // このオブジェクトが破棄された時にUIも一緒に消えるように子オブジェクトにする
            canvasObj.transform.SetParent(this.transform);
        }

        private void UpdateUI()
        {
            if (_fpsText != null)
            {
                string mode = "Client";
                if (Runner != null)
                {
                    if (Runner.IsServer) mode = "Server";
                }

                _fpsText.text = $"Mode: {mode}\nClient FPS: {_clientFPS}\nServer FPS: {ServerFPS}";
            }
        }
    }
}
