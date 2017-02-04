#import <AdColony/AdColony.h>
#import <AdColony/AdColonyPubServices.h>
#import "UnityAppController.h"
#import "PluginBase/AppDelegateListener.h"
#import "AdColonyUnityConstants.h"
#import "AdColonyUnityJson.h"


// Converts NSString to C style string by way of copy (Mono will free it)
#define MakeStringCopy( _x_ ) ( _x_ != NULL && [_x_ isKindOfClass:[NSString class]] ) ? strdup( [_x_ UTF8String] ) : NULL

// Converts C style string to NSString
#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

// Converts C style string to NSString as long as it isnt empty
#define GetStringParamOrNil( _x_ ) ( _x_ != NULL && strlen( _x_ ) ) ? [NSString stringWithUTF8String:_x_] : nil

#define NSSTRING_OR_EMPTY(x)                        (x ? x : @"")
#define NSDICTIONARY_OR_EMPTY(x)                    (x ? x : @{})
#define IS_STRING_SET(x)                            (x && ![x isEqualToString:@""])

void UnitySendMessage(const char *className, const char *methodName, const char *param);

void SafeUnitySendMessage(const char *className, const char *methodName, const char *param) {
    if (className == NULL) {
       NSLog(@"Invalid className for UnitySendMessage, make sure ManagerName is set in platform object constructor.");
    }
    if (methodName == NULL) {
        methodName = "";
    }
    if (param == NULL) {
        param = "";
    }
    UnitySendMessage(className, methodName, param);
}

int serviceAvailabilityToUnityIndex(AdColonyPubServicesAvailability availability) {
    switch (availability) {
        case AdColonyPubServicesAvailabilityUnavailable:
            return 1;
        case AdColonyPubServicesAvailabilityConnecting:
            return 2;
        case AdColonyPubServicesAvailabilityAvailable:
            return 3;
        case AdColonyPubServicesAvailabilityInvisible:
            return 4;
        case AdColonyPubServicesAvailabilityMaintenance:
            return 5;
        case AdColonyPubServicesAvailabilityDisabled:
            return 6;
        case AdColonyPubServicesAvailabilityBanned:
            return 7;
        default:
            return 0;
    }
}


NSString *getGUID() {
    CFUUIDRef newUniqueId = CFUUIDCreate(kCFAllocatorDefault);
    NSString *uuidString = (__bridge_transfer NSString *)CFUUIDCreateString(kCFAllocatorDefault, newUniqueId);
    CFRelease(newUniqueId);
    return uuidString;
}


@interface AdcPubServicesUnityController : NSObject<AdColonyPubServicesDelegate>
@property (nonatomic, copy) NSString *managerName;
@end

@implementation AdcPubServicesUnityController

#pragma mark -

+ (AdcPubServicesUnityController *)sharedInstance {
    static AdcPubServicesUnityController * instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ instance = [[AdcPubServicesUnityController alloc] init]; });
    return instance;
}

- (void)onServiceAvailabilityChanged:(AdColonyPubServicesAvailability)availability error:(NSString *)error {
    NSDictionary *dict = @{@"available": [NSNumber numberWithUnsignedInt:[AdColonyPubServices isServiceAvailable]],
                           @"status": [NSNumber numberWithInt:serviceAvailabilityToUnityIndex(availability)],
                           @"error": NSSTRING_OR_EMPTY(error)};
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnServiceAvailabilityChanged", MakeStringCopy([dict toJsonString]));
}

- (void)onOverlayVisibilityChanged {
    NSString *value = [NSString stringWithFormat:@"%d", [AdColonyPubServices isOverlayVisible]];
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnOverlayVisibilityChanged", MakeStringCopy(value));
}

- (void)onGrantDigitalProductItem:(AdColonyPubServicesDigitalItem *)digitalItem {
    NSDictionary *dict = @{@"product_id":   NSSTRING_OR_EMPTY(digitalItem.productId),
                           @"quantity":     [NSNumber numberWithLong:digitalItem.quantity],
                           @"name":         NSSTRING_OR_EMPTY(digitalItem.name),
                           @"description":  NSSTRING_OR_EMPTY(digitalItem.productDescription)};
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnGrantDigitalProductItem", MakeStringCopy([dict toJsonString]));
}

