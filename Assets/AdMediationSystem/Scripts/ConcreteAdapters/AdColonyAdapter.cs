
#define _MS_ADCOLONY

using UnityEngine;
using System.Collections.Generic;
using Boomlagoon.JSON;

#if _MS_ADCOLONY
using AdColony;
#endif

namespace Virterix {
    namespace AdMediation {

        public class AdColonyAdapter : AdNetworkAdapter {

            public enum AdColonyOrientationType {
                All,
                Landscape,
                Portrait
            }

            [System.Serializable]
            public struct AdColonyParameters {
                public string m_appId;
                public string m_interstitialZoneId;
                public string m_rewardZoneId;
            }

            public AdColonyOrientationType m_orientation;
            [SerializeField]
            public AdColonyParameters m_defaultAndroidParams;
            [SerializeField]
            public AdColonyParameters m_defaultIOSParams;

            public bool m_useRewardVideoPrePopup;
            public bool m_useRewardVideoPostPopup;

#if _MS_ADCOLONY

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

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters, JSONArray jsonPlacements) {
                base.InitializeParameters(parameters, jsonPlacements);
                
#if UNITY_EDITOR
                m_isConfigured = true;
#endif

                m_adOrientation = ConvertOrientationType(m_orientation);

                if (parameters != null) {
                    m_appId = parameters["appId"];
                    m_interstitialZoneId = parameters["interstitialZoneId"];
                    m_rewardZoneId = parameters["rewardZoneId"];
                } else {
#if UNITY_ANDROID
                    m_appId = m_defaultAndroidParams.m_appId;
                    m_interstitialZoneId = m_defaultAndroidParams.m_interstitialZoneId;
                    m_rewardZoneId = m_defaultAndroidParams.m_rewardZoneId;
#elif UNITY_IOS
                       m_appId = m_defaultIOSParams.m_appId;
                       m_interstitialZoneId = m_defaultIOSParams.m_interstitialZoneId;
                       m_rewardZoneId = m_defaultIOSParams.m_rewardZoneId;
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

            public AdOrientationType ConvertOrientationType(AdColonyOrientationType orientationType) {

                AdOrientationType convertedOrientationType = AdOrientationType.AdColonyOrientationAll;
                switch (orientationType) {
                    case AdColonyOrientationType.All:
                        convertedOrientationType = AdOrientationType.AdColonyOrientationAll;
                        break;
                    case AdColonyOrientationType.Landscape:
                        convertedOrientationType = AdOrientationType.AdColonyOrientationLandscape;
                        break;
                    case AdColonyOrientationType.Portrait:
                        convertedOrientationType = AdOrientationType.AdColonyOrientationPortrait;
                        break;
                }
                return convertedOrientationType;
            }

            public override void Prepare(AdType adType, PlacementData placementData = null) {

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

            public override bool Show(AdType adType, PlacementData placementData = null) {
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

			public override void Hide(AdType adType, PlacementData placementData = null) {
            }

            public override bool IsShouldInternetCheckBeforeShowAd(AdType adType) {
                bool shouldCheck = adType == AdType.Incentivized;
                return shouldCheck;
            }

            public override bool IsReady(AdType adType, PlacementData placementData = null) {
                bool isReady = false;

                if (m_isConfigured) {
                    switch (adType) {
                        case AdType.Interstitial:
                            isReady = m_videoInterstitial != null ? !m_videoInterstitial.Expired : false;
                            break;
                        case AdType.Incentivized:
                            isReady = m_incentivizedInterstitial != null ? !m_incentivizedInterstitial.Expired : false;
                            break;
                    }
                }

                return isReady;
            }

            public override void ResetAd(AdType adType, PlacementData placementData = null) {
                switch(adType) {
                    case AdType.Interstitial:
                        m_videoInterstitial = null;
                        break;
                    case AdType.Incentivized:
                        m_incentivizedInterstitial = null;
                        break;
                }
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

#if !UNITY_EDITOR
                Ads.Configure(m_appId, appOptions, zoneIDs.ToArray());
#endif
            }

            void RequestAd(string zoneId, bool showPrePopup = false, bool showPostPopup = false) {
                AdOptions adOptions = new AdColony.AdOptions();
                adOptions.ShowPrePopup = showPrePopup;
                adOptions.ShowPostPopup = showPostPopup;
#if !UNITY_EDITOR
                Ads.RequestInterstitialAd(zoneId, adOptions);
#endif
            }

            void SubscribeAdEvents() {
                Ads.OnConfigurationCompleted += OnConfigurationCompleted;
                Ads.OnOpened += OnOpened;
                Ads.OnClosed += OnClosed;
                Ads.OnRequestInterstitial += OnRequestInterstitial;
                Ads.OnRequestInterstitialFailed += OnRequestInterstitialFailed;
                Ads.OnRequestInterstitialFailedWithZone += OnRequestInterstitialFailedWithZone;
                Ads.OnRewardGranted += OnRewardGranted;
                Ads.OnExpiring += OnExpiring;
            }

            void UnsubscribeAdEvents() {
                Ads.OnConfigurationCompleted -= OnConfigurationCompleted;
                Ads.OnOpened -= OnOpened;
                Ads.OnClosed -= OnClosed;
                Ads.OnRequestInterstitial -= OnRequestInterstitial;
                Ads.OnRequestInterstitialFailed -= OnRequestInterstitialFailed;
                Ads.OnRequestInterstitialFailedWithZone += OnRequestInterstitialFailedWithZone;
                Ads.OnRewardGranted -= OnRewardGranted;
                Ads.OnExpiring += OnExpiring;
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

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnOpened");
#endif
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

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnClosed");
#endif
            }

            void OnRequestInterstitial(InterstitialAd interstitial) {

                if (interstitial.ZoneId == m_interstitialZoneId) {
                    m_videoInterstitial = interstitial;
                    AddEvent(AdType.Interstitial, AdEvent.Prepared);
                }
                else if (interstitial.ZoneId == m_rewardZoneId) {
                    m_incentivizedInterstitial = interstitial;
                    AddEvent(AdType.Incentivized, AdEvent.Prepared);
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnRequestInterstitial " + interstitial.ZoneId);
#endif
            }

            void OnRequestInterstitialFailed() {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnRequestInterstitialFailed");
#endif
            }

            void OnRequestInterstitialFailedWithZone(string zoneId) {
                
                if (zoneId == m_interstitialZoneId) {
                    m_videoInterstitial = null;
                    AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
                }
                else if (zoneId == m_rewardZoneId) {
                    m_incentivizedInterstitial = null;
                    AddEvent(AdType.Incentivized, AdEvent.PrepareFailure);
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnRequestInterstitialFailedWithZone: " + zoneId);
#endif
            }

            void OnExpiring(InterstitialAd interstitial) {
                if (interstitial.ZoneId == m_interstitialZoneId) {
                    m_videoInterstitial = null;
                }
                else if (interstitial.ZoneId == m_rewardZoneId) {
                    m_incentivizedInterstitial = null;
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdColonyAdapter] OnExpiring: " + interstitial.ZoneId);
#endif
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

#endif // _MS_ADCOLONY

        }

    } // namespace AdMediation
} // namespace Virterix

