
//#define _MS_BEACHFRONT

#if _MS_BEACHFRONT

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class BeachfrontAdapter : AdNetworkAdapter, AdListener {

            UnityBFIO m_unityBFIO;

            string m_appID = "";
            string m_adUnitID = "";

            bool m_isInterstitialReady;

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

#if UNITY_ANDROID
                m_appID = parameters["AppID"];
                m_adUnitID = parameters["AdUnitID"];
#endif

                m_unityBFIO = UnityBFIO.getInstance(m_appID, m_adUnitID, this);
            }

            public override void Prepare(AdType adType) {
                if (IsSupported(adType)) {
                    m_unityBFIO.requestAD(m_appID, m_adUnitID);
                }
            }

            public override void Show(AdType adType) {
                if (IsReady(adType)) {
                    m_unityBFIO.showAD();
                }
            }

            public override void Hide(AdType adType) {
            }

            public override bool IsReady(AdType adType) {
                if (IsSupported(adType)) {
                    return m_isInterstitialReady;
                }
                return false;
            }

            //--------------------------------------------------
#region AdListener implementation

            public void onInterstitialFailed(string message) {
                Debug.Log("BeachfrontAdapter.onInterstitialFailed: " + message);
                m_isInterstitialReady = false;
                AddEvent(AdType.RewardVideo, AdEvent.PrepareFailure);
            }

            public void onInterstitialStarted(string message) {

            }

            public void onInterstitialClicked(string message) {
                AddEvent(AdType.RewardVideo, AdEvent.Click);
            }

            public void onInterstitialDismissed(string message) {
                AddEvent(AdType.RewardVideo, AdEvent.RewardAdIncomplete);
            }

            public void onInterstitialCompleted(string message) {
                AddEvent(AdType.RewardVideo, AdEvent.RewardAdComplete);
            }

            public void onReceiveInterstitial(string message) {
                Debug.Log("BeachfrontAdapter.onReceiveInterstitial: " + message);
                m_isInterstitialReady = true;
                AddEvent(AdType.RewardVideo, AdEvent.Prepared);
            }

#endregion // AdListener implementation

        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_BEACHFRONT