- (void)onInAppPurchaseRewardSuccess:(NSString *)productId inGameCurrencyBonus:(unsigned int)inGameCurrencyBonus {
    NSDictionary *dict = @{@"product_id": NSSTRING_OR_EMPTY(productId),
                           @"in_game_currency_bonus": [NSNumber numberWithUnsignedInt:inGameCurrencyBonus]};
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnIAPProductPurchased", MakeStringCopy([dict toJsonString]));
}

- (void)onInAppPurchaseReward:(NSString *)productId didFail:(NSError *)error {
    NSDictionary *dict = @{@"product_id": NSSTRING_OR_EMPTY(productId),
                           @"error": NSSTRING_OR_EMPTY([error description])};
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnIAPProductPurchaseFailed", MakeStringCopy([dict toJsonString]));
}

- (void)onStatsRefreshed {
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnStatsUpdated", nil);
}

- (void)onVIPInformationChanged {
    SafeUnitySendMessage(MakeStringCopy(self.managerName), "_OnVIPInformationUpdated", nil);
}

@end


@interface AdcPubServicesSystemDelegate : NSObject<AppDelegateListener>
@property (nonatomic, strong) NSMutableArray *messages;
@property (nonatomic, assign) BOOL initialized;
@end

@implementation AdcPubServicesSystemDelegate

+ (void)load {
    UnityRegisterAppDelegateListener([AdcPubServicesSystemDelegate sharedInstance]);
}

+ (AdcPubServicesSystemDelegate *)sharedInstance {
    static AdcPubServicesSystemDelegate * instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ instance = [AdcPubServicesSystemDelegate new]; });
    return instance;
}

- (id)init {
    if (self = [super init]) {
        self.messages = @[].mutableCopy;
        self.initialized = FALSE;
    }
    return self;
}

- (void)flushMessages {
    dispatch_async(dispatch_get_main_queue(), ^{
        for (NSDictionary *dict in self.messages) {
            NSString *selectorName = dict[@"function"];
            NSNotification *value = dict[@"value"];
            NSLog(@"flushing message: %@:%@", selectorName, value.description);
            SEL selector = NSSelectorFromString(selectorName);
            ((void(*)(id, SEL, NSNotification *))[self methodForSelector:selector])(self, selector, value);
        }
        self.messages = @[].mutableCopy;
    });
}

- (void)addMessage:(NSString *)function value:(NSNotification *)value {
    dispatch_async(dispatch_get_main_queue(), ^{
        NSLog(@"adding message: %@:%@", function, value.description);
        [self.messages addObject:@{@"function": function,
                                   @"value": value}];

        if (self.initialized) {
            [self flushMessages];
        }
    });
}

// notification user data is deviceToken
- (void)didRegisterForRemoteNotificationsWithDeviceToken:(NSNotification *)notification {
    [self addMessage:@"handleRegisterForRemoteNotificationsWithDeviceToken:" value:notification];
}
- (void)handleRegisterForRemoteNotificationsWithDeviceToken:(NSNotification *)notification {
    NSData *deviceToken = (NSData *)notification.userInfo;
    NSString *deviceTokenString = notification.userInfo.description;
    NSLog(@"didRegisterForRemoteNotificationsWithDeviceToken: %@", deviceTokenString);
    [AdColonyPubServices setPushNotificationDeviceToken:deviceToken];
    SafeUnitySendMessage(MakeStringCopy([AdcPubServicesUnityController sharedInstance].managerName), "_OnRegisteredForPushNotifications", MakeStringCopy(deviceTokenString));
}

// notification user data is error
- (void)didFailToRegisterForRemoteNotificationsWithError:(NSNotification *)notification {
    [self addMessage:@"handleFailToRegisterForRemoteNotificationsWithError:" value:notification];
}
- (void)handleFailToRegisterForRemoteNotificationsWithError:(NSNotification *)notification {
    NSString *error = ((NSError *)notification.userInfo).localizedDescription;
    NSLog(@"didFailToRegisterForRemoteNotificationsWithError: %@", error);
    SafeUnitySendMessage(MakeStringCopy([AdcPubServicesUnityController sharedInstance].managerName), "_OnRegisteredForPushNotificationsFailed", MakeStringCopy(error));
}

