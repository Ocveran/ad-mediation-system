#import "AdBuddizWrapper.h"
#import "AdBuddiz.h"

@interface UnityAdBuddizDelegate : NSObject <AdBuddizDelegate>
@end

@interface UnityAdBuddizRewardedVideoDelegate : NSObject <AdBuddizRewardedVideoDelegate>
@end

static UnityAdBuddizDelegate *delegate = [[UnityAdBuddizDelegate alloc] init];
static UnityAdBuddizRewardedVideoDelegate *rewardedVideoDelegate = [[UnityAdBuddizRewardedVideoDelegate alloc] init];

void _AdBuddiz_setLogLevel(const char* level) {
    
    NSString *lvl = [NSString stringWithUTF8String:level];
    
    if ([@"Info" isEqualToString:lvl]) {
        [AdBuddiz setLogLevel:ABLogLevelInfo];
    } else if ([@"Error" isEqualToString:lvl]) {
        [AdBuddiz setLogLevel:ABLogLevelError];
    } else if ([@"Silent" isEqualToString:lvl]) {
        [AdBuddiz setLogLevel:ABLogLevelSilent];
    }
}

void _AdBuddiz_setPublisherKey(const char* publisherKey) {
    [AdBuddiz setPublisherKey:[NSString stringWithUTF8String:publisherKey]];
}

void _AdBuddiz_setTestModeActive() {
    [AdBuddiz setTestModeActive];
}

void _AdBuddiz_cacheAds() {
    [AdBuddiz setDelegate:delegate];
    [AdBuddiz cacheAds];
}

bool _AdBuddiz_isReadyToShowAd() {
    return [AdBuddiz isReadyToShowAd];
}

bool _AdBuddiz_isReadyToShowAdWithPlacement(const char* placement) {
    return [AdBuddiz isReadyToShowAd:[NSString stringWithUTF8String:placement]];
}

void _AdBuddiz_showAd() {
    [AdBuddiz setDelegate:delegate];
    [AdBuddiz showAd];
}

void _AdBuddiz_showAdWithPlacement(const char* placement) {
    [AdBuddiz showAd:[NSString stringWithUTF8String:placement]];
}

void _AdBuddiz_RewardedVideo_fetch() {
    [AdBuddiz.RewardedVideo setDelegate:rewardedVideoDelegate];
    [AdBuddiz.RewardedVideo fetch];
}

bool _AdBuddiz_RewardedVideo_isReadyToShow() {
    return [AdBuddiz.RewardedVideo isReadyToShow];
}

void _AdBuddiz_RewardedVideo_show() {
    [AdBuddiz.RewardedVideo setDelegate:rewardedVideoDelegate];
    [AdBuddiz.RewardedVideo show];
}

void _AdBuddiz_RewardedVideo_setUserId(const char* userId) {
    [AdBuddiz.RewardedVideo setUserId:[NSString stringWithUTF8String:userId]];
}

void _AdBuddiz_logNative(const char* text) {
    NSLog(@"AdBuddizSDK: %@", [NSString stringWithUTF8String:text]);
}

@implementation UnityAdBuddizDelegate
- (void)didCacheAd
{
    UnitySendMessage("AdBuddizManager", "OnDidCacheAd", "");
}
- (void)didShowAd
{
    UnitySendMessage("AdBuddizManager", "OnDidShowAd", "");
}
- (void)didClick
{
    UnitySendMessage("AdBuddizManager", "OnDidClick", "");
}
- (void)didHideAd
{
    UnitySendMessage("AdBuddizManager", "OnDidHideAd", "");
}
- (void)didFailToShowAd:(AdBuddizError)error
{
    UnitySendMessage("AdBuddizManager", "OnDidFailToShowAd", [[AdBuddiz nameForError:error] UTF8String]);
}
@end

@implementation UnityAdBuddizRewardedVideoDelegate

- (void)didFetch
{
    UnitySendMessage("AdBuddizRewardedVideoManager", "OnDidFetch", "");
}

- (void)didFail:(AdBuddizError)error
{
    UnitySendMessage("AdBuddizRewardedVideoManager", "OnDidFail", [[AdBuddiz nameForError:error] UTF8String]);
}

- (void)didComplete
{
    UnitySendMessage("AdBuddizRewardedVideoManager", "OnDidComplete", "");
}

- (void)didNotComplete
{
    UnitySendMessage("AdBuddizRewardedVideoManager", "OnDidNotComplete", "");
}

@end