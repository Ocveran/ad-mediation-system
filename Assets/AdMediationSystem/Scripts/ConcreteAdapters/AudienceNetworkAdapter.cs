
#define _MS_AUDIENCE_NETWORK

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

#if _MS_AUDIENCE_NETWORK
using AudienceNetwork;
#endif

namespace Virterix {
    namespace AdMediation {
        
        public class AudienceNetworkAdapter : AdNetworkAdapter {

            public const string _PLACEMENT_PARAMETERS_FOLDER = "AudienceNetwork";
            public const string _BANNER_ID_KEY = "bannerPlacementId";
            public const string _INTERSTITIAL_ID_KEY = "interstitialPlacementId";
            public const string _REWARD_VIDEO_ID_KEY = "rewardVideoPlacementId";
            public const string _NATIVE_ID_KEY = "nativePlacementId";
            public const string _BANNER_REFRESH_TIME_KEY = "bannerRefreshTime";
            public const string _NATIVE_REFRESH_TIME_KEY = "nativeRefreshTime";
            public const string _REFRESH_TIME_KEY = "refreshTime";

            public enum AudienceNetworkBannerSize {
                BANNER_HEIGHT_50,
                BANNER_HEIGHT_90,
                RECTANGLE_HEIGHT_250,
                CUSTOM
            }

            public enum AudienceNetworkBannerPosition {
                Bottom,
                Top
            }

            [System.Serializable]
            public struct AudienceNetworkIDs {
                public string m_bannerPlacementId;
                public string m_interstitialPlacementId;
                public string m_rewardVideoPlacementId;
                public string m_nativePlacementId;
            }

            [SerializeField]
            public AudienceNetworkIDs m_defaultAndroidIDs;
            [SerializeField]
            public AudienceNetworkIDs m_defaultIOSIDs;
            [Tooltip("In Seconds")]
            public float m_defaultBannerRefreshTime = 60f;
            [Tooltip("In Seconds")]
            public float m_defaultNativeRefreshTime = 60f;

            protected override string PlacementParametersFolder {
                get {
                    return _PLACEMENT_PARAMETERS_FOLDER + "/";
                }
            }

#if _MS_AUDIENCE_NETWORK

            class AudienceNetworkPlacementData : PlacementData {
                public AudienceNetworkPlacementData() : base() {
                }
                public AudienceNetworkPlacementData(AdType adType, string adID, string placementName = _PLACEMENT_DEFAULT_NAME) : 
                    base(adType, adID, placementName) {
                }
                public Coroutine m_procRefresh;
                public float m_refreshTime;
                public AudienceNetworkNativeAdPanel m_nativePanel;
            }
            
            string m_bannerPlacementId;
            string m_interstitialPlacementId;
            string m_rewardVideoPlacementId;
            string m_nativePlacementId;

            RewardedVideoAd m_rewardVideo;
            //NativeAd m_nativeAd;

            // Default Placements
            AudienceNetworkPlacementData m_bannerPlacement;
            AudienceNetworkPlacementData m_interstitialPlacement;
            AudienceNetworkPlacementData m_rewardVideoPlacement;
            AudienceNetworkPlacementData m_nativePlacement;

