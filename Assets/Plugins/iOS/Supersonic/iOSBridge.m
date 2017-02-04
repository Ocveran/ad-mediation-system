//
//  iOSBridge.m
//  iOSBridge
//
//  Created by Supersonic.
//  Copyright (c) 2015 Supersonic. All rights reserved.
//

#import "iOSBridge.h"
#import "Supersonic/SupersonicIntegrationHelper.h"
#import <UIKit/UIKit.h>


// Converts NSString to C style string by way of copy (Mono will free it)
#define MakeStringCopy( _x_ ) ( _x_ != NULL && [_x_ isKindOfClass:[NSString class]] ) ? strdup( [_x_ UTF8String] ) : NULL

// Converts C style string to NSString
#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

#ifdef __cplusplus
extern "C" {
#endif

extern void UnitySendMessage( const char * className, const char * methodName, const char * param );

#ifdef __cplusplus
}
#endif

@implementation iOSBridge

char* const SUPERSONIC_EVENTS = "SupersonicEvents";

+ (iOSBridge *)start{
    static iOSBridge *instance;
    static dispatch_once_t onceToken;
    dispatch_once( &onceToken,
                  ^{
                      instance = [[iOSBridge alloc] init];
                  });
    
    return instance;
}

- (instancetype)init{
    if(self = [super init]){
        [Supersonic sharedInstance];
    }
    
    return self;
}

- (NSString *)getJsonFromDic:(NSDictionary *)dict{
    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict
                                                       options:0
                                                         error:&error];
    if (! jsonData) {
        NSLog(@"Got an error: %@", error);
        return @"";
    } else {
        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        return jsonString;
    }
}

-(void)setPluginData:(NSString*)pluginType :(NSString*) pluginVersion :(NSString*) pluginFrameworkVersion{
    [SupersonicConfiguration getConfiguration].plugin = pluginType;
    [SupersonicConfiguration getConfiguration].pluginVersion = pluginVersion;
}

-(const char *)getAdvertiserId{
    NSString *advertiserId = [Supersonic sharedInstance].getAdvertiserId;

    return MakeStringCopy(advertiserId);
}

-(void)validateIntegration{
    [SupersonicIntegrationHelper validateIntegration];
}

-(void)shouldTrackNetworkState:(BOOL)flag{
    [[Supersonic sharedInstance] setShouldTrackReachability:flag];
}
    
-(BOOL)setDynamicUserId:(NSString *)dynamicUserId{
    return [[Supersonic sharedInstance] setDynamicUserId:dynamicUserId];
}

-(void)reportAppStarted{
    [SupersonicEventsReporting reportAppStarted];
}

-(void)setAge:(int)age{
    [[Supersonic sharedInstance] setAge:age];
}

-(void)setGender:(NSString *)gender{
    if([gender caseInsensitiveCompare:@"male"] == NSOrderedSame)
        [[Supersonic sharedInstance] setGender:SUPERSONIC_USER_MALE];
    
    else if([gender caseInsensitiveCompare:@"female"] == NSOrderedSame)
        [[Supersonic sharedInstance] setGender:SUPERSONIC_USER_FEMALE];
    
    else if([gender caseInsensitiveCompare:@"unknown"] == NSOrderedSame)
        [[Supersonic sharedInstance] setGender:SUPERSONIC_USER_UNKNOWN];
}

-(void)setMediationSegment:(NSString *)segment{
    [[Supersonic sharedInstance] setMediationSegment:segment];
}


#pragma mark RewardedVideo API

- (void)initRewardedVideoWithAppKey:(NSString *)appKey withUserId:(NSString*)userId{
    [[Supersonic sharedInstance] setRVDelegate:self];
    [[Supersonic sharedInstance] initRVWithAppKey:appKey withUserId:userId];
}

-(void)showRewardedVideo{
    [[Supersonic sharedInstance] showRV];
}

-(void)showRewardedVideoWithPlacementName:(NSString*) placementName{
    [[Supersonic sharedInstance] showRVWithPlacementName:placementName];
}

-(const char *) getPlacementInfo:(NSString *)placementName{
    char* res = NULL;
    
    if (placementName){
        SupersonicPlacementInfo * spi = [[Supersonic sharedInstance] getRVPlacementInfo:placementName];
        if(spi){
            NSDictionary *dict = @{@"placement_name": [spi placementName],
                                   @"reward_amount": [spi rewardAmount],
                                   @"reward_name": [spi rewardName]};
            
            res = MakeStringCopy([self getJsonFromDic:dict]);
        }
    }
    
    return res;
}

-(BOOL)isRewardedVideoAvailable{
    return [[Supersonic sharedInstance] isAdAvailable];
}

-(BOOL)isRewardedVideoPlacementCapped:(NSString *)placementName{
    return [[Supersonic sharedInstance] isRewardedVideoPlacementCapped:placementName];
}

#pragma mark RewardedVideoDelegate

-(void)supersonicRVInitSuccess {
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoInitSuccess", "");
}

- (void)supersonicRVInitFailedWithError:(NSError *)error{
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoInitFail", [self parseErrorToEvent:error]);
}

