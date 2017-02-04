using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

namespace Virterix {
    namespace AdMediation {

        public abstract class AdNetworkAdapter : MonoBehaviour {

            public struct EventParam {
                public AdType m_adType;
                public  AdEvent m_adEvent;
            }

            /// <summary>
            /// Describes the parameters of the disabling network from handling when failed load
            /// </summary>
            public struct TimeoutParams {
                public AdType m_adType;
                public float m_timeout;

                public float FailedLoadTime {
                    set {
                        m_failedLoadTime = value;
                        m_isSetupFailedLoadTime = true;
                    }
                    get { return m_failedLoadTime; }
                }

                bool m_isSetupFailedLoadTime;
                float m_failedLoadTime;
                    
                public bool IsTimeout {
                    get {
                        bool active = false;
                        bool canUsed = m_adType != AdType.None && m_timeout > 0.0001f;

                        if(canUsed && m_isSetupFailedLoadTime) {
                            float elapsedTime = Time.realtimeSinceStartup - m_failedLoadTime;
                            active = elapsedTime < m_timeout;
                            m_isSetupFailedLoadTime = active;
                        }
                        return active;
                    }
                }
            }

            [System.Serializable]
            public struct AdParam {
                public AdType m_adType;
                public bool m_isCheckAvailabilityWhenPreparing;
                public bool m_isPrepareWhenChangeNetwork;
            }

            public enum AdEvent {
                None = 0,
                Selected,
                Prepared,
                Show,
                Click,
                Hide,
                PrepareFailure,
                IncentivizedComplete,
                IncentivizedIncomplete
            }

            public string m_networkName;
            public AdParam[] m_adSupportParams;

            public bool IsBannerVisibled {
                get {
                    return m_isBannerVisible;
                }
                set {
                    m_isBannerVisible = value;
                }
            }
            protected bool m_isBannerVisible = false;

            public event Action<AdNetworkAdapter, AdType, AdEvent> OnEvent = delegate { };

            List<IFetchStrategyParams> m_fetchStrategyParams = new List<IFetchStrategyParams>();
            List<EventParam> m_events = new List<EventParam>();
            TimeoutParams[] m_timeoutParameters;

			protected void Update() {
                UpdateEvents();
            }

			protected void OnDisable() {
                UpdateEvents();
            }

            void UpdateEvents() {
                if (m_events.Count > 0) {
                    EventParam[] eventParams = m_events.ToArray();
                    m_events.Clear();
                    foreach (EventParam eventParam in eventParams) {
                        NotifyEvent(eventParam.m_adType, eventParam.m_adEvent);
                    }
                }
            }

            protected virtual void InitializeParameters(Dictionary<string, string> parameters) {
                m_timeoutParameters = new TimeoutParams[m_adSupportParams.Length];

                for (int i = 0; i < m_timeoutParameters.Length; i++) {
                    TimeoutParams timeoutParams = new TimeoutParams();
                    timeoutParams.m_adType = m_adSupportParams[i].m_adType;

                    if (parameters != null) {
                        string timeoutKey = "timeout-" + AdTypeConvert.AdTypeToString(timeoutParams.m_adType);
                        string timeoutParam = "";

                        if(parameters.ContainsKey(timeoutKey)) {
                            timeoutParam = parameters[timeoutKey];
                        }

                        if (timeoutParam.Length > 0) {
                            timeoutParams.m_timeout = (float)System.Convert.ToDouble(timeoutParam);
                        }
                    }
                    m_timeoutParameters[i] = timeoutParams;
                }
            }

            public virtual void Initialize(Dictionary<string, string> parameters = null) {
                if (parameters != null) {
                    InitializeParameters(parameters);
                }
                Debug.Log("[AdNetworkAdapter.Initialize] Initialize network adapter: " + m_networkName);
            }

            public virtual void SetEnabled(bool state) {
                this.enabled = state;
            }

            public abstract void Prepare(AdType adType);

            public abstract bool Show(AdType adType);

            public abstract void Hide(AdType adType);

            public virtual bool IsSupported(AdType adType) {
                AdParam adSupportParam = GetAdParam(adType);
                bool isSupported = adSupportParam.m_adType != AdType.None;
                return isSupported;
            }

            public bool IsCheckAvailabilityWhenPreparing(AdType adType) {
                AdParam adSupportParam = GetAdParam(adType);
                return adSupportParam.m_isCheckAvailabilityWhenPreparing;
            }

            public bool IsPrepareWhenChangeNetwork(AdType adType) {
                AdParam adSupportParam = GetAdParam(adType);
                return adSupportParam.m_isPrepareWhenChangeNetwork;
            }

            AdParam GetAdParam(AdType adType) {
                AdParam adSupportParam = new AdParam();
                foreach (AdParam param in m_adSupportParams) {
                    if (param.m_adType == adType) {
                        adSupportParam = param;
                        break;
                    }
                }
                return adSupportParam;
            }

            public abstract bool IsReady(AdType adType);

            public void AddEvent(AdType adType, AdEvent adEvent) {
                EventParam eventParam = new EventParam();
                eventParam.m_adType = adType;
                eventParam.m_adEvent = adEvent;
                m_events.Add(eventParam);
            }

            public bool IsTimeout(AdType adType) {
                TimeoutParams failedInfo = GetTimeoutParams(adType);
                bool state = !failedInfo.IsTimeout;
                return state;
            }

            public void SetFetchStrategyParams(AdType adType, IFetchStrategyParams parameters) {
                AddOrReplaceFetchStrategyParams(adType, parameters);
            }

            public void NotifyEvent(AdType adType, AdEvent adEvent) {
                OnEvent(this, adType, adEvent);
            }

            public IFetchStrategyParams GetFetchStrategyParams(AdType adType) {
                IFetchStrategyParams foundParams = null;
                foreach (IFetchStrategyParams param in m_fetchStrategyParams) {
                    if (param.m_adsType == adType) {
                        foundParams = param;
                        break;
                    }
                }
                return foundParams;
            }

            public TimeoutParams GetTimeoutParams(AdType adType) {
                TimeoutParams foundParams = new TimeoutParams();
                if (m_timeoutParameters != null) {
                    foreach (TimeoutParams timeoutParams in m_timeoutParameters) {
                        if (timeoutParams.m_adType == adType) {
                            foundParams = timeoutParams;
                        }
                    }
                }
                return foundParams;
            }

            public void SaveFailedLoadTime(AdType adType) {
                for (int i = 0; i < m_timeoutParameters.Length; i++) {
                    if(m_timeoutParameters[i].m_adType == adType) {
                        m_timeoutParameters[i].FailedLoadTime = Time.realtimeSinceStartup;
                        break;
                    }
                }
            }

            void AddOrReplaceFetchStrategyParams(AdType adType, IFetchStrategyParams parameters) {
                for (int i = 0; i < m_fetchStrategyParams.Count; i++) {
                    if (m_fetchStrategyParams[i].m_adsType == adType) {
                        m_fetchStrategyParams[i] = parameters;
                        return;
                    }
                }
                m_fetchStrategyParams.Add(parameters);
            }

        }

    } // namespace AdMediation
} // namespace Virterix
