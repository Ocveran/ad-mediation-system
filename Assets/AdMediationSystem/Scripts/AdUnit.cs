
using System.Collections;
using System;

namespace Virterix {
    namespace AdMediation {

        /// <summary>
        /// Defines advertising unit
        /// </summary>
        public class AdUnit {

            public AdUnit(AdType adType, AdNetworkAdapter network, IFetchStrategyParams strategyParams, bool enabled) {
                m_adapterAdType = adType;
                m_network = network;
                m_fetchStrategyParams = strategyParams;
                IsEnabled = enabled;
            }

            public AdType AdapterAdType { get { return m_adapterAdType; } }
            public AdNetworkAdapter AdNetwork { get { return m_network; } }
            public IFetchStrategyParams FetchStrategyParams { get { return m_fetchStrategyParams; } }

            public event Action<AdUnit> OnEnable;
            public event Action<AdUnit> OnDisable;

            public bool IsAdReady {
                get {
                    return IsEnabled ? m_network.IsReady(m_adapterAdType) : false;
                }
            }

            public int Impressions {
                get { return m_impressions; }
                set {
                    m_impressions = value;
                    if (m_fetchStrategyParams.m_impressionsInSession > 0) {
                        if (m_impressions >= m_fetchStrategyParams.m_impressionsInSession) {
                            IsEnabled = false;
                        }
                    }
                }
            }
            int m_impressions;

            public bool IsEnabled {
                get { return m_enabled; }
                set {
                    m_enabled = value;
                    if (m_enabled) {
                        if (OnEnable != null) OnEnable(this);
                    }
                    else {
                        if (OnDisable != null) OnDisable(this);
                    }
                }
            }
            bool m_enabled;

            public int FetchCount {
                get { return m_fetchCount; }
            }
            int m_fetchCount;

            public bool IsContainedInFetch {
                get; set;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>True when successfully shown ad</returns>
            public bool ShowAd() {
                bool showed = false;
                if (IsEnabled) {
                    showed = m_network.Show(m_adapterAdType);
                }
                Impressions = showed ? Impressions + 1 : Impressions;
                return showed;
            }

            public void HideAd() {
                m_network.Hide(m_adapterAdType);
            }

            public void PrepareAd() {
                m_network.Prepare(m_adapterAdType);
            }

            public void IncrementFetchCount() {
                m_fetchCount++;
            }

            AdType m_adapterAdType;
            AdNetworkAdapter m_network;
            IFetchStrategyParams m_fetchStrategyParams;
            public Hashtable m_parameters = new Hashtable();
        }

    } // namespace AdMediation
} // namespace Virterix