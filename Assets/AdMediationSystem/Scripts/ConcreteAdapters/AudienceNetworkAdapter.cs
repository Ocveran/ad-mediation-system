
#define _MS_AUDIENCE_NETWORK

#if _MS_AUDIENCE_NETWORK

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AudienceNetwork;

namespace Virterix {
    namespace AdMediation {
         
        public class AudienceNetworkAdapter : AdNetworkAdapter {

            public enum BannerPlacement {
                Top,
                Bottom
            }

            [System.Serializable]
            public struct AudienceNetworkParameters {
                public string bannerPlacementId;
                public string interstitialPlacementId;
            }

            [SerializeField]
            public AudienceNetworkParameters m_defaultAndroidParams;
            [SerializeField]
            public AudienceNetworkParameters m_defaultIOSParams;

            public AdSize m_bannerSize;
            public BannerPlacement m_bannerPlacement;

            string m_bannerPlacementId;
            string m_interstitialPlacementId;

            AdView m_bannerView;
            bool m_isBannerReady;
            Vector2 m_bannerPosition;

            InterstitialAd m_interstitialAd;
            bool m_isInterstitialLoaded;


            void Awake() {
                m_bannerPosition = Vector3.zero;
            }

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                if (parameters != null) {
                    m_bannerPlacementId = parameters["bannerPlacementId"];
                    m_interstitialPlacementId = parameters["interstitialPlacementId"];
                } else {
#if UNITY_ANDROID
                    m_bannerPlacementId = m_defaultAndroidParams.bannerPlacementId;
                    m_interstitialPlacementId = m_defaultAndroidParams.interstitialPlacementId;
#elif UNITY_IOS
                    m_bannerPlacementId = m_defaultIOSParams.bannerPlacementId;
                    m_interstitialPlacementId = m_defaultIOSParams.interstitialPlacementId;
#endif
                }
            }

            public override void Prepare(AdType adType) {
                switch (adType) {
                    case AdType.Banner:
                        if (m_bannerView == null) {
                            CreateBanner(m_bannerPlacementId);
                        }
                        break;
                    case AdType.Interstitial:
                        if (m_interstitialAd == null) {
                            CreateInterstitial(m_interstitialPlacementId);
                        }
                        break;
                }
            }

            public override bool Show(AdType adType) {
                bool showSuccess = false;
                switch (adType) {
                    case AdType.Banner:
                        IsBannerVisibled = true;
                        if (m_bannerView != null && m_isBannerReady) {
                            showSuccess = m_bannerView.Show(m_bannerPosition.x, m_bannerPosition.y);
                        }
                        break;
                    case AdType.Interstitial:
                        if (m_isInterstitialLoaded && m_interstitialAd != null) {
                            showSuccess = m_interstitialAd.Show();
                            if (showSuccess) {
                                AddEvent(adType, AdEvent.Show);
                            }
                        }
                        break;
                }
                return showSuccess;
            }

			public override void Hide(AdType adType) {
                switch (adType) {
				case AdType.Banner:
					IsBannerVisibled = false;
					if (m_isBannerReady) {
                        DestroyBanner();
                        NotifyEvent (AdType.Banner, AdEvent.Hide);
					}
                    break;
                }
            }

            public override bool IsReady(AdType adType) {
                switch (adType) {
                    case AdType.Interstitial:
                        return m_isInterstitialLoaded;
                    case AdType.Banner:
                        return m_isBannerReady;
                    default:
                        return false;
                }
            }

            void CalculateBannerPosition() {
                float bannerHight = 0;
                switch (m_bannerSize) {
                    case AdSize.BANNER_HEIGHT_50:
                        bannerHight = 50f;
                        break;
                    case AdSize.BANNER_HEIGHT_90:
                        bannerHight = 90f;
                        break;
                    case AdSize.RECTANGLE_HEIGHT_250:
                        bannerHight = 250f;
                        break;
                }

                switch (m_bannerPlacement) {
                    case BannerPlacement.Bottom:
                        m_bannerPosition.x = 0f;
                        m_bannerPosition.y = (float)AudienceNetwork.Utility.AdUtility.height() - bannerHight;
                        break;
                    case BannerPlacement.Top:
                        m_bannerPosition.x = 0f;
                        m_bannerPosition.y = 0f;
                        break;
                }
            }

