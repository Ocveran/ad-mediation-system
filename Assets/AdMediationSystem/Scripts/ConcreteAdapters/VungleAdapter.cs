
//#define _MS_VUNGLE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class VungleAdapter : AdNetworkAdapter {

#if _MS_VUNGLE

            string m_appId;
            string m_interstitialPlacementId;
            string m_incentivizedPlacementId;
            AdType m_currAdType;

            void Awake() {
                Vungle.onLogEvent += OnLogEvent;
                Vungle.onAdStartedEvent += OnAdStartedEvent;
                Vungle.onAdFinishedEvent += OnAdFinishedEvent;
            }

            void OnApplicationPause(bool pauseStatus) {
                if (pauseStatus) {
                    Vungle.onPause();
                }
                else {
                    Vungle.onResume();
                }
            }

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                m_appId = parameters["appId"];
                m_interstitialPlacementId = parameters["interstitialId"];
                m_incentivizedPlacementId = parameters["m_incentivizedId"];

                Dictionary<string, bool> placements = new Dictionary<string, bool>();
                placements.Add(m_interstitialPlacementId, false);
                placements.Add(m_incentivizedPlacementId, false);

                string[] array = new string[placements.Keys.Count];
                placements.Keys.CopyTo(array, 0);
                Vungle.init(m_appId, array);
            }

            public override void Prepare(AdType adType) {

            }

            public override bool Show(AdType adType) {
                if(IsReady(adType) && m_currAdType == AdType.None) {
                    m_currAdType = adType;
                    bool incentivized = m_currAdType == AdType.Incentivized;
                    string placementId = incentivized ? m_incentivizedPlacementId : m_interstitialPlacementId;

                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options.Add("incentivized", incentivized);
  
#if UNITY_ANDROID
                    //options.Add("orientation", VungleAdOrientation.AutoRotate);
#elif UNITY_IPHONE
                       //options.Add("orientation", VungleAdOrientation.All);
#endif
                    options.Add("userTag", "");
                    
                    Vungle.playAd(options, placementId);
                    return true;
                }
                return false;
            }

            public override void Hide(AdType adType) {
            }

            public override bool IsReady(AdType adType) {
                bool isReady = false;
                if (IsSupported(adType)) {
                    string placementId = (adType == AdType.Incentivized) ? m_incentivizedPlacementId : m_interstitialPlacementId;
                    isReady = Vungle.isAdvertAvailable(placementId);
                }
                return isReady;
            }

 
            void OnAdStartedEvent(string msg) {
                if (m_currAdType == AdType.None) {
                    return;
                }
                AddEvent(m_currAdType, AdEvent.Show);
            }

            void OnAdFinishedEvent(string msg, AdFinishedEventArgs args) {
                if(m_currAdType == AdType.None) {
                    return;
                }
                
                if (m_currAdType == AdType.Incentivized) {
                    if (args.IsCompletedView) {
                        AddEvent(m_currAdType, AdEvent.IncentivizedComplete);
                    }
                    else {
                        AddEvent(m_currAdType, AdEvent.IncentivizedIncomplete);
                    }
                }

                AddEvent(m_currAdType, AdEvent.Hide);
                m_currAdType = AdType.None;
            }

            void OnLogEvent(string message) {
                Debug.Log("VungleAdapter.OnLogEvent ~ " + message);
            }
            
#endif // _MS_VUNGLE

        }

    } // namespace AdMediation
} // namespace Virterix