            protected override void InitializeParameters(Dictionary<string, string> parameters, JSONArray jsonPlacements) {
                base.InitializeParameters(parameters, jsonPlacements);
                
                float bannerRefreshTime = m_defaultBannerRefreshTime;
                float nativeRefreshTime = m_defaultNativeRefreshTime;

                if (parameters != null) {
                    if (!parameters.TryGetValue(_BANNER_ID_KEY, out m_bannerPlacementId)) {
                        m_bannerPlacementId = "";
                    }
                    if (!parameters.TryGetValue(_INTERSTITIAL_ID_KEY, out m_interstitialPlacementId)) {
                        m_interstitialPlacementId = "";
                    }
                    if (!parameters.TryGetValue(_REWARD_VIDEO_ID_KEY, out m_rewardVideoPlacementId)) {
                        m_rewardVideoPlacementId = "";
                    }
                    if (!parameters.TryGetValue(_NATIVE_ID_KEY, out m_nativePlacementId)) {
                        m_nativePlacementId = "";
                    }

                    if (parameters.ContainsKey(_BANNER_REFRESH_TIME_KEY)) {
                        bannerRefreshTime = Convert.ToInt32(parameters[_BANNER_REFRESH_TIME_KEY]);
                    }
                    if (parameters.ContainsKey(_NATIVE_REFRESH_TIME_KEY)) {
                        nativeRefreshTime = Convert.ToInt32(parameters[_NATIVE_REFRESH_TIME_KEY]);
                    }
                }
                else {
#if UNITY_ANDROID
                    m_bannerPlacementId = m_defaultAndroidIDs.m_bannerPlacementId;
                    m_interstitialPlacementId = m_defaultAndroidIDs.m_interstitialPlacementId;
                    m_rewardVideoPlacementId = m_defaultAndroidIDs.m_rewardVideoPlacementId;
                    m_nativePlacementId = m_defaultAndroidIDs.m_nativePlacementId;
#elif UNITY_IOS
                    m_bannerPlacementId = m_defaultIOSParams.m_bannerPlacementId;
                    m_interstitialPlacementId = m_defaultIOSParams.m_interstitialPlacementId;
                    m_rewardVideoPlacementId = m_defaultIOSParams.m_rewardVideoPlacementId;
                    m_nativePlacementId = m_defaultIOSParams.m_nativePlacementId;
#endif
                }

                if (m_bannerPlacementId.Length > 0) {
                    m_bannerPlacement = new AudienceNetworkPlacementData(AdType.Banner, m_bannerPlacementId);
                    m_bannerPlacement.m_placementParams = GetPlacementParams(AdType.Banner, _PLACEMENT_DEFAULT_NAME);

                    if (bannerRefreshTime > 0) {
                        m_bannerPlacement.m_refreshTime = bannerRefreshTime;
                    }
                    AddPlacement(m_bannerPlacement);
                }

                if (m_interstitialPlacementId.Length > 0) {
                    m_interstitialPlacement = new AudienceNetworkPlacementData(AdType.Interstitial, m_interstitialPlacementId);
                    AddPlacement(m_interstitialPlacement);
                }

                if (m_rewardVideoPlacementId.Length > 0) {
                    m_rewardVideoPlacement = new AudienceNetworkPlacementData(AdType.Incentivized, m_rewardVideoPlacementId);
                    AddPlacement(m_rewardVideoPlacement);
                }

                if (m_nativePlacementId.Length > 0) {
                    m_nativePlacement = new AudienceNetworkPlacementData(AdType.Native, m_nativePlacementId);
                    if (nativeRefreshTime > 0) {
                        m_nativePlacement.m_refreshTime = nativeRefreshTime;
                    }
                    AddPlacement(m_nativePlacement);
                }

                //AdSettings.AddTestDevice("54308c7d-68d1-420f-8542-321aa8949c8e");
            }

            protected override void InitializePlacementData(PlacementData placementData, JSONValue jsonPlacementData) {
                base.InitializePlacementData(placementData, jsonPlacementData);

                AudienceNetworkPlacementData anPlacementData = placementData as AudienceNetworkPlacementData;
                anPlacementData.m_refreshTime = m_defaultBannerRefreshTime;
                
                if (jsonPlacementData.Obj.ContainsKey(_REFRESH_TIME_KEY)) {
                    anPlacementData.m_refreshTime = Convert.ToInt32(jsonPlacementData.Obj.GetNumber(_REFRESH_TIME_KEY));
                }
            }

            protected override PlacementData CreatePlacementData(JSONValue jsonPlacementData) {
                PlacementData placementData = new AudienceNetworkPlacementData();
                return placementData;
            }

            public override void Prepare(AdType adType, PlacementData placement = null) {
                AudienceNetworkPlacementData anPlacementData = placement as AudienceNetworkPlacementData;

                if (GetAdState(adType, anPlacementData) != AdState.Loading) {
                    switch (adType) {
                        case AdType.Banner:
                            RequestBanner(anPlacementData);
                            break;
                        case AdType.Interstitial:
                            RequestInterstitial(anPlacementData);
                            break;
                        case AdType.Incentivized:
                            RequestRewardVideo(anPlacementData);
                            break;
                        case AdType.Native:
                            RequestNativeAd(anPlacementData);
                            break;
                    }
                }
            }

