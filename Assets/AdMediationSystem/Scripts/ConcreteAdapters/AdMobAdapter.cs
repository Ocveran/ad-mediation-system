
#define _MS_ADMOB

using UnityEngine;
using System;
using System.Collections.Generic;
using Boomlagoon.JSON;

#if _MS_ADMOB
using GoogleMobileAds;
using GoogleMobileAds.Api;
#endif

namespace Virterix {
    namespace AdMediation {

        public class AdMobAdapter : AdNetworkAdapter {

            public const string _PLACEMENT_PARAMETERS_FOLDER = "AdMob";
            public const string _BANNER_ID_KEY = "bannerId";
            public const string _INTERSTITIAL_ID_KEY = "interstitialId";
            public const string _VIDEO_ID_KEY = "videoId";
            public const string _REWARD_ID_KEY = "rewardVideoId";

            public enum AdMobBannerSize {
                SmartBanner,
                Banner,
                MediumRectangle,
                Leaderboard,
                IABBanner
            }

            public enum AdMobBannerPosition {
                Center,
                Top,
                TopLeft,
                TopRight,
                Bottom,
                BottomLeft,
                BottomRight
            }

            [System.Serializable]
            public struct AdMobParameters {
                public string m_bannerUnitId;
                public string m_interstitialUnitId;
                public string m_videoUnitId;
                public string m_rewardVideoUnitId;
            }

            [SerializeField]
            public AdMobParameters m_defaultAndroidParams;
            [SerializeField]
            public AdMobParameters m_defaultIOSParams;        
            public bool m_tagForChildDirectedTreatment = false;

            protected override string PlacementParametersFolder {
                get {
                    return _PLACEMENT_PARAMETERS_FOLDER + "/";
                }
            }

#if _MS_ADMOB

            public class AdMobPlacementData : PlacementData {
                public AdMobPlacementData() : base() {
                }
                public AdMobPlacementData(AdType adType, string adID, string placementName = _PLACEMENT_DEFAULT_NAME) :
                    base(adType, adID, placementName) {
                }

                public AdPosition m_bannerPosition;
                public AdSize m_bannerSize;

                public EventHandler<EventArgs> onAdLoadedHandler;
                public EventHandler<AdFailedToLoadEventArgs> onAdFailedToLoadHandler;
                public EventHandler<EventArgs> onAdOpeningHandler;
                public EventHandler<EventArgs> onAdClosedHandler;
                public EventHandler<EventArgs> onAdLeavingApplicationHandler;
            }

            private RewardBasedVideoAd m_rewardVideo;
            private static string m_outputMessage = "";

            string m_bannerUnitId;
            string m_interstitialUnitId;
            string m_videoUnitId;
            string m_rewardVideoUnitId;

            bool m_isBannerLoaded;

            // Default Placements
            AdMobPlacementData m_bannerPlacement;
            AdMobPlacementData m_interstitialPlacement;
            AdMobPlacementData m_videoPlacement;
            AdMobPlacementData m_rewardVideoPlacement;
            
            public static string OutputMessage {
                set { m_outputMessage = value; }
            }