            void CreateBanner(string placementId) {
                DestroyBanner();
                CalculateBannerPosition();

#if !UNITY_EDITOR
                // Create a banner's ad view with a unique placement ID (generate your own on the Facebook app settings).
                // Use different ID for each ad placement in your app.
                m_bannerView = new AdView(placementId, m_bannerSize);
                m_bannerView.Register(this.gameObject);

                m_bannerView.AdViewDidLoad += BannerAdViewDidLoad;
                m_bannerView.AdViewDidFailWithError += BannerAdViewDidFailWithError;
                m_bannerView.AdViewWillLogImpression += BannerAdViewWillLogImpression;
                m_bannerView.AdViewDidClick += BannerAdViewDidClick;

                m_bannerView.LoadAd();
#endif
            }

            void DestroyBanner() {
#if !UNITY_EDITOR
                m_isBannerReady = false;
                if (m_bannerView != null) {
					m_bannerView.AdViewDidLoad -= BannerAdViewDidLoad;
					m_bannerView.AdViewDidFailWithError -= BannerAdViewDidFailWithError;
					m_bannerView.AdViewWillLogImpression -= BannerAdViewWillLogImpression;
					m_bannerView.AdViewDidClick -= BannerAdViewDidClick;
                    m_bannerView.Dispose();
                    m_bannerView = null;
                }
#endif
            }

            void CreateInterstitial(string placementId) {
                DestroyInterstitial();

#if !UNITY_EDITOR
                // Create the interstitial unit with a placement ID (generate your own on the Facebook app settings).
                // Use different ID for each ad placement in your app.
                m_interstitialAd = new InterstitialAd(placementId);
                m_interstitialAd.Register(this.gameObject);

                m_interstitialAd.InterstitialAdDidLoad += InterstitialAdDidLoad;
                m_interstitialAd.InterstitialAdDidFailWithError += InterstitialAdDidFailWithError;
                m_interstitialAd.InterstitialAdDidClose += InterstitialAdDidClose;
                m_interstitialAd.InterstitialAdDidClick += InterstitialAdDidClick;

                // Initiate the request to load the ad.
                m_interstitialAd.LoadAd();
#endif
            }

            void DestroyInterstitial() {
#if !UNITY_EDITOR
                m_isInterstitialLoaded = false;
                if (m_interstitialAd != null) {
					m_interstitialAd.InterstitialAdDidLoad -= InterstitialAdDidLoad;
					m_interstitialAd.InterstitialAdDidFailWithError -= InterstitialAdDidFailWithError;
					m_interstitialAd.InterstitialAdDidClose -= InterstitialAdDidClose;
					m_interstitialAd.InterstitialAdDidClick -= InterstitialAdDidClick;
                    m_interstitialAd.Dispose();
                    m_interstitialAd = null;
                }
#endif
            }

            //------------------------------------------------------------------------
            #region Banner callback handlers

            void BannerAdViewDidLoad() {
                Debug.Log("Ad view loaded.");
                m_isBannerReady = true;
				if (IsBannerVisibled) {
					m_bannerView.Show (m_bannerPosition.x, m_bannerPosition.y);
                }
                AddEvent(AdType.Banner, AdEvent.Prepared);
            }

            void BannerAdViewDidFailWithError(string error) {
                Debug.Log("Ad view failed to load with error: " + error);
                AddEvent(AdType.Banner, AdEvent.PrepareFailure);
            }

            void BannerAdViewWillLogImpression() {
                Debug.Log("Ad view logged impression.");
            }

            void BannerAdViewDidClick() {
                Debug.Log("Ad view clicked.");
                AddEvent(AdType.Banner, AdEvent.Click);
            }

            #endregion //Banner callback handlers

            //------------------------------------------------------------------------
            #region Interstitial callback handlers

            void InterstitialAdDidLoad() {
                Debug.Log("Interstitial ad loaded.");
                m_isInterstitialLoaded = true;
                AddEvent(AdType.Interstitial, AdEvent.Prepared);
            }

            void InterstitialAdDidFailWithError(string error) {
                Debug.Log("Interstitial ad failed to load with error: " + error);
                AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
            }

            void InterstitialAdDidClose() {
                Debug.Log("InterstitialAdDidClose");
                DestroyInterstitial();
                AddEvent(AdType.Interstitial, AdEvent.Hide);
            }

            void InterstitialAdDidClick() {
                Debug.Log("Interstitial ad clicked.");
                AddEvent(AdType.Interstitial, AdEvent.Click);
            }

            #endregion // Interstitial callback handlers
        }

    } // namespace AdMediation
} // namespace Virterix

#endif