            public override bool Show(AdType adType, PlacementData placement = null) {
                AudienceNetworkPlacementData anPlacementData = placement == null ? null : placement as AudienceNetworkPlacementData;
                
                bool showSuccess = false;
                switch (adType) {
                    case AdType.Banner:
                        anPlacementData.m_isBannerAdTypeVisibled = true;

                        if (GetAdState(adType, anPlacementData) == AdState.Available) {
                            AdView bannerView = anPlacementData.m_adView as AdView;
                            Vector2 bannerPosition = anPlacementData.m_bannerCoordinates;
                            showSuccess = bannerView.Show(bannerPosition.x, bannerPosition.y);
                            if (showSuccess) {
                                NotifyEvent(adType, AdEvent.Show, anPlacementData);
                            }
                        }
                        break;
                    case AdType.Interstitial:
                        if (GetAdState(adType, anPlacementData) == AdState.Available) {
                            InterstitialAd interstitialAd = anPlacementData.m_adView as InterstitialAd;
                            showSuccess = interstitialAd.Show();
                            if (showSuccess) {
                                NotifyEvent(AdType.Interstitial, AdEvent.Show, anPlacementData);
                            }
                            else {
                                DestroyInterstitial(anPlacementData);
                            }
                        }
                        break;
                    case AdType.Incentivized:
                        if (GetAdState(adType, anPlacementData) == AdState.Available) {
                            RewardedVideoAd rewardVideo = anPlacementData.m_adView as RewardedVideoAd;
                            showSuccess = rewardVideo.Show();
                            if (showSuccess) {
                                NotifyEvent(AdType.Incentivized, AdEvent.Show, anPlacementData);
                            }
                            else {
                                DestroyRewardVideo(anPlacementData);
                            }
                        }
                        break;
                    case AdType.Native:
                        anPlacementData.m_isBannerAdTypeVisibled = true;
                    
                        if (GetAdState(adType, anPlacementData) == AdState.Available) {
                            NativeAd nativeAd = anPlacementData.m_adView as NativeAd;
                            if (nativeAd.IsValid()) {
                                showSuccess = true;
                                if (anPlacementData.m_nativePanel != null) {
                                    anPlacementData.m_nativePanel.Show();
                                }
                                NotifyEvent(adType, AdEvent.Show, anPlacementData);
                            }
                        }
                        break;
                }
                return showSuccess;
            }

			public override void Hide(AdType adType, PlacementData placement = null) {
                AudienceNetworkPlacementData anPlacementData = placement as AudienceNetworkPlacementData;

                switch (adType) {
                    case AdType.Banner:
                    case AdType.Native:
                        anPlacementData.m_isBannerAdTypeVisibled = false;

                        if (adType == AdType.Banner) {
                            if (GetAdState(AdType.Banner, anPlacementData) == AdState.Available) {
                                AdView bannerView = placement.m_adView as AdView;
                                bannerView.Show(-10000);
                            }
                        }
                        else {
                            if (anPlacementData.m_nativePanel != null) {
                                anPlacementData.m_nativePanel.Hide();
                            }
                        }

                        NotifyEvent(adType, AdEvent.Hide, anPlacementData);
                        break;
                }
            }

            public override void HideBannerTypeAdWithoutNotify(AdType adType, PlacementData placement = null) {
                AudienceNetworkPlacementData anPlacementData = placement as AudienceNetworkPlacementData;
                anPlacementData.m_isBannerAdTypeVisibled = false;

                switch (adType) {
                    case AdType.Banner:
                        if (GetAdState(adType, anPlacementData) == AdState.Available) {
                            AdView bannerView = placement.m_adView as AdView;
                            bannerView.Show(-10000);
                        }
                        break;
                    case AdType.Native:
                        if (anPlacementData.m_nativePanel != null) {
                            anPlacementData.m_nativePanel.Hide();
                        }
                        break;
                }
            }

            public override bool IsReady(AdType adType, PlacementData placement = null) {
                bool isReady = GetAdState(adType, placement) == AdState.Available;

                if (isReady && adType == AdType.Native) {
                    NativeAd nativeAd = placement.m_adView as NativeAd;
                    isReady = nativeAd.IsValid();
                }
                return isReady;
            }