            void Awake() {
                m_rewardVideo = RewardBasedVideoAd.Instance;
                m_rewardVideo.OnAdLoaded += this.HandleRewardBasedVideoLoaded;
                m_rewardVideo.OnAdFailedToLoad += this.HandleRewardBasedVideoFailedToLoad;
                m_rewardVideo.OnAdOpening += this.HandleRewardBasedVideoOpened;
                m_rewardVideo.OnAdStarted += this.HandleRewardBasedVideoStarted;
                m_rewardVideo.OnAdRewarded += this.HandleRewardBasedVideoRewarded;
                m_rewardVideo.OnAdClosed += this.HandleRewardBasedVideoClosed;
                m_rewardVideo.OnAdLeavingApplication += this.HandleRewardBasedVideoLeftApplication;
            }

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters, JSONArray jsonPlacements) {
                base.InitializeParameters(parameters, jsonPlacements);

                if (parameters != null) {
                    try {
                        m_bannerUnitId = parameters[_BANNER_ID_KEY];
                        m_interstitialUnitId = parameters[_INTERSTITIAL_ID_KEY];
                        m_videoUnitId = parameters[_VIDEO_ID_KEY];
                        m_rewardVideoUnitId = parameters[_REWARD_ID_KEY];
                    }
                    catch {
                        m_bannerUnitId = "";
                        m_interstitialUnitId = "";
                        m_videoUnitId = "";
                        m_rewardVideoUnitId = "";
                    }
                }
                else {
#if UNITY_ANDROID
                    m_bannerUnitId = m_defaultAndroidParams.m_bannerUnitId;
                    m_interstitialUnitId = m_defaultAndroidParams.m_interstitialUnitId;
                    m_videoUnitId = m_defaultAndroidParams.m_videoUnitId;
                    m_rewardVideoUnitId = m_defaultAndroidParams.m_rewardVideoUnitId;
#elif UNITY_IOS
                    m_bannerUnitId = m_defaultIOSParams.m_bannerUnitId;
                    m_interstitialUnitId = m_defaultIOSParams.m_interstitialUnitId;
                    m_videoUnitId = m_defaultIOSParams.m_videoUnitId;
                    m_rewardVideoUnitId = m_defaultIOSParams.m_rewardVideoUnitId;
#endif
                }

                if (m_bannerUnitId.Length > 0) {
                    m_bannerPlacement = new AdMobPlacementData(AdType.Banner, m_bannerUnitId);
                    m_bannerPlacement.m_placementParams = GetPlacementParams(AdType.Banner, _PLACEMENT_DEFAULT_NAME);
                    AddPlacement(m_bannerPlacement);
                }

                if (m_interstitialUnitId.Length > 0) {
                    m_interstitialPlacement = new AdMobPlacementData(AdType.Interstitial, m_interstitialUnitId);
                    AddPlacement(m_interstitialPlacement);
                }

                if (m_videoUnitId.Length > 0) {
                    m_videoPlacement = new AdMobPlacementData(AdType.Video, m_videoUnitId);
                    AddPlacement(m_videoPlacement);
                }
            }

            protected override void InitializePlacementData(PlacementData placement, JSONValue jsonPlacementData) {
                base.InitializePlacementData(placement, jsonPlacementData);
            }

            protected override PlacementData CreatePlacementData(JSONValue jsonPlacementData) {
                PlacementData placementData = new AdMobPlacementData();
                return placementData;
            }

            public override void Prepare(AdType adType, PlacementData placement = null) {
                AdMobPlacementData adMobPlacementData = placement == null ? null : placement as AdMobPlacementData;

                if (GetAdState(adType, placement) != AdState.Loading) {
                    switch (adType) {
                        case AdType.Banner:
                            RequestBanner(adMobPlacementData);
                            break;
                        case AdType.Interstitial:
                            RequestInterstitial(adMobPlacementData);
                            break;
                        case AdType.Video:
                            RequestVideoInterstitial(adMobPlacementData);
                            break;
                        case AdType.Incentivized:
                            RequestRewardVideo(m_rewardVideoUnitId);
                            break;
                    }
                }
            }

            public override bool Show(AdType adType, PlacementData placement = null) {
                AdMobPlacementData adMobPlacementData = placement == null ? null : placement as AdMobPlacementData;
                bool isAdAvailable = GetAdState(adType, adMobPlacementData) == AdState.Available;

                if (adType == AdType.Banner || adType == AdType.Native) {
                    adMobPlacementData.m_isBannerAdTypeVisibled = true;
                }

                if (isAdAvailable) {
                    switch (adType) {
                        case AdType.Banner:
                            BannerView bannerView = placement.m_adView as BannerView;
                            isAdAvailable = bannerView != null;
                            if (isAdAvailable) {
                                bannerView.Show();
                                bannerView.SetPosition(adMobPlacementData.m_bannerPosition);
                            }
                            break;
                        case AdType.Interstitial:
                            InterstitialAd interstitial = placement.m_adView as InterstitialAd;
                            interstitial.Show();
                            break;
                        case AdType.Video:
                            InterstitialAd videoInterstitial = placement.m_adView as InterstitialAd;
                            videoInterstitial.Show();
                            break;
                        case AdType.Incentivized:
                            m_rewardVideo.Show();
                            break;
                    }
                }
                return isAdAvailable;
            }