// notification user data is userInfo
- (void)didReceiveRemoteNotification:(NSNotification *)notification {
    [self addMessage:@"handleReceiveRemoteNotification:" value:notification];
}
- (void)handleReceiveRemoteNotification:(NSNotification *)notification {
    NSLog(@"didReceiveRemoteNotification: %@", notification.userInfo);
    NSDictionary *userInfo = (NSDictionary *)notification.userInfo;
    [AdColonyPubServices handleRemoteNotification:userInfo action:nil callback:^(AdColonyPubServicesPushNotification *pushNotification, BOOL handled) {
        NSDictionary *dict = @{@"notification_id": NSSTRING_OR_EMPTY(pushNotification.notificationId),
                               @"action":          NSSTRING_OR_EMPTY(pushNotification.action),
                               @"message":         NSSTRING_OR_EMPTY(pushNotification.message),
                               @"title":           NSSTRING_OR_EMPTY(pushNotification.title),
                               @"category":        NSSTRING_OR_EMPTY(pushNotification.category),
                               @"date_received":   @([pushNotification.dateReceived timeIntervalSince1970]),
                               @"payload":         NSSTRING_OR_EMPTY(pushNotification.payload),
                               @"is_pubservices_notification": @(pushNotification.isPubServicesNotification)};
        SafeUnitySendMessage(MakeStringCopy([AdcPubServicesUnityController sharedInstance].managerName), "_OnPushNotificationReceived", MakeStringCopy([dict toJsonString]));
    }];

}

// notification user data is the NSDictionary containing all the params
- (void)onOpenURL:(NSNotification *)notification {
    NSLog(@"[%s]", __PRETTY_FUNCTION__);
    [self addMessage:@"handleOpenURL:" value:notification];
}
- (void)handleOpenURL:(NSNotification *)notification {
    NSLog(@"[%s]", __PRETTY_FUNCTION__);
    NSLog(@"onOpenURL: %@", notification.userInfo);
    NSDictionary *openUrlParams = (NSDictionary *)notification.userInfo;
    NSURL *url = [openUrlParams objectForKey:@"url"];
    NSString *sourceApplication = [openUrlParams objectForKey:@"sourceApplication"];
    [AdColonyPubServices handleOpenURL:url sourceApplication:NSSTRING_OR_EMPTY(sourceApplication) callback:^(NSDictionary *urlParams, BOOL adcHandled) {
        NSDictionary *dict = @{@"url": NSSTRING_OR_EMPTY(url.absoluteString),
                               @"source_application": NSSTRING_OR_EMPTY(sourceApplication),
                               @"url_params": NSDICTIONARY_OR_EMPTY(urlParams),
                               @"handled": @(adcHandled)};
        SafeUnitySendMessage(MakeStringCopy([AdcPubServicesUnityController sharedInstance].managerName), "_OnURLOpened", MakeStringCopy([dict toJsonString]));
    }];
}

@end



@interface AdcAdsUnityController : NSObject<AdColonyPubServicesDelegate>
@property (nonatomic, copy) NSString *managerName;
@property (nonatomic, strong) NSMutableDictionary *interstitialAds;
@property (nonatomic, copy) NSString *appOptionsJson;
@end

@implementation AdcAdsUnityController

#pragma mark -

+ (AdcAdsUnityController *)sharedInstance {
    static AdcAdsUnityController * instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ instance = [[AdcAdsUnityController alloc] init]; });
    return instance;
}

- (id)init {
    if (self = [super init]) {
        self.interstitialAds = @{}.mutableCopy;
    }
    return self;
}

@end