            /// <summary>
            /// Sets native panel for placement. For reset native panel set to null. 
            /// </summary>
            public void SetNativePanel(AudienceNetworkNativeAdPanel nativePanel, PlacementData placement) {
                AudienceNetworkPlacementData anPlacementData = placement as AudienceNetworkPlacementData;
       
                if (anPlacementData != null) {
                    anPlacementData.m_nativePanel = nativePanel;
                    if (anPlacementData.m_adView != null) {
                        NativeAd nativeAd = anPlacementData.m_adView as NativeAd;
                        if (nativePanel == null) {
                            anPlacementData.m_nativePanel.SetNativeAd(null);
                        }
                        else {
                            nativePanel.SetNativeAd(nativeAd);
                        }
                    }
                }
                else {
                    Debug.LogWarning("AudienceNetworkAdapter.SetNativePanel() Placement data is null!");
                }

                if (nativePanel != null) {
                    if (placement.m_isBannerAdTypeVisibled) {
                        nativePanel.Show();
                    }
                    else {
                        nativePanel.Hide(true);
                    }
                }
            }

            public void SetNativePanel(AudienceNetworkNativeAdPanel nativePanel, string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
                PlacementData placementData = GetPlacementData(AdType.Native, placement);
                SetNativePanel(nativePanel, placementData);
            }

            void LoadNativeContent(NativeAd nativeAd) {
                StartCoroutine(nativeAd.LoadIconImage(nativeAd.IconImageURL));
                StartCoroutine(nativeAd.LoadCoverImage(nativeAd.CoverImageURL));
                StartCoroutine(nativeAd.LoadAdChoicesImage(nativeAd.AdChoicesImageURL));
            }

            AdSize ConvertToAdSize(AudienceNetworkBannerSize bannerSize) {
                AdSize nativeAdSize = AdSize.BANNER_HEIGHT_50;
                switch (bannerSize) {
                    case AudienceNetworkBannerSize.BANNER_HEIGHT_50:
                        nativeAdSize = AdSize.BANNER_HEIGHT_50;
                        break;
                    case AudienceNetworkBannerSize.BANNER_HEIGHT_90:
                        nativeAdSize = AdSize.BANNER_HEIGHT_90;
                        break;
                    case AudienceNetworkBannerSize.RECTANGLE_HEIGHT_250:
                        nativeAdSize = AdSize.RECTANGLE_HEIGHT_250;
                        break;
                }
                return nativeAdSize;
            }

            void CalculateBannerPosition(AudienceNetworkPlacementData placement) {

                float bannerHight = 0;
                AudienceNetworkBannerPlacementParameters placementParams = placement.m_placementParams as AudienceNetworkBannerPlacementParameters;

                switch (placementParams.m_bannerSize) {
                    case AudienceNetworkBannerSize.BANNER_HEIGHT_50:
                        bannerHight = 50f;
                        break;
                    case AudienceNetworkBannerSize.BANNER_HEIGHT_90:
                        bannerHight = 90f;
                        break;
                    case AudienceNetworkBannerSize.RECTANGLE_HEIGHT_250:
                        bannerHight = 250f;
                        break;
                }

                Vector2 bannerCoordinates = Vector2.zero;

                switch (placementParams.m_bannerPosition) {
                    case AudienceNetworkBannerPosition.Bottom:
                        bannerCoordinates.x = 0f;
#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
                        bannerCoordinates.y = (float)AudienceNetwork.Utility.AdUtility.height() - bannerHight;
#endif
                        break;
                    case AudienceNetworkBannerPosition.Top:
                        bannerCoordinates.x = 0f;
                        bannerCoordinates.y = 0f;
                        break;
                }

                placement.m_bannerCoordinates = bannerCoordinates;
            }

