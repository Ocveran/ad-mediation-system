
#define _MS_CHARTBOOST

#if _MS_CHARTBOOST

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ChartboostSDK;

namespace Virterix {
    namespace AdMediation {

        public class ChartboostAdapter : AdNetworkAdapter {

            void Awake() {
            }

            void OnEnable() {
                SubscribeEvents();
            }

            void OnDisable() {
                UnsubscribeEvents();
            }

            void SubscribeEvents() {
                // Interstitial
                Chartboost.didCacheInterstitial += DidCacheInterstitial;
                Chartboost.didFailToLoadInterstitial += DidFailToLoadInterstitial;
                Chartboost.shouldDisplayInterstitial += ShouldDisplayInterstitial;
                Chartboost.didCloseInterstitial += DidCloseInterstitial;
                Chartboost.didDismissInterstitial += DidDismissInterstitial;
                // RewardedVideo
                Chartboost.didCacheRewardedVideo += DidCacheRewardedVideo;
                Chartboost.didFailToLoadRewardedVideo += DidFailToLoadRewardedVideo;
                Chartboost.shouldDisplayRewardedVideo += shouldDisplayRewardedVideo;
                Chartboost.didCloseRewardedVideo += DidCloseRewardedVideo;
                Chartboost.didDismissRewardedVideo += DidDismissRewardedVideo;
                Chartboost.didCompleteRewardedVideo += DidCompleteRewardedVideo;               
            }

            void UnsubscribeEvents() {
                // Interstitia
                Chartboost.didCacheInterstitial -= DidCacheInterstitial;
                Chartboost.didFailToLoadInterstitial -= DidFailToLoadInterstitial;
                Chartboost.shouldDisplayInterstitial -= ShouldDisplayInterstitial;
                Chartboost.didCloseInterstitial -= DidCloseInterstitial;
                Chartboost.didDismissInterstitial -= DidDismissInterstitial;
                // RewardedVideo
                Chartboost.didCacheRewardedVideo -= DidCacheRewardedVideo;
                Chartboost.didFailToLoadRewardedVideo -= DidFailToLoadRewardedVideo;
                Chartboost.shouldDisplayRewardedVideo -= shouldDisplayRewardedVideo;
                Chartboost.didCloseRewardedVideo -= DidCloseRewardedVideo;
                Chartboost.didDismissRewardedVideo -= DidDismissRewardedVideo;
                Chartboost.didCompleteRewardedVideo -= DidCompleteRewardedVideo;
            }
            
            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                bool autocache = false;
                string autocacheStr = "";
                string appId = "";
                string appSignature = "";

                parameters.TryGetValue("autocache", out autocacheStr);
                autocache = autocacheStr == "true";
                parameters.TryGetValue("appId", out appId);
                parameters.TryGetValue("appSignature", out appSignature);

                if (appId != null && appSignature != null) {
                    Chartboost.CreateWithAppId(appId, appSignature);
                }
                Chartboost.setAutoCacheAds(autocache);
            }

            public override void Prepare(AdType adType) {
                switch(adType) {
                    case AdType.Interstitial:
                        Chartboost.cacheInterstitial(CBLocation.Default);
                        break;
                    case AdType.Incentivized:
                        Chartboost.cacheRewardedVideo(CBLocation.Default);
                        break;
                }
            }

            public override bool Show(AdType adType) {
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            Chartboost.showInterstitial(CBLocation.Default);
                            break;
                        case AdType.Incentivized:
                            Chartboost.showRewardedVideo(CBLocation.Default);
                            break;
                    }
                    return true;
                }
                return false;
            }

            public override void Hide(AdType adType) {

            }

            public override bool IsReady(AdType adType) {
                bool isReady = false;
                switch (adType) {
                    case AdType.Interstitial:
                        isReady = Chartboost.hasInterstitial(CBLocation.Default);
                        break;
                    case AdType.Incentivized:
                        isReady = Chartboost.hasRewardedVideo(CBLocation.Default);
                        break;
                }
                return isReady;
            }


            // Interstitial

            void DidCacheInterstitial(CBLocation location) {
                AddEvent(AdType.Interstitial, AdEvent.Prepared);
            }
        
            void DidFailToLoadInterstitial(CBLocation location, CBImpressionError error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[ChartboostAdapter] DidFailToLoadInterstitial error:" + error.ToString());
#endif
                AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
            }

            bool ShouldDisplayInterstitial(CBLocation location) {
                AddEvent(AdType.Interstitial, AdEvent.Show);
                bool showInterstitial = true;
                return showInterstitial;
            }

            void DidCloseInterstitial(CBLocation location) {
            }

            void DidDismissInterstitial(CBLocation location) {
                AddEvent(AdType.Interstitial, AdEvent.Hide);
            }

            // Reward Video

            void DidCacheRewardedVideo(CBLocation location) {
                AddEvent(AdType.Incentivized, AdEvent.Prepared);
            }

            void DidFailToLoadRewardedVideo(CBLocation location, CBImpressionError error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[ChartboostAdapter] DidFailToLoadRewardedVideo error:" + error.ToString());
#endif
                AddEvent(AdType.Incentivized, AdEvent.PrepareFailure);
            }

            bool shouldDisplayRewardedVideo(CBLocation location) {
                AddEvent(AdType.Incentivized, AdEvent.Show);
                bool showIncentivized = true;
                return showIncentivized;
            }

            void DidCloseRewardedVideo(CBLocation location) {            
            }

            void DidCompleteRewardedVideo(CBLocation location, int count) {
                AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
            }

            void DidDismissRewardedVideo(CBLocation location) {
                AddEvent(AdType.Incentivized, AdEvent.Hide);
            }

        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_CHARTBOOST