using System.Runtime.InteropServices;
using UnityEngine;

namespace WebGLTest.Platform
{
    /// <summary>
    /// 実行プラットフォーム（PC / Mobile）の判定とブラウザのウィンドウサイズ取得。
    /// WebGLビルド時はJS側から、それ以外はSystemInfoから判定。
    /// </summary>
    public static class PlatformDetect
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int PlatformBridge_IsMobile();
        [DllImport("__Internal")] private static extern int PlatformBridge_WindowWidth();
        [DllImport("__Internal")] private static extern int PlatformBridge_WindowHeight();
        [DllImport("__Internal")] private static extern int PlatformBridge_CssWindowWidth();
        [DllImport("__Internal")] private static extern int PlatformBridge_CssWindowHeight();

        public static bool IsMobile() => PlatformBridge_IsMobile() == 1;
        public static int WindowWidthPx() => PlatformBridge_WindowWidth();
        public static int WindowHeightPx() => PlatformBridge_WindowHeight();
        public static int WindowWidthCss() => PlatformBridge_CssWindowWidth();
        public static int WindowHeightCss() => PlatformBridge_CssWindowHeight();
#else
        public static bool IsMobile() => SystemInfo.deviceType == DeviceType.Handheld;
        public static int WindowWidthPx() => Screen.width;
        public static int WindowHeightPx() => Screen.height;
        public static int WindowWidthCss() => Screen.width;
        public static int WindowHeightCss() => Screen.height;
#endif
    }
}