            public override void Hide(AdType adType, PlacementData placementData = null) {
                AdMobPlacementData adMobPlacementData = placementData == null ? null : placementData as AdMobPlacementData;

                switch (adType) {
                    case AdType.Banner:
                        adMobPlacementData.m_isBannerAdTypeVisibled = false;

                        if (GetAdState(adType, placementData) == AdState.Available) {
                            BannerView bannerView = placementData.m_adView as BannerView;
                            bannerView.Hide();
                        }
                        AddEvent(AdType.Banner, AdEvent.Hide, placementData);
                        break;
                }
            }

            public override void HideBannerTypeAdWithoutNotify(AdType adType, PlacementData placementData = null) {
                AdMobPlacementData adMobPlacementData = placementData == null ? null : placementData as AdMobPlacementData;
                adMobPlacementData.m_isBannerAdTypeVisibled = false;

                switch (adType) {
                    case AdType.Banner:
                        if (GetAdState(adType, placementData) == AdState.Available) {
                            BannerView bannerView = placementData.m_adView as BannerView;
                            bannerView.Hide();
                        }
                        break;
                }
            }

            public override bool IsReady(AdType adType, PlacementData placement = null) {
#if UNITY_EDITOR
                return false;
#endif
                bool isReady = GetAdState(adType, placement) == AdState.Available;
                AdMobPlacementData adMobPlacementData = placement == null ? null : placement as AdMobPlacementData;

                switch(adType) {
                    case AdType.Incentivized:
                        isReady = m_rewardVideo.IsLoaded();
                        break;
                }

                return isReady;
            }

            public AdSize ConvertToAdSize(AdMobBannerSize bannerSize) {
                AdSize admobAdSize = AdSize.Banner;

                switch (bannerSize) {
                    case AdMobBannerSize.Banner:
                        admobAdSize = AdSize.Banner;
                        break;
                    case AdMobBannerSize.IABBanner:
                        admobAdSize = AdSize.IABBanner;
                        break;
                    case AdMobBannerSize.SmartBanner:
                        admobAdSize = AdSize.SmartBanner;
                        break;
                    case AdMobBannerSize.Leaderboard:
                        admobAdSize = AdSize.Leaderboard;
                        break;
                    case AdMobBannerSize.MediumRectangle:
                        admobAdSize = AdSize.MediumRectangle;
                        break;
                }
                return admobAdSize;
            }

            public AdPosition ConvertToAdPosition(AdMobBannerPosition bannerPosition) {
                AdPosition admobAdPosition = AdPosition.Center;

                switch (bannerPosition) {
                    case AdMobBannerPosition.Bottom:
                        admobAdPosition = AdPosition.Bottom;
                        break;
                    case AdMobBannerPosition.BottomLeft:
                        admobAdPosition = AdPosition.BottomLeft;
                        break;
                    case AdMobBannerPosition.BottomRight:
                        admobAdPosition = AdPosition.BottomRight;
                        break;
                    case AdMobBannerPosition.Top:
                        admobAdPosition = AdPosition.Top;
                        break;
                    case AdMobBannerPosition.TopLeft:
                        admobAdPosition = AdPosition.TopLeft;
                        break;
                    case AdMobBannerPosition.TopRight:
                        admobAdPosition = AdPosition.TopRight;
                        break;
                    case AdMobBannerPosition.Center:
                        admobAdPosition = AdPosition.Center;
                        break;
                }
                return admobAdPosition;
            }

