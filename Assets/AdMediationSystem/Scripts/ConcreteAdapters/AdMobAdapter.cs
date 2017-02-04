
#define _MS_ADMOB

#if _MS_ADMOB

using UnityEngine;
using System;
using System.Collections;
using GoogleMobileAds;
using GoogleMobileAds.Api;

namespace Virterix {
    namespace AdMediation {

        public class GoogleMobileAdsDemoHandler : IDefaultInAppPurchaseProcessor {
            private readonly string[] validSkus = { "android.test.purchased" };

            //Will only be sent on a success.
            public void ProcessCompletedInAppPurchase(IInAppPurchaseResult result) {
                result.FinishPurchase();
            }

            //Check SKU against valid SKUs.
            public bool IsValidPurchase(string sku) {
                foreach (string validSku in validSkus) {
                    if (sku == validSku) {
                        return true;
                    }
                }
                return false;
            }

            //Return the app's public key.
            public string AndroidPublicKey {
                //In a real app, return public key instead of null.
                get { return null; }
            }
        }

        public class AdMobAdapter : AdNetworkAdapter {

            [System.Serializable]
            public struct AdMobParameters {
                public string bannerUnitId;
                public string interstitialUnitId;
                public string videoUnitId;
            }

            [SerializeField]
            public AdMobParameters m_defaultAndroidParams;
            [SerializeField]
            public AdMobParameters m_defaultIOSParams;

            private BannerView m_bannerView;
            private InterstitialAd m_interstitial;
            private InterstitialAd m_videoInterstitial;
            private static string m_outputMessage = "";

            string m_bannerUnitId;
            string m_interstitialUnitId;
            string m_videoUnitId;

            bool m_isBannerLoaded;

            public AdSize BannerSize {
                set {
                    m_adSize = value;
                }
            }
            AdSize m_adSize = AdSize.SmartBanner;

            public AdPosition BannerPosition {
                set {
                    m_adPosition = value;
                }
            }
            AdPosition m_adPosition = AdPosition.Bottom;

            public static string OutputMessage {
                set { m_outputMessage = value; }
            }

            void Awake() {
            }

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                if (parameters != null) {
                    try {
                        m_bannerUnitId = parameters["bannerId"];
                        m_interstitialUnitId = parameters["interstitialId"];
                        m_videoUnitId = parameters["videoId"];
                    }
                    catch {
                        m_bannerUnitId = "";
                        m_interstitialUnitId = "";
                        m_videoUnitId = "";
                    }
                } else {
#if UNITY_ANDROID
                    m_bannerUnitId = m_defaultAndroidParams.bannerUnitId;
                    m_interstitialUnitId = m_defaultAndroidParams.interstitialUnitId;
                    m_videoUnitId = m_defaultAndroidParams.videoUnitId;
#elif UNITY_IOS
                    m_bannerUnitId = m_defaultIOSParams.bannerUnitId;
                    m_interstitialUnitId = m_defaultIOSParams.interstitialUnitId;
                    m_videoUnitId = m_defaultIOSParams.videoUnitId;
#endif
                }
            }

            public override void Prepare(AdType adType) {
                if (!IsSupported(adType)) {
                    return;
                }

                switch (adType) {
                    case AdType.Banner:
                        RequestBanner(m_bannerUnitId, m_adSize, m_adPosition);
                        break;
                    case AdType.Interstitial:
                        RequestInterstitial(m_interstitialUnitId);
                        break;
                    case AdType.Video:
                        RequestVideoInterstitial(m_videoUnitId);
                        break;
                }
            }

