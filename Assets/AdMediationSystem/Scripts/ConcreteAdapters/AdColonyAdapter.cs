
#define _MS_ADCOLONY

#if _MS_ADCOLONY

using UnityEngine;
using System.Collections.Generic;
using AdColony;

namespace Virterix {
    namespace AdMediation {

        public class AdColonyAdapter : AdNetworkAdapter {

            [System.Serializable]
            public struct AdColonyParameters {
                public string m_appId;
                public string m_interstitialZoneId;
                public string m_rewardZoneId;
                public AdOrientationType m_orientation;
            }

            [SerializeField]
            public AdColonyParameters m_defaultAndroidParams;
            [SerializeField]
            public AdColonyParameters m_defaultIOSParams;

            public bool m_useRewardVideoPrePopup;
            public bool m_useRewardVideoPostPopup;

            string m_interstitialZoneId;
            string m_rewardZoneId;
            string m_appId;
            AdOrientationType m_adOrientation;

            InterstitialAd m_videoInterstitial;
            InterstitialAd m_incentivizedInterstitial;
            bool m_isConfigured = false;


            void Awake() {
                SubscribeAdEvents();
            }

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

#if UNITY_EDITOR
                m_isConfigured = true;
#endif

                if (parameters != null) {
                    m_appId = parameters["appId"];
                    m_interstitialZoneId = parameters["interstitialZoneId"];
                    m_rewardZoneId = parameters["rewardZoneId"];
                    m_adOrientation = ConvertOrientation(parameters["orientation"]);
                } else {
#if UNITY_ANDROID
                    m_appId = m_defaultAndroidParams.m_appId;
                    m_interstitialZoneId = m_defaultAndroidParams.m_interstitialZoneId;
                    m_rewardZoneId = m_defaultAndroidParams.m_rewardZoneId;
                    m_adOrientation = m_defaultAndroidParams.m_orientation;
#elif UNITY_IOS
                       m_appId = m_defaultIOSParams.m_appId;
                       m_interstitialZoneId = m_defaultIOSParams.m_interstitialZoneId;
                       m_rewardZoneId = m_defaultIOSParams.m_rewardZoneId;
                       m_adOrientation = m_defaultIOSParams.m_orientation;
#endif
                }

                ConfigureAds();
            }

            AdOrientationType ConvertOrientation(string orientation) {
                switch(orientation) {
                    case "all":
                        return AdOrientationType.AdColonyOrientationAll;
                    case "landscape":
                        return AdOrientationType.AdColonyOrientationLandscape;
                    case "portrait":
                        return AdOrientationType.AdColonyOrientationPortrait;
                    default:
                        return AdOrientationType.AdColonyOrientationAll;
                }
            }

            public override void Prepare(AdType adType) {

                if (!m_isConfigured) {
                    AddEvent(adType, AdEvent.PrepareFailure);
                    return;
                }

                if (!IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            RequestAd(m_interstitialZoneId);
                            break;
                        case AdType.Incentivized:
                            RequestAd(m_rewardZoneId);
                            break;
                    }
                }

            }

            public override bool Show(AdType adType) {
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Interstitial:
                            Ads.ShowAd(m_videoInterstitial);
                            break;
                        case AdType.Incentivized:
                            Ads.ShowAd(m_incentivizedInterstitial);
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
                bool isReady = false;

                if (m_isConfigured) {
                    switch (adType) {
                        case AdType.Interstitial:
                            isReady = m_videoInterstitial != null;
                            //isReady = isReady ? !m_videoInterstitial.Expired : isReady;
                            break;
                        case AdType.Incentivized:
                            isReady = m_incentivizedInterstitial != null;
                            //isReady = isReady ? !m_incentivizedInterstitial.Expired : isReady;
                            break;
                    }
                }

                return isReady;
            }

            void ConfigureAds() {
                AppOptions appOptions = new AppOptions();
                appOptions.UserId = SystemInfo.deviceUniqueIdentifier;
                appOptions.AdOrientation = m_adOrientation;

                List<string> zoneIDs = new List<string>();
                if (m_interstitialZoneId != "") {
                    zoneIDs.Add(m_interstitialZoneId);
                }
                if (m_rewardZoneId != "") {
                    zoneIDs.Add(m_rewardZoneId);
                }

                Ads.Configure(m_appId, appOptions, zoneIDs.ToArray());
            }