// When native code plugin is implemented in .mm / .cpp file, then functions
// should be surrounded with extern "C" block to conform C function naming rules
extern "C" {
    void _AdcSetManagerName(const char *managerName) {
        [AdcPubServicesUnityController sharedInstance].managerName = GetStringParam(managerName);
    }

    void _AdcPubServicesConfigure(const char *initParamsJson) {
        NSLog(@"[%s] initParamsJson: %s", __PRETTY_FUNCTION__, initParamsJson);
        NSString *json = GetStringParam(initParamsJson);
        NSDictionary *initParams = [json jsonStringToDictionary];
        [AdColonyPubServices configureWithInitParams:initParams delegate:[AdcPubServicesUnityController sharedInstance]];
    }

    void _AdcSetNotificationsAllowed(int value) {
        // Shift because iOS mask is offset
        value = value << 1;
        [AdColonyPubServices setNotificationsAllowed:(AdColonyPubServicesNotificationMask)value];
    }

    bool _AdcPurchaseProduct(const char *productId, const char *base64ReceiptData, const char *transactionId, int quantity, int inGameCurrencyQuantityForProduct) {
        if (productId == nil || base64ReceiptData == nil || transactionId == nil || quantity <= 0) {
            return false;
        }

        if (inGameCurrencyQuantityForProduct < 0) {
            inGameCurrencyQuantityForProduct = 0;
        }

        [AdColonyPubServices grantRewardFromInAppPurchase:GetStringParamOrNil(productId)
                                              receiptData:GetStringParamOrNil(base64ReceiptData)
                                            transactionId:GetStringParamOrNil(transactionId)
                         inGameCurrencyQuantityForProduct:inGameCurrencyQuantityForProduct];
        return true;
    }

    void _AdcShowOverlay() {
        [AdColonyPubServices showOverlay];
    }

    void _AdcCloseOverlay() {
        [AdColonyPubServices closeOverlay];
    }

    bool _AdcIsOverlayVisible() {
        return [AdColonyPubServices isOverlayVisible];
    }

    bool _AdcIsServiceAvailable() {
        return [AdColonyPubServices isServiceAvailable];
    }

    char *_AdcGetExperiments() {
        NSDictionary *dict = [AdColonyPubServices getExperiments];
        if (dict) {
            return MakeStringCopy([dict toJsonString]);
        }
        return NULL;
    }

    char *_AdcGetVipInformation() {
        NSDictionary *dict = [[AdColonyPubServices getVIPInformation] rawData];
        if (dict) {
            return MakeStringCopy([dict toJsonString]);
        }
        return NULL;
    }

    char *_AdcGetStats() {
        NSArray *stats = [AdColonyPubServices getStats];
        if (stats) {
            return MakeStringCopy([stats toJsonString]);
        }
        return NULL;
    }

    int _AdcGetStat(const char *name) {
        int ret = -1;
        NSNumber *statObj = [AdColonyPubServices getStatValue:GetStringParamOrNil(name)];
        if (statObj) {
            ret = [statObj intValue];
        }
        return ret;
    }

    bool _AdcSetStat(const char *name, int value) {
        return [AdColonyPubServices setStat:GetStringParamOrNil(name) value:value];
    }

    bool _AdcIncrementStat(const char *name, int value) {
        return [AdColonyPubServices incrementStat:GetStringParamOrNil(name) value:value];
    }

    void _AdcRefreshStats() {
        [AdColonyPubServices refreshStats];
    }

    void _AdcMarkStartRound() {
        [AdColonyPubServices markStartRound];
    }

    void _AdcMarkEndRound() {
        [AdColonyPubServices markEndRound];
    }

    void _AdcRegsiterForPushNotifications(int value) {
        NSLog(@"[%s]", __PRETTY_FUNCTION__);
        NSLog(@"_AdcRegsiterForPushNotifications from iOS: %d", value);
        [AdColonyPubServices registerForPushNotifications:value];
    }

    void _AdcUnregsiterForPushNotifications(int value) {
        NSLog(@"[%s]", __PRETTY_FUNCTION__);
        [AdColonyPubServices unregisterForPushNotifications];
    }

    void _AdcSetUnityInitialized() {
        NSLog(@"[%s]", __PRETTY_FUNCTION__);
        dispatch_async(dispatch_get_main_queue(), ^{
            [AdcPubServicesSystemDelegate sharedInstance].initialized = TRUE;
            [[AdcPubServicesSystemDelegate sharedInstance] flushMessages];
        });
    }

    // Ads

    void _AdcSetManagerNameAds(const char *managerName) {
        [AdcAdsUnityController sharedInstance].managerName = GetStringParam(managerName);
    }

    void _AdcConfigure(const char *appId_, const char *appOptionsJson_, int zoneIdsCount_, const char *zoneIds_[]) {
        NSLog(@"ADS _AdcConfigure");

        NSString *appId = GetStringParamOrNil(appId_);

        NSMutableArray *zoneIds = @[].mutableCopy;
        for (int i = 0; i < zoneIdsCount_; ++i) {
            [zoneIds addObject:GetStringParamOrNil(zoneIds_[i])];
        }

        NSString *appOptionsJson = GetStringParamOrNil(appOptionsJson_);
        [AdcAdsUnityController sharedInstance].appOptionsJson = appOptionsJson;
        AdColonyAppOptions *appOptions = nil;
        if (appOptionsJson) {
            appOptions = [[AdColonyAppOptions alloc] init];
            [appOptions setupWithJson:appOptionsJson];
        }

        [AdColony configureWithAppID:appId zoneIDs:zoneIds options:appOptions completion:^(NSArray<AdColonyZone *> *zones) {
            NSLog(@"ADS configure completed");

            NSMutableArray *zoneJsonArray = [NSMutableArray array];
            for (AdColonyZone *zone in zones) {
                [zoneJsonArray addObject:[zone toJsonString]];

                if (zone.rewarded) {
                    // For each zone returned that is also a rewarded zone, register a callback that will then call _OnRewardGranted.
                    NSString *zoneID = zone.identifier;
                    [zone setReward:^(BOOL success, NSString * _Nonnull name, int amount) {
                        NSDictionary *rewardDict = @{ADC_ON_REWARD_GRANTED_ZONEID_KEY  : zoneID,
                                                     ADC_ON_REWARD_GRANTED_SUCCESS_KEY : [NSNumber numberWithBool:success],
                                                     ADC_ON_REWARD_GRANTED_NAME_KEY    : name,
                                                     ADC_ON_REWARD_GRANTED_AMOUNT_KEY  : [NSString stringWithFormat:@"%d", amount]};
                        SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnRewardGranted", MakeStringCopy([rewardDict toJsonString]));
                    }];
                }
            }

            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnConfigure", MakeStringCopy([zoneJsonArray toJsonString]));
        }];
    }

    const char *_AdcGetSDKVersion() {
        return MakeStringCopy([AdColony getSDKVersion]);
    }

    void _AdcShowInterstitialAd(const char *id) {
        NSLog(@"ADS _AdcShowInterstitialAd %s", ((id == NULL) ? "NULL" : id));
        NSString *adId = GetStringParamOrNil(id);
        if (adId) {
            AdColonyInterstitial *ad = [[AdcAdsUnityController sharedInstance].interstitialAds objectForKey:adId];
            if (ad) {
                UnityAppController* unityAppController = GetAppController();
                [ad showWithPresentingViewController:unityAppController.rootViewController];
                return;
            } else {
                NSLog(@"ADS _AdcShowInterstitialAd ad not found %@", adId);
            }
        }
        NSLog(@"ADS Unable to show ad");
    }

    void _AdcCancelInterstitialAd(const char *id) {
        NSLog(@"ADS _AdcCancelInterstitialAd");
        NSString *adId = GetStringParamOrNil(id);
        if (adId) {
            AdColonyInterstitial *ad = [[AdcAdsUnityController sharedInstance].interstitialAds objectForKey:adId];
            if (ad) {
                [ad cancel];
                return;
            }
        }
        NSLog(@"ADS Unable to cancel ad");
    }

    void _AdcDestroyInterstitialAd(const char *id) {
        NSString *adId = GetStringParamOrNil(id);
        [[AdcAdsUnityController sharedInstance].interstitialAds removeObjectForKey:adId];
    }

    void _AdcRequestInterstitialAd(const char *zoneId, const char *adOptionsJson) {
        NSLog(@"ADS _AdcRequestInterstitialAd");

        NSString *myAdOptionsJson = GetStringParamOrNil(adOptionsJson);
        AdColonyAdOptions *adOptions = nil;
        if (myAdOptionsJson) {
            adOptions = [[AdColonyAdOptions alloc] init];
            [adOptions setupWithJson:myAdOptionsJson];
        }

        [AdColony requestInterstitialInZone:GetStringParam(zoneId)
                                    options:adOptions
                                    success:^(AdColonyInterstitial *ad) {
                                        NSLog(@"ADS _AdcRequestInterstitialAd success");
                                        NSString *adId = getGUID();
                                        NSLog(@"ADS _AdcRequestInterstitialAd success, id: %@", adId);
                                        [AdcAdsUnityController sharedInstance].interstitialAds[adId] = ad;
                                        NSDictionary *dict = @{@"zone_id": ad.zoneID,
                                                               @"expired": @(ad.expired),
                                                               @"audio_enabled": @(ad.audioEnabled),
                                                               @"iap_enabled": @(ad.iapEnabled),
                                                               @"id": adId};

                                        // Setup callbacks
                                        [ad setOpen:^{
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnOpened", MakeStringCopy([dict toJsonString]));
                                        }];
                                        [ad setClose:^{
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnClosed", MakeStringCopy([dict toJsonString]));
                                        }];
                                        [ad setAudioStop:^{
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnAudioStopped", MakeStringCopy([dict toJsonString]));
                                        }];
                                        [ad setAudioStart:^{
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnAudioStarted", MakeStringCopy([dict toJsonString]));
                                        }];
                                        [ad setExpire:^{
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnExpiring", MakeStringCopy([dict toJsonString]));
                                        }];
                                        [ad setIapOpportunity:^(NSString * _Nonnull iapProductID, AdColonyIAPEngagement engagement) {
                                            NSMutableDictionary *mutableDict = dict.mutableCopy;
                                            [mutableDict setObject:iapProductID forKey:ADC_ON_IAP_OPPORTUNITY_IAP_PRODUCT_ID_KEY];
                                            [mutableDict setObject:@((int)engagement) forKey:ADC_ON_IAP_OPPORTUNITY_ENGAGEMENT_KEY];
                                            SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnIAPOpportunity", MakeStringCopy([mutableDict toJsonString]));
                                        }];

                                        SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnRequestInterstitial", MakeStringCopy([dict toJsonString]));
                                    }
                                    failure:^(AdColonyAdRequestError *error) {
                                        NSLog(@"ADS _AdcRequestInterstitialAd failure");
                                        NSDictionary *dict = @{@"error_code": @(error.code)};
                                        SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnRequestInterstitialFailed", MakeStringCopy([dict toJsonString]));
                                    }];
    }

    // should return JSON
    const char *_AdcGetZone(const char *zoneId) {
        NSString *zoneString = GetStringParamOrNil(zoneId);
        if (zoneString == nil) {
            return nil;
        }

        AdColonyZone *zone = [AdColony zoneForID:zoneString];
        if (zone == nil) {
            return nil;
        }

        return MakeStringCopy([zone toJsonString]);
    }

    const char *_AdcGetUserID() {
        return MakeStringCopy([AdColony getUserID]);
    }

    void _AdcSetAppOptions(const char *appOptionsJson) {
        [AdcAdsUnityController sharedInstance].appOptionsJson = GetStringParam(appOptionsJson);
        
        AdColonyAppOptions *appOptions = [[AdColonyAppOptions alloc] init];
        [appOptions setupWithJson:[AdcAdsUnityController sharedInstance].appOptionsJson];
        [AdColony setAppOptions:appOptions];
    }

    const char *_AdcGetAppOptions() {
        return MakeStringCopy([AdcAdsUnityController sharedInstance].appOptionsJson);
    }

    void _AdcSendCustomMessage(const char *type, const char *content) {
        NSString *typeString = GetStringParamOrNil(type);
        if (typeString != nil) {
            [AdColony sendCustomMessageOfType:typeString
                                  withContent:GetStringParamOrNil(content)
                                        reply:^(id _Nullable obj) {
                                            if ([obj isKindOfClass:[NSString class]]) {
                                                NSDictionary *messageDictionary = @{ADC_ON_CUSTOM_MESSAGE_RECEIVED_TYPE_KEY    : typeString,
                                                                                    ADC_ON_CUSTOM_MESSAGE_RECEIVED_MESSAGE_KEY : GetStringParam(content)};
                                                SafeUnitySendMessage(MakeStringCopy([AdcAdsUnityController sharedInstance].managerName), "_OnCustomMessageRecieved", MakeStringCopy([messageDictionary toJsonString]));
                                            }
                                        }];
        }
    }

    void _AdcLogInAppPurchase(const char *transactionId, const char *productId, int purchasePriceMicro, const char *currencyCode) {
        [AdColony iapCompleteWithTransactionID:GetStringParam(transactionId)
                                     productID:GetStringParam(productId)
                                         price:[NSNumber numberWithInt:purchasePriceMicro]
                                  currencyCode:GetStringParamOrNil(currencyCode)];
    }
}
