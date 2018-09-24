
using UnityEngine;
using System.Collections;
using System;

namespace Virterix {
    namespace AdMediation {

        /// <summary>
        /// Defines advertising unit
        /// </summary>
        public class AdUnit {

            public AdUnit(string placementName, AdType adType, AdNetworkAdapter network, IFetchStrategyParams strategyParams, bool enabled) {
                m_adapterAdType = adType;
                m_network = network;
                m_fetchStrategyParams = strategyParams;
                IsEnabled = enabled;
                m_placementName = placementName;
            }

            public AdType AdapterAdType { get { return m_adapterAdType; } }
            public AdNetworkAdapter AdNetwork { get { return m_network; } }
            public IFetchStrategyParams FetchStrategyParams { get { return m_fetchStrategyParams; } }
            public int Index { get; set; }
            public float DisplayTime { get; set; }
            public string PlacementName {
                get { return m_placementName; }
            }
            public AdNetworkAdapter.PlacementData PlacementData {
                get {
                    if (m_placementData == null && !m_isGotPlacementData) {
                        m_placementData = AdNetwork.GetPlacementData(AdapterAdType, PlacementName);
                        m_isGotPlacementData = true;
                    }
                    return m_placementData;
                }
            }
            AdNetworkAdapter.PlacementData m_placementData;
            bool m_isGotPlacementData;

            public event Action<AdUnit> OnEnable;
            public event Action<AdUnit> OnDisable;

            public bool IsAdReady {
                get {
                    return IsEnabled ? m_network.IsReady(m_adapterAdType, PlacementData) : false;
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

            /// <returns>True when successfully shown ad</returns>
            public bool ShowAd() {
                bool showed = false;
                if (IsEnabled) {
                    showed = m_network.Show(m_adapterAdType, PlacementData);
                }
                Impressions = showed ? Impressions + 1 : Impressions;

                if (m_adapterAdType == AdType.Banner || m_adapterAdType == AdType.Native) {
                    if (showed) {
                        m_isShown = true;
                        m_startImpressionTime = Time.unscaledTime;
                    }
                }
                return showed;
            }

            public void HideAd() {
                UpdateDisplayTimeWhenAdHidden();
                m_network.Hide(m_adapterAdType, PlacementData);
            }

            public void HideBannerTypeAdWithoutNotify() {
                UpdateDisplayTimeWhenAdHidden();
                m_network.HideBannerTypeAdWithoutNotify(m_adapterAdType, PlacementData);
            }

            public void PrepareAd() {
                m_network.Prepare(m_adapterAdType, PlacementData);
            }

            public void ResetAd() {
                m_network.ResetAd(m_adapterAdType, PlacementData);
            }

            public void IncrementFetchCount() {
                m_fetchCount++;
            }

            void UpdateDisplayTimeWhenAdHidden() {
                if (m_adapterAdType == AdType.Banner || m_adapterAdType == AdType.Native) {
                    if (m_isShown) {
                        m_isShown = false;
                        float passedTime = Time.unscaledTime - m_startImpressionTime;
                        DisplayTime += passedTime;
                    }
                }
            }

            string m_placementName;
            AdType m_adapterAdType;
            AdNetworkAdapter m_network;
            IFetchStrategyParams m_fetchStrategyParams;
            public Hashtable m_parameters = new Hashtable();
            float m_startImpressionTime;
            bool m_isShown;
        }

    } // namespace AdMediation
} // namespace Virterix