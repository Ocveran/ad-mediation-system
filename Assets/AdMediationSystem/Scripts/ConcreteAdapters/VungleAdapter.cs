
#define _MS_VUNGLE

#if _MS_VUNGLE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class VungleAdapter : AdNetworkAdapter {

            string m_appId;
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

                string androidAppId = "";
                string iosAppId = "";
                string winAppId = "";

                switch(Application.platform) {
                    case RuntimePlatform.Android:
                        androidAppId = m_appId;
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        iosAppId = m_appId;
                        break;
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerARM:
                        winAppId = m_appId;
                        break;
                }
                Vungle.init(androidAppId, iosAppId, winAppId);
            }

            public override void Prepare(AdType adType) {

            }

            public override bool Show(AdType adType) {
                if(IsReady(adType) && m_currAdType == AdType.None) {
                    m_currAdType = adType;
                    bool incentivized = m_currAdType == AdType.Incentivized;
                    
                    Dictionary<string, object> options = new Dictionary<string, object>();
                    options.Add("incentivized", incentivized);
#if UNITY_ANDROID
                    //options.Add("orientation", VungleAdOrientation.AutoRotate);
#elif UNITY_IPHONE
                       //options.Add("orientation", VungleAdOrientation.All);
#endif
                    options.Add("userTag", "");
                    
                    Vungle.playAdWithOptions(options);
                    return true;
                }
                return false;
            }

            public override void Hide(AdType adType) {
            }

            public override bool IsReady(AdType adType) {
                bool isReady = false;
                if (IsSupported(adType)) {
                    isReady = Vungle.isAdvertAvailable();
                }
                return isReady;
            }

 
            void OnAdStartedEvent() {
                if (m_currAdType == AdType.None) {
                    return;
                }
                AddEvent(m_currAdType, AdEvent.Show);
            }

            void OnAdFinishedEvent(AdFinishedEventArgs args) {
                if(m_currAdType == AdType.None) {
                    return;
                }
                
                if (m_currAdType == AdType.Incentivized) {
                    if (Mathf.Approximately((float)args.TimeWatched, (float)args.TotalDuration)) {
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
        }

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_VUNGLE