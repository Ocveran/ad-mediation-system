extern "C" {
    void _AdBuddiz_setLogLevel(const char* level);
    void _AdBuddiz_setPublisherKey(const char* publisherKey);
    void _AdBuddiz_setTestModeActive();
    
    void _AdBuddiz_cacheAds();
    bool _AdBuddiz_isReadyToShowAd();
    bool _AdBuddiz_isReadyToShowAdWithPlacement(const char* placement);
    void _AdBuddiz_showAd();
    void _AdBuddiz_showAdWithPlacement(const char* placement);
    
    void _AdBuddiz_RewardedVideo_fetch();
    bool _AdBuddiz_RewardedVideo_isReadyToShow();
    void _AdBuddiz_RewardedVideo_show();
    void _AdBuddiz_RewardedVideo_setUserId(const char* placement);
    
    void _AdBuddiz_logNative(const char* text);
}
