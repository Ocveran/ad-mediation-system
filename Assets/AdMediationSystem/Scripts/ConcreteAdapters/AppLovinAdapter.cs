
#define _MS_APPLOVIN

#if _MS_APPLOVIN

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Virterix {
    namespace AdMediation {

        public class AppLovinAdapter : AdNetworkAdapter {

            string m_rewardInfo;
            bool m_isRewardRejected;

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                string sdkKey = "";

                if (parameters != null) {
                    if (!parameters.TryGetValue("sdkKey", out sdkKey)) {
                        sdkKey = "";
                    }
                }

#if !UNITY_EDITOR && UNITY_ANDROID || UNITY_IPHONE
                AppLovin.SetSdkKey(sdkKey);
                AppLovin.SetUnityAdListener(this.name);
                AppLovin.InitializeSdk();
#endif
            }

            public override void Hide(AdType adType) {
            }

            public override bool IsReady(AdType adType) {
                bool isReady = false;

#if !UNITY_EDITOR && UNITY_ANDROID || UNITY_IPHONE
                switch (adType) {
                    case AdType.Interstitial:
                        isReady = AppLovin.HasPreloadedInterstitial();
                        break;
                    case AdType.Incentivized:
                        isReady = AppLovin.IsIncentInterstitialReady();
                        break;
                }
#endif

                return isReady;
            }

            public override void Prepare(AdType adType) {
#if !UNITY_EDITOR && UNITY_ANDROID || UNITY_IPHONE
                if (!IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            AppLovin.PreloadInterstitial();
                            break;
                        case AdType.Incentivized:
                            AppLovin.LoadRewardedInterstitial();
                            break;
                    }
                }
#endif
            }

            public override bool Show(AdType adType) {
                bool success = false;
                if (IsReady(adType)) {
                    switch (adType) {
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
                else if (evnt.Contains("DISPLAYEDREWARDED")) {
                    m_isRewardRejected = false;
                    AddEvent(AdType.Incentivized, AdEvent.Show);
                }
                else if (evnt.Contains("HIDDENREWARDED")) {
                    // A rewarded video was closed.  Preload the next rewarded video.
                    if (!m_isRewardRejected) {
                        AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
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
                    AddEvent(AdType.Incentivized, AdEvent.Hide);
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
            }

        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_APPLOVIN
