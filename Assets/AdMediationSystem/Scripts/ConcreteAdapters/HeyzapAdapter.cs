
//#define _MS_HEYZAP

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if _MS_HEYZAP
using Heyzap;
#endif

namespace Virterix {
    namespace AdMediation {

        public class HeyzapAdapter : AdNetworkAdapter {

            public string m_defaultAndroidPublisherId;
            public string m_defaultIOSPublisherId;

#if _MS_HEYZAP

            string m_publisherID;

            void Awake() {
                HZInterstitialAd.SetDisplayListener(AdInterstitialListener);
                HZIncentivizedAd.SetDisplayListener(AdIncentivizedListener);
            }

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);
                string publisherID = "";

                if (parameters != null) {
                    m_publisherID = parameters["publisherID"];
                } else {
#if UNITY_ANDROID
                    m_publisherID = m_defaultAndroidPublisherId;
#elif UNITY_IOS
                    m_publisherID = m_defaultIOSPublisherId;
#endif
                }
            }

            public override void Initialize(Dictionary<string, string> parameters = null) {
                base.Initialize(parameters);
                HeyzapAds.Start(m_publisherID, HeyzapAds.FLAG_DISABLE_AUTOMATIC_FETCHING);
            }

            public override void Prepare(AdType adType) {
                switch (adType) {
                    case AdType.Interstitial:
                        HZInterstitialAd.Fetch();
                        break;
                    case AdType.Incentivized:
                        HZIncentivizedAd.Fetch();
                        break;
                }
            }

            public override bool Show(AdType adType) {
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            HZInterstitialAd.Show();
                            break;
                        case AdType.Incentivized:
                            HZIncentivizedAd.Show();
                            m_currShowingAdTypeToFixHold = AdType.Incentivized;
                            break;
                    }
                    return true;
                } else {
                    return false;
                }
            }

            public override void Hide(AdType adType) {
            }

            public override bool IsReady(AdType adType) {
                switch (adType) {
                    case AdType.Interstitial:
                        return HZInterstitialAd.IsAvailable();
                    case AdType.Incentivized:
                        return HZIncentivizedAd.IsAvailable();
                    default:
                        return false;
                }
            }

            void AdInterstitialListener(string adState, string adTag) {
                if (adState.Equals("show")) {
                    // Do something when the ad shows, like pause your game
                    AddEvent(AdType.Interstitial, AdEvent.Show);
                }
                if (adState.Equals("hide")) {
                    // Do something after the ad hides itself
                    AddEvent(AdType.Interstitial, AdEvent.Hide);
                }
                if (adState.Equals("click")) {
                    // Do something when an ad is clicked on
                    AddEvent(AdType.Interstitial, AdEvent.Click);
                }
                if (adState.Equals("failed")) {
                    // Do something when an ad fails to show
                    AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
                }
                if (adState.Equals("available")) {
                    // Do something when an ad has successfully been fetched
                    AddEvent(AdType.Interstitial, AdEvent.Prepared);
                }
                if (adState.Equals("fetch_failed")) {
                    // Do something when an ad did not fetch
                    AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
                }
                if (adState.Equals("audio_starting")) {
                    // The ad being shown will use audio. Mute any background music
                }
                if (adState.Equals("audio_finished")) {
                    // The ad being shown has finished using audio.
                    // You can resume any background music.
                }
            }
            
            void AdIncentivizedListener(string adState, string adTag) {
                if (adState.Equals("show")) {
                    // Do something when the ad shows, like pause your game
                    AddEvent(AdType.Incentivized, AdEvent.Show);
                }
                if (adState.Equals("hide")) {
                    // Do something after the ad hides itself
                    this.AddEvent(AdType.Incentivized, AdEvent.Hide);
                    if (m_currShowingAdTypeToFixHold == AdType.Incentivized) {
                        m_currShowingAdTypeToFixHold = AdType.None;
                    }
                }
                if (adState.Equals("click")) {
                    // Do something when an ad is clicked on
                    AddEvent(AdType.Incentivized, AdEvent.Click);
                }
                if (adState.Equals("failed")) {
                    // Do something when an ad fails to show
                    AddEvent(AdType.Incentivized, AdEvent.PrepareFailure);
                    if (m_currShowingAdTypeToFixHold == AdType.Incentivized) {
                        m_currShowingAdTypeToFixHold = AdType.None;
                    }
                }
                if (adState.Equals("available")) {
                    // Do something when an ad has successfully been fetched
                    AddEvent(AdType.Incentivized, AdEvent.Prepared);
                    if (m_currShowingAdTypeToFixHold == AdType.Incentivized) {
                        m_currShowingAdTypeToFixHold = AdType.None;
                    }
                }
                if (adState.Equals("fetch_failed")) {
                    // Do something when an ad did not fetch
                }
                if (adState.Equals("audio_starting")) {
                    // The ad being shown will use audio. Mute any background music           
                }
                if (adState.Equals("audio_finished")) {
                    // The ad being shown has finished using audio.
                    // You can resume any background music.
                }
                if (adState.Equals("incentivized_result_complete")) {
                    // The user has watched the entire video and should be given a reward.
                    AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
                }
                if (adState.Equals("incentivized_result_incomplete")) {
                    // The user did not watch the entire video and should not be given a reward.
                    AddEvent(AdType.Incentivized, AdEvent.IncentivizedIncomplete);
                }
            }

#endif  // _MS_HEYZAP

        }

    } // namespace AdMediation
} // namespace Virterix