            public override bool Show(AdType adType) {
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Banner:
                            IsBannerVisibled = true;
                            m_bannerView.Show();
                            break;
                        case AdType.Interstitial:
                            m_interstitial.Show();
                            break;
                        case AdType.Video:
                            m_videoInterstitial.Show();
                            break;
                    }
                    return true;
                } else {
                    return false;
                }
            }

            public override void Hide(AdType adType) {
                switch (adType) {
                    case AdType.Banner:
                        IsBannerVisibled = false;
                        if (m_isBannerLoaded) {
                            m_bannerView.Hide();
                            AddEvent(AdType.Banner, AdEvent.Hide);
                        }
                        break;
                }
            }

            public override bool IsReady(AdType adType) {
#if UNITY_EDITOR
                return false;
#endif

                switch (adType) {
                    case AdType.Banner:
                        return m_isBannerLoaded;
                    case AdType.Interstitial:
                        if (m_interstitial == null) {
                            return false;
                        } else {
                            return m_interstitial.IsLoaded();
                        }
                    case AdType.Video:
                        if (m_videoInterstitial == null) {
                            return false;
                        } else {
                            return m_videoInterstitial.IsLoaded();
                        }
                    default:
                        return false;
                }
            }

            private void RequestBanner(string adUnitId, AdSize adSize, AdPosition adPosition) {
                DestroyBanner();

                // Create a 320x50 banner at the top of the screen.
                m_bannerView = new BannerView(adUnitId, adSize, adPosition);
                // Register for ad events.
                m_bannerView.OnAdLoaded += HandleAdLoaded;
                m_bannerView.OnAdFailedToLoad += HandleAdFailedToLoad;
                m_bannerView.OnAdOpening += HandleAdOpened;
                m_bannerView.OnAdClosed += HandleAdClosed;
                // Load a banner ad.
                m_bannerView.LoadAd(CreateAdRequest());
            }

            void DestroyBanner() {
                m_isBannerLoaded = false;
                if (m_bannerView != null) {
                    m_bannerView.OnAdLoaded -= HandleAdLoaded;
                    m_bannerView.OnAdFailedToLoad -= HandleAdFailedToLoad;
                    m_bannerView.OnAdOpening -= HandleAdOpened;
                    m_bannerView.OnAdClosed -= HandleAdClosed;
                    m_bannerView.Destroy();
                    m_bannerView = null;
                }
            }

            private void RequestInterstitial(string adUnitId) {
                DestroyInterstitial();

                // Create an interstitial.
                m_interstitial = new InterstitialAd(adUnitId);
                // Register for ad events.
                m_interstitial.OnAdLoaded += HandleInterstitialLoaded;
                m_interstitial.OnAdFailedToLoad += HandleInterstitialFailedToLoad;
                m_interstitial.OnAdOpening += HandleInterstitialOpened;
                m_interstitial.OnAdClosed += HandleInterstitialClosed;
                m_interstitial.LoadAd(CreateAdRequest());
            }

            void DestroyInterstitial() {
                if (m_interstitial != null) {
                    m_interstitial.OnAdLoaded -= HandleInterstitialLoaded;
                    m_interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
                    m_interstitial.OnAdOpening -= HandleInterstitialOpened;
                    m_interstitial.OnAdClosed -= HandleInterstitialClosed;
                    m_interstitial.Destroy();
                    m_interstitial = null;
                }
            }

            private void RequestVideoInterstitial(string adUnitId) {
                DestroyVideoInterstitial();

                // Create an interstitial.
                m_videoInterstitial = new InterstitialAd(adUnitId);
                // Register for ad events.
                m_videoInterstitial.OnAdLoaded += HandleVideoInterstitialLoaded;
                m_videoInterstitial.OnAdFailedToLoad += HandleVideoInterstitialFailedToLoad;
                m_videoInterstitial.OnAdOpening += HandleVideoInterstitialOpened;
                m_videoInterstitial.OnAdClosed += HandleVideoInterstitialClosed;
                m_videoInterstitial.LoadAd(CreateAdRequest());
            }

            void DestroyVideoInterstitial() {
                if (m_videoInterstitial != null) {
                    m_videoInterstitial.OnAdLoaded -= HandleVideoInterstitialLoaded;
                    m_videoInterstitial.OnAdFailedToLoad -= HandleVideoInterstitialFailedToLoad;
                    m_videoInterstitial.OnAdOpening -= HandleVideoInterstitialOpened;
                    m_videoInterstitial.OnAdClosed -= HandleVideoInterstitialClosed;
                    m_videoInterstitial.Destroy();
                    m_videoInterstitial = null;
                }
            }

            // Returns an ad request with custom ad targeting.
            private AdRequest CreateAdRequest() {
                return new AdRequest.Builder()
                        .TagForChildDirectedTreatment(false)
                        .AddExtra("color_bg", "9B30FF")
                        .Build();
            }

            //------------------------------------------------------------------------
            #region Banner callback handlers

            public void HandleAdLoaded(object sender, EventArgs args) {
                print("HandleAdLoaded event received.");
                if (!m_isBannerLoaded) {
                    m_isBannerLoaded = true;
                    AddEvent(AdType.Banner, AdEvent.Prepared);
                    if (!IsBannerVisibled) {
                        m_bannerView.Hide();
                    }
                }
            }

            public void HandleAdFailedToLoad(object sender, AdFailedToLoadEventArgs args) {
                print("HandleFailedToReceiveAd event received with message: " + args.Message);
                AddEvent(AdType.Banner, AdEvent.PrepareFailure);
            }

            public void HandleAdOpened(object sender, EventArgs args) {
                print("HandleAdOpened event received");
                AddEvent(AdType.Banner, AdEvent.Show);
            }

            void HandleAdClosing(object sender, EventArgs args) {
                print("HandleAdClosing event received");
            }

            public void HandleAdClosed(object sender, EventArgs args) {
                print("HandleAdClosed event received");
            }

            public void HandleAdLeftApplication(object sender, EventArgs args) {
                print("HandleAdLeftApplication event received");
            }

            #endregion // Banner callback handlers

            //------------------------------------------------------------------------
            #region Interstitial callback handlers

            public void HandleInterstitialLoaded(object sender, EventArgs args) {
                print("HandleInterstitialLoaded event received.");
                AddEvent(AdType.Interstitial, AdEvent.Prepared);
            }

            public void HandleInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs args) {
                print("HandleInterstitialFailedToLoad event received with message: " + args.Message);
                AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
            }

            public void HandleInterstitialOpened(object sender, EventArgs args) {
                print("HandleInterstitialOpened event received");
                AddEvent(AdType.Interstitial, AdEvent.Show);
            }

            void HandleInterstitialClosing(object sender, EventArgs args) {
                print("HandleInterstitialClosing event received");
            }

            public void HandleInterstitialClosed(object sender, EventArgs args) {
                print("HandleInterstitialClosed event received");
                AddEvent(AdType.Interstitial, AdEvent.Hide);
            }

            public void HandleInterstitialLeftApplication(object sender, EventArgs args) {
                print("HandleInterstitialLeftApplication event received");
            }

            #endregion // Interstitial callback handlers

            //------------------------------------------------------------------------
            #region Video Interstitial callback handlers

            public void HandleVideoInterstitialLoaded(object sender, EventArgs args) {
                print("HandleInterstitialLoaded event received.");
                AddEvent(AdType.Video, AdEvent.Prepared);
            }

            public void HandleVideoInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs args) {
                print("HandleInterstitialFailedToLoad event received with message: " + args.Message);
                AddEvent(AdType.Video, AdEvent.PrepareFailure);
            }

            public void HandleVideoInterstitialOpened(object sender, EventArgs args) {
                print("HandleInterstitialOpened event received");
                AddEvent(AdType.Video, AdEvent.Show);
            }

            void HandleVideoInterstitialClosing(object sender, EventArgs args) {
                print("HandleInterstitialClosing event received");
            }

            public void HandleVideoInterstitialClosed(object sender, EventArgs args) {
                print("HandleInterstitialClosed event received");
                AddEvent(AdType.Video, AdEvent.Hide);
            }

            public void HandleVideoInterstitialLeftApplication(object sender, EventArgs args) {
                print("HandleInterstitialLeftApplication event received");
            }

            #endregion  // Video Interstitial callback handlers
        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_ADMOB