mergeInto(LibraryManager.library, {
    PlatformBridge_IsMobile: function () {
        var ua = navigator.userAgent || navigator.vendor || (window.opera || "");
        var isMobile = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(ua);
        // iPad on iPadOS 13+ identifies as Mac; treat as mobile when touch is supported.
        if (!isMobile && /Macintosh/.test(ua) && navigator.maxTouchPoints > 1) {
            isMobile = true;
        }
        return isMobile ? 1 : 0;
    },
    PlatformBridge_WindowWidth: function () {
        return Math.floor(window.innerWidth * (window.devicePixelRatio || 1));
    },
    PlatformBridge_WindowHeight: function () {
        return Math.floor(window.innerHeight * (window.devicePixelRatio || 1));
    },
    PlatformBridge_CssWindowWidth: function () {
        return window.innerWidth;
    },
    PlatformBridge_CssWindowHeight: function () {
        return window.innerHeight;
    }
});