            private void RequestBanner(AdMobPlacementData placement) {
                DestroyBanner(placement);

#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.RequestBanner() " + " placement: " + placement.PlacementName);
#endif

                SetAdState(AdType.Banner, placement, AdState.Loading);

                AdMobBannerPlacementParameters bannerParams = placement.m_placementParams as AdMobBannerPlacementParameters;
                placement.m_bannerSize = ConvertToAdSize(bannerParams.m_bannerSize);
                placement.m_bannerPosition = ConvertToAdPosition(bannerParams.m_bannerPosition);

                BannerView bannerView = new BannerView(placement.m_adID, placement.m_bannerSize, placement.m_bannerPosition);
                placement.m_adView = bannerView;
                bannerView.Hide();

                // Register for ad events.

                placement.onAdLoadedHandler = delegate (object sender, EventArgs args) {
                    HandleAdLoaded(placement, sender, args);
                };
                bannerView.OnAdLoaded += placement.onAdLoadedHandler;

                placement.onAdFailedToLoadHandler = delegate (object sender, AdFailedToLoadEventArgs args) {
                    HandleAdFailedToLoad(placement, sender, args);
                };
                bannerView.OnAdFailedToLoad += placement.onAdFailedToLoadHandler;

                placement.onAdOpeningHandler = delegate (object sender, EventArgs args) {
                    HandleAdOpened(placement, sender, args);
                };
                bannerView.OnAdOpening += placement.onAdOpeningHandler;

                placement.onAdClosedHandler = delegate (object sender, EventArgs args) {
                    HandleAdClosed(placement, sender, args);
                };
                bannerView.OnAdClosed += placement.onAdClosedHandler;

                placement.onAdLeavingApplicationHandler = delegate (object sender, EventArgs args) {
                    HandleAdLeftApplication(placement, sender, args);
                };
                bannerView.OnAdLeavingApplication += placement.onAdLeavingApplicationHandler;

                // Load a banner ad.
                bannerView.LoadAd(CreateAdRequest());
            }

            void DestroyBanner(AdMobPlacementData placement) {
                m_isBannerLoaded = false;

                if (placement.m_adView != null) {
                    BannerView bannerView = placement.m_adView as BannerView;
                    placement.m_adView = null;

                    bannerView.OnAdLoaded -= placement.onAdLoadedHandler;
                    bannerView.OnAdFailedToLoad -= placement.onAdFailedToLoadHandler;
                    bannerView.OnAdOpening -= placement.onAdOpeningHandler;
                    bannerView.OnAdClosed -= placement.onAdClosedHandler;
                    bannerView.OnAdLeavingApplication -= placement.onAdLeavingApplicationHandler;

                    bannerView.Destroy();
                    SetAdState(AdType.Banner, placement, AdState.Uncertain);
                }
            }

            private void RequestInterstitial(AdMobPlacementData placement) {
                DestroyInterstitial(placement);

                SetAdState(AdType.Interstitial, placement, AdState.Loading);

                // Create an interstitial.
                InterstitialAd interstitial = new InterstitialAd(placement.m_adID);
                placement.m_adView = interstitial;

                // Register for ad events.
                placement.onAdLoadedHandler = delegate (object sender, EventArgs args) {
                    HandleInterstitialLoaded(placement, sender, args);
                };
                interstitial.OnAdLoaded += placement.onAdLoadedHandler;

                placement.onAdFailedToLoadHandler = delegate (object sender, AdFailedToLoadEventArgs args) {
                    HandleInterstitialFailedToLoad(placement, sender, args);
                };
                interstitial.OnAdFailedToLoad += placement.onAdFailedToLoadHandler;

                placement.onAdOpeningHandler = delegate (object sender, EventArgs args) {
                    HandleInterstitialOpened(placement, sender, args);
                };
                interstitial.OnAdOpening += placement.onAdOpeningHandler;

                placement.onAdClosedHandler = delegate (object sender, EventArgs args) {
                    HandleInterstitialClosed(placement, sender, args);
                };
                interstitial.OnAdClosed += placement.onAdClosedHandler;

                placement.onAdLeavingApplicationHandler = delegate (object sender, EventArgs args) {
                    HandleInterstitialLeftApplication(placement, sender, args);
                };
                interstitial.OnAdLeavingApplication += placement.onAdLeavingApplicationHandler;

                interstitial.LoadAd(CreateAdRequest());
            }

            void DestroyInterstitial(AdMobPlacementData placement) {
                if (placement.m_adView != null) {
                    InterstitialAd interstitial = placement.m_adView as InterstitialAd;
                    placement.m_adView = null;

                    interstitial.OnAdLoaded -= placement.onAdLoadedHandler;
                    interstitial.OnAdFailedToLoad -= placement.onAdFailedToLoadHandler;
                    interstitial.OnAdOpening -= placement.onAdOpeningHandler;
                    interstitial.OnAdClosed -= placement.onAdClosedHandler;
                    interstitial.OnAdLeavingApplication -= placement.onAdLeavingApplicationHandler;

                    interstitial.Destroy();
                    SetAdState(AdType.Interstitial, placement, AdState.Uncertain);
                }
            }

