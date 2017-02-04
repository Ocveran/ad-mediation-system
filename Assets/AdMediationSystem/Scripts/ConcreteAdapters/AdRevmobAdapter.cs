
//#define _MS_REVMOB

#if _MS_REVMOB

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class AdRevmobAdapter : AdNetworkAdapter, IRevMobListener {

            public RevMob.Position m_bannerPosition;

            private static Dictionary<string, string> m_revmobAppIds = new Dictionary<string, string>();
            private RevMob m_revmob;

            RevMobBanner m_revMobBanner;
            RevMobFullscreen m_revMobInterstitial;
            RevMobFullscreen m_revMobRewardedVideo;
            RevMobFullscreen m_revMobVideo;

            bool m_isReadyInterstitial;
            bool m_isReadyRewardedVideo;

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                string mediaId = parameters["MediaID"];

                m_revmobAppIds.Add("Android", mediaId);
                m_revmobAppIds.Add("IOS", mediaId);

                m_revmob = RevMob.Start(m_revmobAppIds, this.name);
                m_revmob.SetTestingMode(RevMob.Test.WITH_ADS);
            }

            public override void Prepare(AdType adType) {
                switch (adType) {
                    case AdType.Banner:
                        if (m_revMobBanner == null) {
                            m_revMobBanner = m_revmob.CreateBanner(m_bannerPosition);
                        }
                        break;
                    case AdType.Interstitial:
                        if (m_revMobInterstitial == null) {
                            m_revMobInterstitial = m_revmob.CreateFullscreen();
                        }
                        break;
                    case AdType.RewardVideo:
                        if (m_revMobRewardedVideo == null) {
                            m_revMobRewardedVideo = m_revmob.CreateRewardedVideo();
                        }
                        break;
                    case AdType.Video:
                        if (m_revMobVideo == null) {
                            m_revMobVideo = m_revmob.CreateVideo();
                        }
                        break;
                }
            }

            public override void Show(AdType adType) {
                switch (adType) {
                    case AdType.Banner:
                        m_revMobBanner.Show();
                        break;
                    case AdType.Interstitial:
                        m_revMobInterstitial.Show();
                        break;
                    case AdType.RewardVideo:
                        m_revMobRewardedVideo.ShowRewardedVideo();
                        break;
                    case AdType.Video:
                        m_revMobVideo.ShowVideo();
                        break;
                }
            }

            public override void Hide(AdType adType) {
                switch (adType) {
                    case AdType.Banner:
                        if (m_revMobBanner != null) {
                            m_revMobBanner.Hide();
                        }
                        break;
                }
            }

            public override bool IsReady(AdType adType) {
                switch (adType) {
                    case AdType.RewardVideo:
                        return m_isReadyRewardedVideo;
                    case AdType.Interstitial:
                        return m_isReadyInterstitial;
                }

                return false;
            }

            //--------------------------------------------------
#region IRevMobListener implementation

            public void SessionIsStarted() {
                Debug.Log("AdRevmobAdapter > Session started.");
            }

            public void RewardedVideoLoaded() {
                Debug.Log("AdRevmobAdapter > RewardedVideoLoaded.");
            }

            public void RewardedVideoNotCompletelyLoaded() {
                Debug.Log("AdRevmobAdapter > RewardedVideoNotCompletelyLoaded.");
            }

            public void RewardedVideoStarted() {
                Debug.Log("AdRevmobAdapter > RewardedVideoStarted.");
            }

            public void RewardedVideoFinished() {
                Debug.Log("AdRevmobAdapter > RewardedVideoFinished.");
            }

            public void RewardedVideoCompleted() {
                Debug.Log("AdRevmobAdapter > RewardedVideoCompleted.");
            }

            public void RewardedPreRollDisplayed() {
                Debug.Log("AdRevmobAdapter > RewardedPreRollDisplayed.");
            }

            public void VideoLoaded() {
                Debug.Log("AdRevmobAdapter > VideoLoaded.");
            }

            public void VideoNotCompletelyLoaded() {
                Debug.Log("AdRevmobAdapter > VideoNotCompletelyLoaded.");
            }

            public void VideoStarted() {
                Debug.Log("AdRevmobAdapter > VideoStarted.");
            }

            public void VideoFinished() {
                Debug.Log("AdRevmobAdapter > VideoFinished.");
            }

            public void SessionNotStarted(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Session not started.");
            }

            public void AdDidReceive(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Ad did receive. " + revMobAdType);

                if (revMobAdType == "Fullscreen") {
                    m_isReadyRewardedVideo = true;
                    AddEvent(AdType.RewardVideo, AdEvent.Prepared);

                    m_isReadyInterstitial = false;
                    AddEvent(AdType.Interstitial, AdEvent.Prepared);
                }
            }

            public void AdDidFail(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Ad did fail." + revMobAdType);


                if (revMobAdType == "Fullscreen") {
                    m_isReadyRewardedVideo = false;
                    AddEvent(AdType.RewardVideo, AdEvent.PrepareFailure);

                    m_isReadyInterstitial = false;
                    AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
                }
            }

            public void AdDisplayed(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Ad displayed." + revMobAdType);
            }

            public void UserClickedInTheAd(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Ad clicked." + revMobAdType);
            }

            public void UserClosedTheAd(string revMobAdType) {
                Debug.Log("AdRevmobAdapter > Ad closed." + revMobAdType);
            }

            public void InstallDidReceive(string message) {
                Debug.Log("AdRevmobAdapter > Install received");
            }

            public void InstallDidFail(string message) {
                Debug.Log("AdRevmobAdapter > Install not received");
            }

            public void EulaIsShown() {
                Debug.Log("AdRevmobAdapter > Eula is displayed");
            }

            public void EulaAccepted() {
                Debug.Log("AdRevmobAdapter > Eula was accepted");
            }

            public void EulaRejected() {
                Debug.Log("AdRevmobAdapter > Eula was rejected");
            }

#endregion // IRevMobListener implementation

        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_REVMOB
