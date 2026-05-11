using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace WebGLTest.Player
{
    /// <summary>
    /// スマホ用タッチ操作。
    /// - 画面左下の丸ジョイスティック → 平面移動（MoveInput）
    /// - ジョイスティック以外をドラッグ → カメラ視点（LookDelta）
    /// PlayerSpawnerからこのコンポーネントを生成して値を読み取る想定。
    /// </summary>
    public class MobileTouchControls : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookDelta { get; private set; }

        private const float JoystickRadius = 130f;        // 親の見た目半径(px)
        private const float JoystickGrabRadius = 200f;     // 押し始め時にジョイスティックとみなす半径
        private const float LookSensitivity = 0.2f;

        private RectTransform _joystickBase;
        private RectTransform _joystickKnob;
        private Vector2 _joystickAnchorScreen;

        private int _joystickFingerId = -1;
        private int _cameraFingerId = -1;
        private Vector2 _lastCameraScreenPos;

        private void Awake()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            var canvasObj = new GameObject("MobileTouchCanvas");
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObj.AddComponent<CanvasScaler>(); // デフォルト(ConstantPixelSize)のままでOK

            var sprite = MakeCircleSprite();

            // ジョイスティック土台
            var baseObj = new GameObject("JoystickBase");
            baseObj.transform.SetParent(canvasObj.transform, false);
            var baseImg = baseObj.AddComponent<Image>();
            baseImg.sprite = sprite;
            baseImg.color = new Color(1f, 1f, 1f, 0.25f);
            baseImg.raycastTarget = false;
            _joystickBase = baseObj.GetComponent<RectTransform>();
            _joystickBase.anchorMin = Vector2.zero;
            _joystickBase.anchorMax = Vector2.zero;
            _joystickBase.pivot = new Vector2(0.5f, 0.5f);
            _joystickBase.sizeDelta = Vector2.one * (JoystickRadius * 2f);
            _joystickBase.anchoredPosition = new Vector2(JoystickRadius + 50f, JoystickRadius + 50f);
            _joystickAnchorScreen = _joystickBase.anchoredPosition;

            // ノブ（中の小さい丸）
            var knobObj = new GameObject("JoystickKnob");
            knobObj.transform.SetParent(baseObj.transform, false);
            var knobImg = knobObj.AddComponent<Image>();
            knobImg.sprite = sprite;
            knobImg.color = new Color(1f, 1f, 1f, 0.7f);
            knobImg.raycastTarget = false;
            _joystickKnob = knobObj.GetComponent<RectTransform>();
            _joystickKnob.anchorMin = new Vector2(0.5f, 0.5f);
            _joystickKnob.anchorMax = new Vector2(0.5f, 0.5f);
            _joystickKnob.pivot = new Vector2(0.5f, 0.5f);
            _joystickKnob.sizeDelta = Vector2.one * JoystickRadius;
            _joystickKnob.anchoredPosition = Vector2.zero;
        }

        private static Sprite MakeCircleSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // 毎フレーム LookDelta はリセット（差分のみ伝える）
            LookDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            ProcessTouchesNewInput();
#else
            ProcessTouchesLegacy();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void ProcessTouchesNewInput()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            for (int i = 0; i < ts.touches.Count; i++)
            {
                var t = ts.touches[i];
                int fingerId = t.touchId.ReadValue();
                var phase = t.phase.ReadValue();
                Vector2 pos = t.position.ReadValue();

                if (phase == TouchPhase.Began)
                {
                    HandleTouchBegan(fingerId, pos);
                }
                else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                {
                    HandleTouchMoved(fingerId, pos);
                }
                else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
                {
                    HandleTouchEnded(fingerId);
                }
            }
        }
#else
        private void ProcessTouchesLegacy()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                int fingerId = t.fingerId;
                Vector2 pos = t.position;

                switch (t.phase)
                {
                    case UnityEngine.TouchPhase.Began:
                        HandleTouchBegan(fingerId, pos);
                        break;
                    case UnityEngine.TouchPhase.Moved:
                    case UnityEngine.TouchPhase.Stationary:
                        HandleTouchMoved(fingerId, pos);
                        break;
                    case UnityEngine.TouchPhase.Ended:
                    case UnityEngine.TouchPhase.Canceled:
                        HandleTouchEnded(fingerId);
                        break;
                }
            }
        }
#endif

        private void HandleTouchBegan(int fingerId, Vector2 pos)
        {
            if (_joystickFingerId == -1 && Vector2.Distance(pos, _joystickAnchorScreen) <= JoystickGrabRadius)
            {
                _joystickFingerId = fingerId;
                UpdateJoystick(pos);
            }
            else if (_cameraFingerId == -1)
            {
                _cameraFingerId = fingerId;
                _lastCameraScreenPos = pos;
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 pos)
        {
            if (fingerId == _joystickFingerId)
            {
                UpdateJoystick(pos);
            }
            else if (fingerId == _cameraFingerId)
            {
                Vector2 delta = pos - _lastCameraScreenPos;
                LookDelta += delta * LookSensitivity;
                _lastCameraScreenPos = pos;
            }
        }

        private void HandleTouchEnded(int fingerId)
        {
            if (fingerId == _joystickFingerId)
            {
                _joystickFingerId = -1;
                MoveInput = Vector2.zero;
                if (_joystickKnob != null) _joystickKnob.anchoredPosition = Vector2.zero;
            }
            else if (fingerId == _cameraFingerId)
            {
                _cameraFingerId = -1;
            }
        }

        private void UpdateJoystick(Vector2 screenPos)
        {
            Vector2 delta = screenPos - _joystickAnchorScreen;
            float mag = delta.magnitude;
            if (mag > JoystickRadius)
            {
                delta = delta / mag * JoystickRadius;
                mag = JoystickRadius;
            }
            _joystickKnob.anchoredPosition = delta;
            MoveInput = delta / JoystickRadius; // -1..1
        }
    }
}