            private void RequestVideoInterstitial(AdMobPlacementData placement) {
                DestroyVideoInterstitial(placement);

                SetAdState(AdType.Video, placement, AdState.Loading);

                // Create an interstitial.
                InterstitialAd videoInterstitial = new InterstitialAd(placement.m_adID);
                placement.m_adView = videoInterstitial;

                // Register for ad events.
                placement.onAdLoadedHandler = delegate (object sender, EventArgs args) {
                    HandleVideoInterstitialLoaded(placement, sender, args);
                };
                videoInterstitial.OnAdLoaded += placement.onAdLoadedHandler;

                placement.onAdFailedToLoadHandler = delegate (object sender, AdFailedToLoadEventArgs args) {
                    HandleVideoInterstitialFailedToLoad(placement, sender, args);
                };
                videoInterstitial.OnAdFailedToLoad += placement.onAdFailedToLoadHandler;

                placement.onAdOpeningHandler = delegate (object sender, EventArgs args) {
                    HandleVideoInterstitialOpened(placement, sender, args);
                };
                videoInterstitial.OnAdOpening += placement.onAdOpeningHandler;

                placement.onAdClosedHandler = delegate (object sender, EventArgs args) {
                    HandleVideoInterstitialClosed(placement, sender, args);
                };
                videoInterstitial.OnAdClosed += placement.onAdClosedHandler;

                videoInterstitial.LoadAd(CreateAdRequest());
            }

            void DestroyVideoInterstitial(AdMobPlacementData placement) {
                if (placement.m_adView != null) {
                    InterstitialAd videoInterstitial = placement.m_adView as InterstitialAd;
                    placement.m_adView = null;

                    videoInterstitial.OnAdLoaded -= placement.onAdLoadedHandler;
                    videoInterstitial.OnAdFailedToLoad -= placement.onAdFailedToLoadHandler;
                    videoInterstitial.OnAdOpening -= placement.onAdOpeningHandler;
                    videoInterstitial.OnAdClosed -= placement.onAdClosedHandler;

                    videoInterstitial.Destroy();
                    SetAdState(AdType.Video, placement, AdState.Uncertain);
                }
            }

            private void RequestRewardVideo(string adUnitId) {
                SetAdState(AdType.Interstitial, null, AdState.Loading);
                m_rewardVideo.LoadAd(CreateAdRequest(), adUnitId);
            }

            // Returns an ad request with custom ad targeting.
            private AdRequest CreateAdRequest() {
                AdRequest request = new AdRequest.Builder()
                        .TagForChildDirectedTreatment(m_tagForChildDirectedTreatment)
                        .AddExtra("color_bg", "9B30FF") /*
                        .AddTestDevice(AdRequest.TestDeviceSimulator)
                        .AddTestDevice("0174B8AAC6D39B5DD89D5CCE260726AF")*/
                        .Build();
                return request;
            }

            //------------------------------------------------------------------------
#region Banner callback handlers

            public void HandleAdLoaded(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdLoaded() " + " placement: " + placement.PlacementName +
                    " isVisibled: " + placement.m_isBannerAdTypeVisibled);
#endif

                SetAdState(AdType.Banner, placement, AdState.Available);
                BannerView bannerView = placement.m_adView as BannerView;
                if (placement.m_isBannerAdTypeVisibled) {
                    bannerView.Show();
                    bannerView.SetPosition(placement.m_bannerPosition);
                }
                else {
                    bannerView.Hide();
                }
                AddEvent(AdType.Banner, AdEvent.Prepared, placement);
            }

            public void HandleAdFailedToLoad(AdMobPlacementData placement, object sender, AdFailedToLoadEventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdFailedToLoad() " + " placement: " + placement.PlacementName +
                    " message: " + args.Message);
#endif
                DestroyBanner(placement);
                AddEvent(AdType.Banner, AdEvent.PrepareFailure, placement);
            }

            public void HandleAdOpened(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdOpened() " +" placement: " + placement.PlacementName);
#endif
                AddEvent(AdType.Banner, AdEvent.Show, placement);
            }

            void HandleAdClosing(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdClosing() " + " placement: " + placement.PlacementName);
#endif
            }

            public void HandleAdClosed(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdClosed() " + " placement: " + placement.PlacementName);
#endif
            }

            public void HandleAdLeftApplication(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleAdLeftApplication() " + " placement: " + placement.PlacementName);
#endif
            }