            IEnumerator ProcRefreshBannerAdType(AdType adType, AudienceNetworkPlacementData placement, float refreshTime) {
                float lifeTime = 0.0f;
                float period = 0.8f;

                while (true) {
                    yield return new WaitForSecondsRealtime(period);
                    if (placement.m_state == AdState.Available && placement.m_isBannerAdTypeVisibled) {
                        lifeTime += period;
                    }

                    if (lifeTime >= refreshTime) {
                        switch (adType) {
                            case AdType.Banner:
                                lifeTime = 0.0f;
                                if (placement.m_adView != null) {
                                    AdView adView = placement.m_adView as AdView;
                                    adView.LoadAd();
                                }
                                break;
                            case AdType.Native:
                                if (!placement.m_isBannerAdTypeVisibled) {
                                    lifeTime = 0.0f;
                                    if (placement.m_adView != null) {
                                        DestroyNativeAd(placement);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
  
            void RequestBanner(AudienceNetworkPlacementData placement) {
                DestroyBanner(placement);
                CalculateBannerPosition(placement);
                SetAdState(AdType.Banner, placement, AdState.Loading);
                placement.m_procRefresh = StartCoroutine(
                    ProcRefreshBannerAdType(AdType.Banner, placement, placement.m_refreshTime));

#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
                // Create a banner's ad view with a unique placement ID (generate your own on the Facebook app settings).
                // Use different ID for each ad placement in your app.

                AudienceNetworkBannerPlacementParameters placementParams = placement.m_placementParams as AudienceNetworkBannerPlacementParameters;
                AdView bannerView = new AdView(placement.m_adID, ConvertToAdSize(placementParams.m_bannerSize));
                placement.m_adView = bannerView;
                bannerView.Register(this.gameObject);
                bannerView.DisableAutoRefresh();

                bannerView.AdViewDidLoad += delegate { BannerAdViewDidLoad(placement); };
                bannerView.AdViewDidFailWithError += delegate(string error) { BannerAdViewDidFailWithError(placement, error); };
                bannerView.AdViewWillLogImpression += delegate { BannerAdViewWillLogImpression(placement); };
                bannerView.AdViewDidClick += delegate { BannerAdViewDidClick(placement); };
                bannerView.LoadAd();
#endif
            }

            void DestroyBanner(AudienceNetworkPlacementData placement) {

                if (placement.m_procRefresh != null) {
                    StopCoroutine(placement.m_procRefresh);
                    placement.m_procRefresh = null;
                }

                AdView bannerView = placement.m_adView as AdView;
                placement.m_adView = null;

                if (bannerView != null) {
                    bannerView.AdViewDidLoad = null;
                    bannerView.AdViewDidFailWithError = null;
                    bannerView.AdViewWillLogImpression = null;
                    bannerView.AdViewDidClick = null;
                    if (GetAdState(AdType.Banner, placement) == AdState.Available) {
                        bannerView.Dispose();
                    }
                    SetAdState(AdType.Banner, placement, AdState.Uncertain);
                }
            }

            void RequestInterstitial(AudienceNetworkPlacementData placement) {
                DestroyInterstitial(placement);
                SetAdState(AdType.Interstitial, placement, AdState.Loading);
#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
                // Create the interstitial unit with a placement ID (generate your own on the Facebook app settings).
                // Use different ID for each ad placement in your app.
                InterstitialAd interstitialAd = new InterstitialAd(placement.m_adID);
                interstitialAd.Register(this.gameObject);

                interstitialAd.InterstitialAdDidLoad += delegate { InterstitialAdDidLoad(placement); };
                interstitialAd.InterstitialAdDidFailWithError += delegate(string error) { InterstitialAdDidFailWithError(placement, error); };
                interstitialAd.InterstitialAdDidClose += delegate { InterstitialAdDidClose(placement); };
                interstitialAd.InterstitialAdDidClick += delegate { InterstitialAdDidClick(placement); };

                // Initiate the request to load the ad.
                interstitialAd.LoadAd();
                placement.m_adView = interstitialAd;
#endif
            }

            void DestroyInterstitial(AudienceNetworkPlacementData placement) {
                if (placement.m_adView != null) {
                    InterstitialAd interstitialAd = placement.m_adView as InterstitialAd;
                    placement.m_adView = null;

                    interstitialAd.InterstitialAdDidLoad = null;
                    interstitialAd.InterstitialAdDidFailWithError = null;
                    interstitialAd.InterstitialAdDidClose = null;
                    interstitialAd.InterstitialAdDidClick = null;

#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("AudienceNetworkAdapter.DestroyInterstitial() state:" + GetAdState(AdType.Interstitial, placement));
#endif

                    if (GetAdState(AdType.Interstitial, placement) == AdState.Available) {
                        interstitialAd.Dispose();
                    }
                    SetAdState(AdType.Interstitial, placement, AdState.Uncertain);
                }
            }

            void RequestRewardVideo(AudienceNetworkPlacementData placement) {
                DestroyRewardVideo(placement);
                SetAdState(AdType.Incentivized, placement, AdState.Loading);
#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
                // Create the interstitial unit with a placement ID (generate your own on the Facebook app settings).
                // Use different ID for each ad placement in your app.
                RewardedVideoAd rewardVideo = new RewardedVideoAd(placement.m_adID);
                placement.m_adView = rewardVideo;
                //m_rewardVideo.Register(this.gameObject);

                rewardVideo.RewardedVideoAdDidLoad += delegate { RewardedVideoAdDidLoad(placement); };
                rewardVideo.RewardedVideoAdDidFailWithError += delegate(string error) { RewardedVideoAdDidFailWithError(placement, error); };
                rewardVideo.RewardedVideoAdComplete += delegate { RewardedVideoAdComplete(placement); };
                rewardVideo.RewardedVideoAdDidClick += delegate { RewardedVideoAdDidClick(placement); };
                rewardVideo.RewardedVideoAdDidClose += delegate { RewardedVideoAdDidClose(placement); };

                // Initiate the request to load the ad.
                rewardVideo.LoadAd();
#endif
            }

            void DestroyRewardVideo(AudienceNetworkPlacementData placement) {
                if (placement.m_adView != null) {
                    RewardedVideoAd rewardVideo = placement.m_adView as RewardedVideoAd;
                    placement.m_adView = null;

                    rewardVideo.RewardedVideoAdDidLoad = null;
                    rewardVideo.RewardedVideoAdDidFailWithError = null;
                    rewardVideo.RewardedVideoAdComplete = null;
                    rewardVideo.RewardedVideoAdDidClick = null;
                    rewardVideo.RewardedVideoAdDidClose = null;
                    if (GetAdState(AdType.Incentivized, placement) == AdState.Available) {
                        rewardVideo.Dispose();
                    }
                    SetAdState(AdType.Incentivized, placement, AdState.Uncertain);
                }
            }

            void RequestNativeAd(AudienceNetworkPlacementData placement) {
                DestroyNativeAd(placement);
                SetAdState(AdType.Native, placement, AdState.Loading);

                placement.m_procRefresh = StartCoroutine(
                    ProcRefreshBannerAdType(AdType.Native, placement, placement.m_refreshTime));

#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
                NativeAd nativeAd = new NativeAd(placement.m_adID);
                placement.m_adView = nativeAd;
                if (placement.m_nativePanel != null) {
                    placement.m_nativePanel.SetNativeAd(nativeAd);
                }
                
                nativeAd.NativeAdDidLoad = (delegate { NativeAdDidLoad(placement); });
                nativeAd.NativeAdDidFailWithError = (delegate (string error) { NativeAdDidFailWithError(placement, error); });
                nativeAd.NativeAdDidClick = (delegate { NativeAdDidClick(placement); });
                nativeAd.NativeAdWillLogImpression = (delegate { NativeAdWillLogImpression(placement); });
   
                // Initiate the request to load the ad.
                nativeAd.LoadAd();

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RequestNativeAd() " + nativeAd.IsValid());
#endif
#endif
            }

            void DestroyNativeAd(AudienceNetworkPlacementData placement) {
                NativeAd nativeAd = placement.m_adView as NativeAd;
                placement.m_adView = null;

                if (placement.m_procRefresh != null) {
                    StopCoroutine(placement.m_procRefresh);
                    placement.m_procRefresh = null;
                }

                if (placement.m_nativePanel != null) {
                    placement.m_nativePanel.SetNativeAd(null);
                }

                if (nativeAd != null) {
#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("AudienceNetworkAdapter.DestroyNativeAd() IsValid:" + nativeAd.IsValid());
#endif

                    nativeAd.NativeAdDidLoad = null;
                    nativeAd.NativeAdDidClick = null;
                    nativeAd.NativeAdDidFailWithError = null;
                    nativeAd.NativeAdWillLogImpression = null;
                    if (GetAdState(AdType.Native, placement) == AdState.Available) {
                        nativeAd.Dispose();
                    }
                    SetAdState(AdType.Native, placement, AdState.Uncertain);
                }
            }

            //------------------------------------------------------------------------
            #region Banner callback handlers

            void BannerAdViewDidLoad(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.BannerAdViewDidLoad() placement:" + placement.PlacementName);
#endif

                SetAdState(AdType.Banner, placement, AdState.Available);
				if (placement.m_isBannerAdTypeVisibled && placement.m_adView != null) {
                    AudienceNetworkBannerPlacementParameters placementParams = placement.m_placementParams as AudienceNetworkBannerPlacementParameters;
                    AdView bannerView = placement.m_adView as AdView;
                    Vector2 bannerPosition = placement.m_bannerCoordinates;
                    bool success = bannerView.Show(bannerPosition.x, bannerPosition.y);
                }
                AddEvent(AdType.Banner, AdEvent.Prepared, placement);
            }

            void BannerAdViewDidFailWithError(AudienceNetworkPlacementData placement, string error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.BannerAdViewDidFailWithError() placement:" + placement.PlacementName +  " error: " + error );
#endif
                DestroyBanner(placement);
                AddEvent(AdType.Banner, AdEvent.PrepareFailure, placement);
            }

            void BannerAdViewWillLogImpression(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.BannerAdViewWillLogImpression()");
#endif
            }

            void BannerAdViewDidClick(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.BannerAdViewDidClick()");
#endif
                AddEvent(AdType.Banner, AdEvent.Click, placement);
            }

            #endregion //Banner callback handlers

            //------------------------------------------------------------------------
            #region Interstitial callback handlers

            void InterstitialAdDidLoad(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.InterstitialAdDidLoad()");
#endif
                SetAdState(AdType.Interstitial, placement, AdState.Available);
                AddEvent(AdType.Interstitial, AdEvent.Prepared, placement);
            }

            void InterstitialAdDidFailWithError(AudienceNetworkPlacementData placement, string error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.InterstitialAdDidFailWithError() error: " + error);
#endif
                DestroyInterstitial(placement);
                AddEvent(AdType.Interstitial, AdEvent.PrepareFailure, placement);
            }

            void InterstitialAdDidClose(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.InterstitialAdDidClose()");
#endif
                DestroyInterstitial(placement);
                AddEvent(AdType.Interstitial, AdEvent.Hide, placement);
            }

            void InterstitialAdDidClick(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.InterstitialAdDidClick()");
#endif
                AddEvent(AdType.Interstitial, AdEvent.Click, placement);
            }

            #endregion // Interstitial callback handlers

            //------------------------------------------------------------------------
            #region Reward Video callback handlers

            void RewardedVideoAdDidLoad(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RewardedVideoAdDidLoad()");
#endif
                SetAdState(AdType.Incentivized, placement, AdState.Available);
                AddEvent(AdType.Incentivized, AdEvent.Prepared, placement);
            }

            void RewardedVideoAdDidFailWithError(AudienceNetworkPlacementData placement, string error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RewardedVideoAdDidFailWithError() error: " + error);
#endif
                DestroyRewardVideo(placement);
                AddEvent(AdType.Incentivized, AdEvent.PrepareFailure, placement);
            }

            void RewardedVideoAdDidClick(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RewardedVideoAdDidClick()");
#endif
                AddEvent(AdType.Incentivized, AdEvent.Click, placement);
            }

            void RewardedVideoAdDidClose(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RewardedVideoAdDidClose()");
#endif
                DestroyRewardVideo(placement);
                AddEvent(AdType.Incentivized, AdEvent.Hide, placement);
            }

            void RewardedVideoAdComplete(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.RewardedVideoAdComplete()");
#endif
                DestroyRewardVideo(placement);
                AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete, placement);
            }

            #endregion // Reward Video callback handlers

            //------------------------------------------------------------------------
            #region Native Ad callback handlers

            void NativeAdDidLoad(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.NativeAdDidLoad() placement:" + placement.PlacementName);
#endif

                SetAdState(AdType.Native, placement, AdState.Available);

                NativeAd nativeAd = placement.m_adView as NativeAd;
                if (nativeAd != null) {
                    LoadNativeContent(nativeAd);
                }

                if (placement.m_nativePanel != null) {
                    placement.m_nativePanel.RefreshTexts();

                    if (placement.m_isBannerAdTypeVisibled) {
                        placement.m_nativePanel.Show();
                    }
                    else {
                        placement.m_nativePanel.Hide();
                    }
                }

                AddEvent(AdType.Native, AdEvent.Prepared, placement);
            }

            void NativeAdDidFailWithError(AudienceNetworkPlacementData placement, string error) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.NativeAdDidFailWithError() placement:" + placement.PlacementName + " error:" + error);
#endif

                DestroyNativeAd(placement);
                AddEvent(AdType.Native, AdEvent.PrepareFailure, placement);
            }

            void NativeAdWillLogImpression(AudienceNetworkPlacementData placement) {
#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AudienceNetworkAdapter.NativeAdWillLogImpression() placement:" + placement.PlacementName);
#endif
            }

            void NativeAdDidClick(AudienceNetworkPlacementData placement) {

            }

            #endregion // Native Ad callback handlers


#endif // _MS_AUDIENCE_NETWORK

            }

        } // namespace AdMediation
} // namespace Virterix

