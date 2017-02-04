using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Virterix {
    namespace AdMediation {
        
        /// <summary>
        /// Handles specific a type advertising
        /// </summary>
        public class AdMediator : MonoBehaviour {

            //===============================================================================
            #region Properties
            //-------------------------------------------------------------------------------

            public IFetchStrategy FetchStrategy {
                set {
                    m_fetchStrategy = value;
                }
                get {
                    return m_fetchStrategy;
                }
            }
            IFetchStrategy m_fetchStrategy;

            public AdType m_adType;
            public bool m_autoFetchWhenHide;

            public List<AdUnit> FetchUnits {
                get { return m_fetchUnits; }
            }

            public int AllUnitCount {
                get { return m_units.Length; }
            }

            public int FetchUnitCount {
                get { return m_fetchUnits.Count; }
            }

            public AdUnit CurrentUnit {
                get { return m_currUnit; }
            }

            public string CurrentNetworkName {
                get {
                    if (CurrentUnit != null) {
                        return CurrentUnit.AdNetwork.m_networkName;
                    } else {
                        return null;
                    }
                }
            }

            public bool IsReadyToShow {
                get {
                    bool ready = false;
                    foreach(AdUnit unit in m_fetchUnits) {
                        if(unit.IsAdReady) {
                            ready = true;
                            break;
                        }
                    }
                    return ready;
                }
            }

            public bool IsCurrentNetworkReadyToShow {
                get {
                    bool ready = false;
                    if (CurrentUnit != null) {
                        ready = CurrentUnit.IsAdReady;
                    }
                    return ready;
                }
            }

            #endregion // Properties

            AdUnit[] m_units;
            List<AdUnit> m_fetchUnits = new List<AdUnit>();
            protected AdUnit m_currUnit;
            bool m_isBannerVisibled = true;

            //===============================================================================
            #region // Methods
            //-------------------------------------------------------------------------------

            /// <summary>
            /// Should be called only once when initialize
            /// </summary>
            public void InitUnits(AdUnit[] units) {
                m_units = units;
                foreach (AdUnit unit in m_units) {
                    unit.OnEnable += OoUnitEnable;
                    unit.OnDisable += OnUnitDisable;
                }
                FillFetchUnits(true);
            }

            public virtual void Fetch() {
                if (m_fetchStrategy == null) {
                    Debug.LogWarning("[AdMediator.Fetch] Not strategy of fetch! adType:" + m_adType);
                    return;
                }

                AdUnit unit = m_fetchStrategy.Fetch(this, FetchUnits.ToArray());

                if (unit != null) {
                    SetCurrentUnit(unit);
                } else {
                    Debug.Log("[AdMediator.Fetch] Not fetched ad unit. Unit count:" + m_fetchUnits.Count);
                }
            }

            public virtual void Show() {
                m_isBannerVisibled = true;

                if (m_currUnit != null) {
                    if (m_currUnit.AdapterAdType == AdType.Banner) {
                        m_currUnit.AdNetwork.IsBannerVisibled = m_isBannerVisibled;
                    }

                    bool isShowSucceed = m_currUnit.ShowAd();
                    if (!isShowSucceed && m_currUnit.AdapterAdType != AdType.Banner) {
                        if (!ShowReadyNetwork()) {
                            Fetch();
                        }
                    }
                } else {
                    Fetch();
                    Debug.Log("[AdMediator.Show] Not current network");
                }
            }

            public virtual void Hide() {
                m_isBannerVisibled = false;

                if (m_currUnit != null) {
                    if (m_currUnit.AdapterAdType == AdType.Banner) {
                        m_currUnit.AdNetwork.IsBannerVisibled = m_isBannerVisibled;
                    }

                    m_currUnit.HideAd();
                } else {
                    Debug.Log("[AdMediator.Hide] Not current network");
                }
            }

            public int FindIndexInFetchUnits(AdUnit unit) {
                int currIndex = 0;
                for (int i = 0; i < m_fetchUnits.Count; i++) {
                    if(m_fetchUnits[i] == unit) {
                        currIndex = i;
                        break;
                    }
                }
                return currIndex;
            }

            bool ShowReadyNetwork() {              
                if(m_fetchUnits.Count == 0 || m_currUnit == null) {
                    return false;
                }

                FillFetchUnits(true);
                AdUnit readyUnit = null;
                int currIndex = FindIndexInFetchUnits(m_currUnit);

                int startIndex = currIndex + 1;
                startIndex = startIndex >= m_fetchUnits.Count ? 0 : startIndex;

                for (int i = startIndex; i != currIndex;) {
                    AdUnit unit = m_fetchUnits[i];
                    if (unit.IsAdReady) {
                        readyUnit = unit;
                        break;
                    }

                    i++;
                    if (i >= m_fetchUnits.Count) {
                        i = 0;
                    }
                }

                if (readyUnit != null) {
                    SetCurrentUnit(readyUnit);
                    m_fetchStrategy.Reset(this, readyUnit);
                    return readyUnit.ShowAd();
                }
                return false;
            }

            public void FillFetchUnits(bool ignoreAutoFlag = false) {
                if (ignoreAutoFlag || m_fetchStrategy.IsAllowAutoFillUnits()) {
                    if (m_fetchUnits.Count != m_units.Length) {
                        m_fetchUnits.Clear();
                        foreach (AdUnit unit in m_units) {
                            if (unit.IsEnabled && unit.AdNetwork.IsTimeout(unit.AdapterAdType)) {
                                AddUnitToFetch(unit);
                            }
                        }
                    }
                }
            }

            void AddUnitToFetch(AdUnit unit) {
                unit.IsContainedInFetch = true;
                m_fetchUnits.Add(unit);
            }

            void RemoveUnitFromFetch(AdUnit unit) {
                unit.IsContainedInFetch = false;
                m_fetchUnits.Remove(unit);
            }

            void RequestToPrepare(AdUnit unit) {
                CancelWaitNetworkPrepare();
                float waitingTime = unit.FetchStrategyParams.m_waitingResponseTime;
                StartCoroutine("WaitNetworkPrepare", waitingTime);
                unit.PrepareAd();
            }

            IEnumerator WaitNetworkPrepare(float waitingTime) {
                float passedTime = 0.0f;
                float passedTimeForCheckAvailability = 0.0f;
                bool isCheckAvailabilityWhenPreparing = m_currUnit.AdNetwork.IsCheckAvailabilityWhenPreparing(m_adType);
                float interval = 0.2f;

                while (true) {
                    yield return new WaitForSeconds(interval);
                    passedTime += interval;
                    passedTimeForCheckAvailability += interval;

                    if (passedTime > waitingTime) {
                        m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdNetworkAdapter.AdEvent.PrepareFailure);
                        break;
                    } else if (isCheckAvailabilityWhenPreparing && passedTimeForCheckAvailability > 2.0f) {
                        if (m_currUnit.IsAdReady) {
                            m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdNetworkAdapter.AdEvent.Prepared);
                            break;
                        }
                    }
                }
                yield return null;
            }

            void CancelWaitNetworkPrepare() {
                this.StopCoroutine("WaitNetworkPrepare");
            }

            void ResetCurrentUnit() {
                if (m_currUnit != null) {
                    if (m_currUnit.AdapterAdType == AdType.Banner) {
                        m_currUnit.HideAd();
                    }

                    m_currUnit.AdNetwork.OnEvent -= OnCurrentNetworkEvent;
                    if (m_currUnit.AdNetwork.IsPrepareWhenChangeNetwork(m_adType)) {
                        m_currUnit.PrepareAd();
                    }

                    m_currUnit = null;
                }
            }

            void SetCurrentUnit(AdUnit unit) {
                if (!unit.Equals(m_currUnit)) {
                    ResetCurrentUnit();

                    m_currUnit = unit;
                    m_currUnit.AdNetwork.IsBannerVisibled = m_isBannerVisibled;

                    // If a banner wasn't hide from mediator then show a banner of current network
                    if (m_currUnit.AdapterAdType == AdType.Banner && m_isBannerVisibled) {
                        m_currUnit.ShowAd();
                    }

                    m_currUnit.AdNetwork.OnEvent += OnCurrentNetworkEvent;
                }

                m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdNetworkAdapter.AdEvent.Selected);

                if (m_currUnit.IsAdReady) {
                    m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdNetworkAdapter.AdEvent.Prepared);
                } else {
                    RequestToPrepare(m_currUnit);
                }
            }

            void OoUnitEnable(AdUnit unit) {
                AddUnitToFetch(unit);
            }

            void OnUnitDisable(AdUnit unit) {
                RemoveUnitFromFetch(unit);
            }

            void OnCurrentNetworkEvent(AdNetworkAdapter network, AdType adType, AdNetworkAdapter.AdEvent adEvent) {
                if (adType != m_currUnit.AdapterAdType) {
                    return;
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[AdMediator.OnNetworkEvent] Type:" + m_adType + " Intrnl Type:" + m_currUnit.AdapterAdType + " Network:" + network.m_networkName + " Event:" + adEvent);
#endif

                AdMediationSystem.NotifyAdNetworkEvent(network, m_adType, adEvent);

                switch (adEvent) {
                    case AdNetworkAdapter.AdEvent.PrepareFailure:

                        CancelWaitNetworkPrepare();
                        network.SaveFailedLoadTime(m_currUnit.AdapterAdType);
                        RemoveUnitFromFetch(m_currUnit);

                        if (m_fetchUnits.Count == 0) {
                            FillFetchUnits();
                        } else {
                            Fetch();
                        }

                        break;
                    case AdNetworkAdapter.AdEvent.Prepared:

                        CancelWaitNetworkPrepare();
                        FillFetchUnits();

                        break;
                    case AdNetworkAdapter.AdEvent.Hide:

                        if (m_autoFetchWhenHide) {
                            Fetch();
                        }

                        break;
                }
            }

        }

        #endregion // Methods

    } // namespace AdMediation
} // namespace Virterix