-(void)supersonicRVAdAvailabilityChanged:(BOOL)hasAvailableAds{
    UnitySendMessage(SUPERSONIC_EVENTS, "onVideoAvailabilityChanged", (hasAvailableAds) ? "true" : "false");
}

-(void)supersonicRVAdOpened{
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoAdOpened", "");
}

-(void)supersonicRVAdStarted{
    UnitySendMessage(SUPERSONIC_EVENTS, "onVideoStart", "");
}

-(void)supersonicRVAdEnded{
    UnitySendMessage(SUPERSONIC_EVENTS, "onVideoEnd", "");
}

-(void)supersonicRVAdClosed{
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoAdClosed", "");
}

-(void)supersonicRVAdRewarded:(SupersonicPlacementInfo*) placementInfo{
    NSDictionary *dict = @{@"placement_reward_amount": placementInfo.rewardAmount,
                           @"placement_reward_name": placementInfo.rewardName,
                           @"placement_name": placementInfo.placementName};
    
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoAdRewarded", MakeStringCopy([self getJsonFromDic:dict]));
}

- (void)supersonicRVAdFailedWithError:(NSError *)error{
    UnitySendMessage(SUPERSONIC_EVENTS, "onRewardedVideoShowFail", [self parseErrorToEvent:error]);
}



#pragma mark Interstitial API

-(void)initInterstitialWithAppKey:(NSString *)appKey withUserId:(NSString*)userId{
    [[Supersonic sharedInstance] setISDelegate:self];
    [[Supersonic sharedInstance] initISWithAppKey:appKey withUserId:userId];
}

-(void)loadInterstitial{
    [[Supersonic sharedInstance] loadIS];
}

-(void)showInterstitial{
    UIViewController *viewController = [[[UIApplication sharedApplication] keyWindow] rootViewController];
    [[Supersonic sharedInstance] showISWithViewController:viewController];
}

-(void)showInterstitialWithPlacementName:(NSString*) placementName{
    UIViewController *viewController = [[[UIApplication sharedApplication] keyWindow] rootViewController];
    [[Supersonic sharedInstance] showISWithViewController:viewController placementName:placementName];
}

-(BOOL)isInterstitialReady{
    return [[Supersonic sharedInstance] isInterstitialAvailable];
}

-(BOOL)isInterstitialPlacementCapped:(NSString *)placementName{
    return [[Supersonic sharedInstance] isInterstitialPlacementCapped:placementName];
}

#pragma mark InterstitialDelegate

-(void)supersonicISInitSuccess{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialInitSuccess", "");
}

-(void)supersonicISInitFailedWithError:(NSError *)error{
    if (error)
        UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialInitFailed", [self parseErrorToEvent:error]);
    else
        UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialInitFailed", "");
}

-(void)supersonicISReady{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialReady", "");
}

-(void)supersonicISFailed{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialLoadFailed", "");
}

-(void)supersonicISShowSuccess{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialShowSuccess", "");
}

-(void)supersonicISShowFailWithError:(NSError *)error{
    if (error)
        UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialShowFailed", [self parseErrorToEvent:error]);
    else
        UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialShowFailed","");
}

-(void)supersonicISAdClicked{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialClick", "");
}

-(void)supersonicISAdOpened{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialOpen", "");
}

-(void)supersonicISAdClosed{
    UnitySendMessage(SUPERSONIC_EVENTS, "onInterstitialClose", "");
}



//public boolean onOfferwallAdCredited(int credits, int totalCredits, boolean totalCreditsFlag);
//public void onGetOfferwallCreditsFail(SupersonicError supersonicError);


#pragma mark Offerwall API

-(void)initOfferwallWithAppKey:(NSString *)appKey withUserId:(NSString*)userId{
    [[Supersonic sharedInstance] setOWDelegate:self];
    [[Supersonic sharedInstance] initOWWithAppKey:appKey withUserId:userId];
}

-(void)showOfferwall{
    [[Supersonic sharedInstance] showOW];
}

-(void)showOfferwallWithPlacementName:(NSString*) placementName{
    [[Supersonic sharedInstance] showOWWithPlacement:placementName];
}

-(void)getOfferwallCredits{
    [[Supersonic sharedInstance] getOWCredits];
}

-(BOOL)isOfferwallAvailable{
    return [[Supersonic sharedInstance] isOWAvailable];
}



#pragma mark OfferwallDelegate

-(void)supersonicOWInitSuccess{
    UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallInitSuccess", "");
}

-(void)supersonicOWShowSuccess{
    UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallOpened", "");
}

-(void)supersonicOWInitFailedWithError:(NSError *)error{
    if (error)
        UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallInitFail", [self parseErrorToEvent:error]);
    else
        UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallInitFail", "");
}

-(void)supersonicOWShowFailedWithError:(NSError *)error{
    if (error)
        UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallShowFail", [self parseErrorToEvent:error]);
    else
        UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallShowFail", "");
}

-(void)supersonicOWAdClosed{
    UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallClosed", "");
}