#endregion // Banner callback handlers

            //------------------------------------------------------------------------
#region Interstitial callback handlers

            public void HandleInterstitialLoaded(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialLoaded()");
#endif
                SetAdState(AdType.Interstitial, placement, AdState.Available);
                AddEvent(AdType.Interstitial, AdEvent.Prepared, placement);
            }

            public void HandleInterstitialFailedToLoad(AdMobPlacementData placement, object sender, AdFailedToLoadEventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialFailedToLoad() message: " + args.Message);
#endif
                DestroyInterstitial(placement);
                AddEvent(AdType.Interstitial, AdEvent.PrepareFailure, placement);
            }

            public void HandleInterstitialOpened(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialOpened()");
#endif
                AddEvent(AdType.Interstitial, AdEvent.Show, placement);
            }

            void HandleInterstitialClosing(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialClosing()");
#endif
            }

            public void HandleInterstitialClosed(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialClosed()");
#endif
                DestroyInterstitial(placement);
                AddEvent(AdType.Interstitial, AdEvent.Hide, placement);
            }

            public void HandleInterstitialLeftApplication(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("AdMobAdapter.HandleInterstitialLeftApplication()");
#endif
            }

#endregion // Interstitial callback handlers

            //------------------------------------------------------------------------
#region Video Interstitial callback handlers

            public void HandleVideoInterstitialLoaded(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialLoaded event received.");
#endif
                SetAdState(AdType.Video, placement, AdState.Available);
                AddEvent(AdType.Video, AdEvent.Prepared, placement);
            }

            public void HandleVideoInterstitialFailedToLoad(AdMobPlacementData placement, object sender, AdFailedToLoadEventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialFailedToLoad event received with message: " + args.Message);
#endif
                DestroyVideoInterstitial(placement);
                AddEvent(AdType.Video, AdEvent.PrepareFailure, placement);
            }

            public void HandleVideoInterstitialOpened(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialOpened event received");
#endif
                AddEvent(AdType.Video, AdEvent.Show, placement);
            }

            void HandleVideoInterstitialClosing(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialClosing event received");
#endif
            }

            public void HandleVideoInterstitialClosed(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialClosed event received");
#endif
                DestroyVideoInterstitial(placement);
                AddEvent(AdType.Video, AdEvent.Hide, placement);
            }

            public void HandleVideoInterstitialLeftApplication(AdMobPlacementData placement, object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                print("HandleInterstitialLeftApplication event received");
#endif
            }

#endregion  // Video Interstitial callback handlers

            //------------------------------------------------------------------------
#region Reward Video callback handlers

            public void HandleRewardBasedVideoLoaded(object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoLoaded event received");
#endif
                SetAdState(AdType.Incentivized, null, AdState.Available);
                AddEvent(AdType.Incentivized, AdEvent.Prepared);
            }

            public void HandleRewardBasedVideoFailedToLoad(object sender, AdFailedToLoadEventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoFailedToLoad event received with message: " + args.Message);
#endif
                SetAdState(AdType.Incentivized, null, AdState.Uncertain);
                AddEvent(AdType.Incentivized, AdEvent.PrepareFailure);
            }

            public void HandleRewardBasedVideoOpened(object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoOpened event received");
#endif
                AddEvent(AdType.Incentivized, AdEvent.Show);
            }

            public void HandleRewardBasedVideoStarted(object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoStarted event received");
#endif
            }

            public void HandleRewardBasedVideoClosed(object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoClosed event received");
#endif
                SetAdState(AdType.Incentivized, null, AdState.Uncertain);
                AddEvent(AdType.Incentivized, AdEvent.Hide);
            }

            public void HandleRewardBasedVideoRewarded(object sender, Reward args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoRewarded event received for " + args.Amount.ToString() + " " + args.Type);
#endif
                string type = args.Type;
                double amount = args.Amount;
                AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
            }

            public void HandleRewardBasedVideoLeftApplication(object sender, EventArgs args) {
#if AD_MEDIATION_DEBUG_MODE
                MonoBehaviour.print("HandleRewardBasedVideoLeftApplication event received");
#endif
            }

            #endregion // Reward Video callback handlers

#endif // _MS_ADMOB

        }

    } // namespace AdMediation
} // namespace Virterix
