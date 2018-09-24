
#define _MS_APPLOVIN

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Boomlagoon.JSON;

namespace Virterix {
    namespace AdMediation {

        public class AppLovinAdapter : AdNetworkAdapter {

            public enum AppLovinBannerPosition {
                Center,
                Top,
                Bottom,
                Left,
                Right
            }

            public AppLovinBannerPosition m_bannerPlacementPosX;
            public AppLovinBannerPosition m_bannerPlacementPosY;

#if _MS_APPLOVIN

            string m_rewardInfo;
            bool m_isRewardRejected;
            bool m_isBannerLoaded;

            protected override void InitializeParameters(Dictionary<string, string> parameters, JSONArray jsonPlacements) {
                base.InitializeParameters(parameters, jsonPlacements);

                string sdkKey = "";

                if (parameters != null) {
                    if (!parameters.TryGetValue("sdkKey", out sdkKey)) {
                        sdkKey = "StxIgPTR5H-CHX-VolnxtbADt94m_rWShRlMsIpxan8sJ6M6s72ikCxaM_KLqsoWIXu8rg4PSCGnJcu9lwtS7o";
                    }
                }

#if !UNITY_EDITOR && UNITY_ANDROID || UNITY_IPHONE
                AppLovin.SetSdkKey(sdkKey);
                AppLovin.SetUnityAdListener(this.name);
                AppLovin.InitializeSdk();
#endif
            }


            float ConvertBanerPosition(AppLovinBannerPosition placement) {
                float convertedPlacement = 0;
                
                switch (placement) {
                    case AppLovinBannerPosition.Bottom:
                        convertedPlacement = AppLovin.AD_POSITION_BOTTOM;
                        break;
                    case AppLovinBannerPosition.Center:
                        convertedPlacement = AppLovin.AD_POSITION_CENTER;
                        break;
                    case AppLovinBannerPosition.Left:
                        convertedPlacement = AppLovin.AD_POSITION_LEFT;
                        break;
                    case AppLovinBannerPosition.Right:
                        convertedPlacement = AppLovin.AD_POSITION_RIGHT;
                        break;
                    case AppLovinBannerPosition.Top:
                        convertedPlacement = AppLovin.AD_POSITION_TOP;
                        break;

                }

                return convertedPlacement;
            }

            public override void Hide(AdType adType, PlacementData placementData = null) {
                switch (adType) {
                    case AdType.Banner:
                        AppLovin.HideAd();
                        break;
                }
            }

            public override bool IsReady(AdType adType, PlacementData placementData = null) {
                bool isReady = false;
                
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
                switch (adType) {
                    case AdType.Interstitial:
                        isReady = AppLovin.HasPreloadedInterstitial();
                        break;
                    case AdType.Incentivized:
                        isReady = AppLovin.IsIncentInterstitialReady();
                        break;
                    case AdType.Banner:
                        isReady = m_isBannerLoaded;
                        break;
                }
#endif

                return isReady;
            }

            public override void Prepare(AdType adType, PlacementData placementData = null) {

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
                if (!IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            AppLovin.PreloadInterstitial();
                            break;
                        case AdType.Incentivized:
                            AppLovin.LoadRewardedInterstitial();
                            break;
                        case AdType.Banner:
                            break;
                    }
                }
#endif
            }

            public override bool Show(AdType adType, PlacementData placementData = null) {
                bool success = false;
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Banner:
                            float posX = ConvertBanerPosition(m_bannerPlacementPosX);
                            float posY = ConvertBanerPosition(m_bannerPlacementPosY);
                            AppLovin.ShowAd(posX, posY);
                            break;
                        case AdType.Interstitial:
                            AppLovin.ShowInterstitial();
                            break;
                        case AdType.Incentivized:
                            AppLovin.ShowRewardedInterstitial();
                            break;
                    }
                    success = true;
                }
                return success;
            }

            void onAppLovinEventReceived(string evnt) {

                // ----- INTERSTITIAL
                if (evnt.Contains("DISPLAYEDINTER")) {
                    // An ad was shown.  Pause the game.
                    AddEvent(AdType.Interstitial, AdEvent.Show);
                }
                else if (evnt.Contains("HIDDENINTER")) {
                    // Ad ad was closed.  Resume the game.
                    // If you're using PreloadInterstitial/HasPreloadedInterstitial, make a preload call here.
                    AddEvent(AdType.Interstitial, AdEvent.Hide);
                }
                else if (evnt.Contains("LOADEDINTER")) {
                    // An interstitial ad was successfully loaded.
                    AddEvent(AdType.Interstitial, AdEvent.Prepared);
                }
                else if (string.Equals(evnt, "LOADINTERFAILED")) {
                    // An interstitial ad failed to load.
                    AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
                }
                // ----- REWARD VIDEO
                else if (evnt.Contains("DISPLAYEDREWARDED")) {
                    m_isRewardRejected = false;
                    AddEvent(AdType.Incentivized, AdEvent.Show);
                }
                else if (evnt.Contains("HIDDENREWARDED")) {
                    // A rewarded video was closed.  Preload the next rewarded video.
                    if (!m_isRewardRejected) {
                        AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
                    } else {
                        AddEvent(AdType.Incentivized, AdEvent.IncentivizedIncomplete);
                    }
                    AddEvent(AdType.Incentivized, AdEvent.Hide);
                }
                else if (evnt.Contains("REWARDAPPROVEDINFO")) {
                    m_rewardInfo = evnt;
                }
                else if (evnt.Contains("REWARDTIMEOUT")) {
                    m_isRewardRejected = true;
                }
                else if (evnt.Contains("USERCLOSEDEARLY")) {
                    m_isRewardRejected = true;
                }
                else if (evnt.Contains("REWARDREJECTED")) {
                    m_isRewardRejected = true;
                }
                else if (evnt.Contains("LOADEDREWARDED")) {
                    // A rewarded video was successfully loaded.
                    AddEvent(AdType.Incentivized, AdEvent.Prepared);
                }
                else if (evnt.Contains("LOADREWARDEDFAILED")) {
                    // A rewarded video failed to load.
                    AddEvent(AdType.Incentivized, AdEvent.PrepareFailure);
                }
                // ------ BANNER
                else if (evnt.Contains("LOADEDBANNER")) {
                    m_isBannerLoaded = true;
                    AddEvent(AdType.Banner, AdEvent.Prepared);
                }
                else if (evnt.Contains("LOADBANNERFAILED")) {
                    m_isBannerLoaded = false;
                    AddEvent(AdType.Banner, AdEvent.PrepareFailure);
                }
                else if (evnt.Contains("DISPLAYEDBANNER")) {
                    AddEvent(AdType.Banner, AdEvent.Show);
                }
                else if (evnt.Contains("HIDDENBANNER")) {
                    AddEvent(AdType.Banner, AdEvent.Hide);
                }
                else if (evnt.Contains("LEFTAPPLICATION")) {

                }
                else if (evnt.Contains("DISPLAYFAILED")) {
                    
                }
            }

#endif // _MS_APPLOVIN

        }

    } // namespace AdMediation
} // namespace Virterix