- (BOOL)supersonicOWDidReceiveCredit:(NSDictionary *)creditInfo{
    if(creditInfo)
        UnitySendMessage(SUPERSONIC_EVENTS, "onOfferwallAdCredited", [self getJsonFromDic:creditInfo].UTF8String);
    
    return YES;
}

-(void)supersonicOWFailGettingCreditWithError:(NSError *)error{
    if (error)
        UnitySendMessage(SUPERSONIC_EVENTS, "onGetOfferwallCreditsFail", [self parseErrorToEvent:error]);
    else
        UnitySendMessage(SUPERSONIC_EVENTS, "onGetOfferwallCreditsFail", "");
}

-(const char*)parseErrorToEvent:(NSError *)error{
    if (error){
        NSString* codeStr =  [NSString stringWithFormat:@"%ld", (long)[error code]];
        
        NSDictionary *dict = @{@"error_description": [error localizedDescription],
                               @"error_code": codeStr};
        
        return MakeStringCopy([self getJsonFromDic:dict]);
    }
    
    return nil;
}


#pragma mark C Section


#ifdef __cplusplus
extern "C" {
#endif

    void CFStart(){
        [iOSBridge start];
    }
    
    void CFReportAppStarted(){
        [[iOSBridge start] reportAppStarted];
    }

    void CFSetAge(int age){
        [[iOSBridge start] setAge:age];
    }
    
    void CFSetGender(const char* gender){
        [[iOSBridge start] setGender:GetStringParam(gender)];
    }
    
    void CFSetMediationSegment(const char* segment){
        [[iOSBridge start] setMediationSegment:GetStringParam(segment)];
    }
    
    void CFSetPluginData(const char* pluginType, const char* pluginVersion, const char* pluginFrameworkVersion){
        [[iOSBridge start] setPluginData:GetStringParam(pluginType) :GetStringParam(pluginVersion) :GetStringParam(pluginFrameworkVersion)];
    }
    
    void CFInitRewardedVideo(const char* appKey, const char* userId){
        [[iOSBridge start] initRewardedVideoWithAppKey:GetStringParam(appKey) withUserId:GetStringParam(userId)];

    }

    void CFShowRewardedVideo(){
        [[iOSBridge start] showRewardedVideo];
    }
    
    void CFShowRewardedVideoWithPlacementName(char* placementName){
        [[iOSBridge start] showRewardedVideoWithPlacementName:GetStringParam(placementName)];
    }
    
    const char * CFGetPlacementInfo(char* placementName){
        return [[iOSBridge start] getPlacementInfo:GetStringParam(placementName)];
    }

    bool CFIsRewardedVideoAvailable(){
        return [[iOSBridge start] isRewardedVideoAvailable];
    }
    
    bool CFIsRewardedVideoPlacementCapped(char* placementName){
        return [[iOSBridge start] isRewardedVideoPlacementCapped:GetStringParam(placementName)];
    }

    void CFInitInterstitial(const char* appKey,const char* userId){
        [[iOSBridge start] initInterstitialWithAppKey:GetStringParam(appKey) withUserId:GetStringParam(userId)];
    }

    void CFLoadInterstitial(){
        [[iOSBridge start] loadInterstitial];
    }
    
    void CFShowInterstitial(){
        [[iOSBridge start] showInterstitial];
    }
    
    void CFShowInterstitialWithPlacementName(char* placementName){
        [[iOSBridge start] showInterstitialWithPlacementName:GetStringParam(placementName)];
    }
    
    bool CFIsInterstitialReady(){
        return [[iOSBridge start] isInterstitialReady];
    }
    
    bool CFIsInterstitialPlacementCapped(char* placementName){
        return [[iOSBridge start] isInterstitialPlacementCapped:GetStringParam(placementName)];
    }

    void CFInitOfferwall(const char* appKey,const char* userId){
        [[iOSBridge start] initOfferwallWithAppKey:GetStringParam(appKey) withUserId:GetStringParam(userId)];
    }

    void CFShowOfferwall(){
        [[iOSBridge start] showOfferwall];
    }
    
    void CFShowOfferwallWithPlacementName(char* placementName){
        [[iOSBridge start] showOfferwallWithPlacementName:GetStringParam(placementName)];
    }

    void CFGetOfferwallCredits(){
        [[iOSBridge start] getOfferwallCredits];
    }
    
    bool CFIsOfferwallAvailable(){
        return [[iOSBridge start] isOfferwallAvailable];
    }
    
    const char* CFGetAdvertiserId(){
        return [[iOSBridge start] getAdvertiserId];
    }

    void CFValidateIntegration(){
        [[iOSBridge start] validateIntegration];
    }
    
    void CFShouldTrackNetworkState(bool flag){
        [[iOSBridge start] shouldTrackNetworkState:flag];
    }
    
    bool CFSetDynamicUserId(char* dynamicUserId){
        return [[iOSBridge start] setDynamicUserId:GetStringParam(dynamicUserId)];
    }

#ifdef __cplusplus
}
#endif

@end