            void RequestAd(string zoneId, bool showPrePopup = false, bool showPostPopup = false) {
                AdOptions adOptions = new AdColony.AdOptions();
                adOptions.ShowPrePopup = showPrePopup;
                adOptions.ShowPostPopup = showPostPopup;
                Ads.RequestInterstitialAd(zoneId, adOptions);
            }

            void SubscribeAdEvents() {
                Ads.OnConfigurationCompleted += OnConfigurationCompleted;
                Ads.OnOpened += OnOpened;
                Ads.OnClosed += OnClosed;
                Ads.OnRequestInterstitial += OnRequestInterstitial;
                Ads.OnRequestInterstitialFailed += OnRequestInterstitialFailed;
                Ads.OnRewardGranted += OnRewardGranted;
            }

            void UnsubscribeAdEvents() {
                Ads.OnConfigurationCompleted -= OnConfigurationCompleted;
                Ads.OnOpened -= OnOpened;
                Ads.OnClosed -= OnClosed;
                Ads.OnRequestInterstitial -= OnRequestInterstitial;
                Ads.OnRequestInterstitialFailed -= OnRequestInterstitialFailed;
                Ads.OnRewardGranted -= OnRewardGranted;
            }

            //===============================================================================
            #region Callback Event Methods
            //-------------------------------------------------------------------------------

            void OnConfigurationCompleted(List<Zone> zones) {  
                if (zones == null || zones.Count <= 0) {
                    Debug.Log("[AdColonyAdapter] Configure Failed");
                }
                else {
                    Debug.Log("[AdColonyAdapter] Configure Succeeded.");
                    m_isConfigured = true;
                }
            }

            void OnOpened(InterstitialAd interstitial) {
                if (interstitial.ZoneId == m_interstitialZoneId) {
                    AddEvent(AdType.Interstitial, AdEvent.Show);
                }
                else if (interstitial.ZoneId == m_rewardZoneId) {
                    AddEvent(AdType.Incentivized, AdEvent.Show);
                }

                Debug.Log("[AdColonyAdapter] OnOpened");
            }

            void OnClosed(InterstitialAd interstitial) {
                if (interstitial.ZoneId == m_interstitialZoneId) {
                    m_videoInterstitial = null;
                    AddEvent(AdType.Interstitial, AdEvent.Hide);
                }
                else if (interstitial.ZoneId == m_rewardZoneId) {
                    m_incentivizedInterstitial = null;
                    AddEvent(AdType.Incentivized, AdEvent.Hide);
                }

                Debug.Log("[AdColonyAdapter] OnClosed");
            }

            void OnRequestInterstitial(InterstitialAd interstitial) {
                Debug.Log("[AdColonyAdapter] OnRequestInterstitial Start");

                if (interstitial.ZoneId == m_interstitialZoneId) {
                    m_videoInterstitial = interstitial;
                    AddEvent(AdType.Interstitial, AdEvent.Prepared);
                }
                else if (interstitial.ZoneId == m_rewardZoneId) {
                    m_incentivizedInterstitial = interstitial;
                    AddEvent(AdType.Incentivized, AdEvent.Prepared);
                }

                Debug.Log("[AdColonyAdapter] OnRequestInterstitial " + interstitial.ZoneId);
            }

            void OnRequestInterstitialFailed() {
                Debug.Log("[AdColonyAdapter] OnRequestInterstitialFailed");
            }

            void OnRewardGranted(string zoneId, bool success, string name, int amount) {
                if (zoneId == m_rewardZoneId) {
                    AdEvent adEvent = success ? AdEvent.IncentivizedComplete : AdEvent.IncentivizedIncomplete;
                    AddEvent(AdType.Incentivized, adEvent);
                }
            }

            //===============================================================================
            #endregion // Callback Event Methods
            //-------------------------------------------------------------------------------

        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_ADCOLONY
