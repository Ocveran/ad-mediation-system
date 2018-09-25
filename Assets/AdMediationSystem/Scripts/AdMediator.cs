using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

namespace Virterix {
    namespace AdMediation {

        /// <summary>
        /// Handles specific a type advertising
        /// </summary>
        public class AdMediator : MonoBehaviour {

            const string _PREFIX_LAST_UNITID_SAVE_KEY = "adm_last_unit_";

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
            public string m_placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME;
            public bool m_isAutoFetchWhenHide;
            [Tooltip("If banner type ad is displayed longer than set value when ad hide, then performs Fetch. (In Seconds)")]
            public float m_minDisplayTimeForBannerTypaAd = 0f;
            [Tooltip ("Is continue show ad after restart the app from the interrupt place.")]
            public bool m_isContinueAfterEndSession;
            [Tooltip("When all networks don't fill ad, then Fetch will be performed automatically after the delay. " +
                "Negative value is disabled. (In Seconds)")]
            public float m_deferredFetchDelay = -1;

            public List<AdUnit> FetchUnits {
                get { return m_fetchUnits; }
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

            public bool IsLastNetworkSuccessfullyPrepared {
                get { return m_isLastNetworkSuccessfullyPrepared; }
            }
            bool m_isLastNetworkSuccessfullyPrepared;
  
            string LastAdUnitIdSaveKey {
                get {
                    return (_PREFIX_LAST_UNITID_SAVE_KEY + m_adType.ToString() + "_" + m_placementName).ToLower();
                }
            }

            #endregion // Properties

            AdUnit[] m_units;
            List<AdUnit> m_fetchUnits = new List<AdUnit>();
            protected AdUnit m_currUnit;
            int m_lastActiveUnitId;
            bool m_isBannerTypeAdViewVisibled = false;
            AdUnit m_checkingUnit;
            Coroutine m_procWaitNetworkPrepare;
            Coroutine m_procDeferredFetch;

            //===============================================================================
            #region MonoBehavior Methods
            //-------------------------------------------------------------------------------

            private void OnApplicationPause(bool pause) {
                if (pause) {
                    SaveLastActiveAdUnit();
                }
            }

            private void OnApplicationQuit() {
                SaveLastActiveAdUnit();
            }

            #endregion // MonoBehavior Methods

            //===============================================================================
            #region Methods
            //-------------------------------------------------------------------------------

            /// <summary>
            /// Should be called only once when initialize
            /// </summary>
            public void Initialize(AdUnit[] units) {
                m_units = units;
                m_lastActiveUnitId = -1;
                int index = 0;

                foreach (AdUnit unit in m_units) {
                    unit.OnEnable += OnUnitEnable;
                    unit.OnDisable += OnUnitDisable;
                    unit.Index = index++;
                }
                FillFetchUnits(true);
                
                if (m_isContinueAfterEndSession) {
                    m_lastActiveUnitId = PlayerPrefs.GetInt(LastAdUnitIdSaveKey, -1);
                    if (m_lastActiveUnitId != -1 && m_lastActiveUnitId < m_units.Length) {
                        m_fetchStrategy.Reset(this, m_units[m_lastActiveUnitId]);
                    }
                }
            }

            public virtual void Fetch() {

                if (m_fetchStrategy == null) {
                    Debug.LogWarning("AdMediator.Fetch() Not strategy of fetch! adType:" + m_adType);
                    return;
                }

                if (FetchUnits.Count == 0) {
                    FillFetchUnits(true);
                }

                if (m_procDeferredFetch != null) {
                    StopCoroutine(m_procDeferredFetch);
                    m_procDeferredFetch = null;
                }

                AdUnit unit = m_fetchStrategy.Fetch(this, FetchUnits.ToArray());

                if (unit != null) {
                    SetCurrentUnit(unit);

                    if (CurrentUnit != null ) {
                        CurrentUnit.DisplayTime = 0.0f;
                        if (m_isBannerTypeAdViewVisibled && (m_adType == AdType.Banner || m_adType == AdType.Native)) {
                            CurrentUnit.ShowAd();
                        }
                    }
                } else {
#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("AdMediator.Fetch() Not fetched ad unit. Unit count:" + m_fetchUnits.Count);
#endif
                }
            }

            public virtual void Show() {

                if (m_adType == AdType.Banner || m_adType == AdType.Native) {
                    m_isBannerTypeAdViewVisibled = true;
                    if (m_currUnit != null) {
                        m_currUnit.ShowAd();
                    }
                }
                
                if (m_currUnit != null) {
                    if (m_currUnit.AdNetwork.IsShouldInternetCheckBeforeShowAd(m_currUnit.AdapterAdType)) {
                        if (m_checkingUnit == null) {
                            m_checkingUnit = m_currUnit;
                            AdMediationSystem.Instance.InternetChecker.StartCheck(OnCheckInternetCompleted);
                        }
                    }
                    else {
                        _ShowCurrAdUnit();
                    }
                } else {
                    Fetch();
#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("AdMediator.Show() Not current unit");
#endif
                }
            }

            public virtual void Refresh() {
                if (m_currUnit != null) {
                    m_currUnit.PrepareAd();
                }
            }

            void OnCheckInternetCompleted(bool successConnection) {
                if (successConnection) {
                    _ShowCurrAdUnit();
                } else {
                    m_checkingUnit.ResetAd();
                    if (!ShowAnyReadyNetwork()) {
                        Fetch();
                    }
                }
                m_checkingUnit = null;
            }

            void _ShowCurrAdUnit() {
                if (m_currUnit != null) {
                    bool isShowSuccess = m_currUnit.IsAdReady ? m_currUnit.ShowAd() : false;
                    if (!isShowSuccess) {
                        if (!ShowAnyReadyNetwork()) {
                            Fetch();
                        }
                    }
                }
            }

            public virtual void Hide(string placementName) {
                if (m_adType == AdType.Banner || m_adType == AdType.Native) {
                    m_isBannerTypeAdViewVisibled = false;
                }

                if (m_currUnit != null) {
                    m_currUnit.HideAd();
                } else {
                    Debug.Log("AdMediator.Hide() Not current unit");
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

            bool ShowAnyReadyNetwork() {
                if (m_fetchUnits.Count == 0 || m_currUnit == null) {
                    return false;
                }

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

            /// <summary>
            /// Call with ignore auto fill from a fetch strategy. 
            /// Call with check auto fill from the mediator to keep an actual data in a fetch strategy.
            /// </summary>
            public void FillFetchUnits(bool ignoreAutoFillFlag = false) {
                if (m_fetchUnits.Count == 0) {
                    m_fetchStrategy.Reset(null, null);
                }

                if (ignoreAutoFillFlag || m_fetchStrategy.IsAllowAutoFillUnits()) {
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
                if (m_adType == AdType.Banner || m_adType == AdType.Native) {
                    unit.HideBannerTypeAdWithoutNotify();
                }
                unit.IsContainedInFetch = false;
                m_fetchUnits.Remove(unit);
            }

            void RequestToPrepare(AdUnit unit) {
                CancelWaitNetworkPrepare();
                float waitingTime = unit.FetchStrategyParams.m_waitingResponseTime;
                m_procWaitNetworkPrepare = StartCoroutine(ProcWaitNetworkPrepare(unit, waitingTime));
                unit.PrepareAd();
            }

            IEnumerator ProcWaitNetworkPrepare(AdUnit unit, float waitingTime) {
                float passedTime = 0.0f;
                float passedTimeForCheckAvailability = 0.0f;
                bool isCheckAvailabilityWhenPreparing = unit.AdNetwork.IsCheckAvailabilityWhenPreparing(unit.AdapterAdType);
                float interval = 0.4f;

                while (true) {
                    yield return new WaitForSeconds(interval);
                    passedTime += interval;
                    passedTimeForCheckAvailability += interval;

                    if (passedTime > waitingTime) {
                        unit.AdNetwork.NotifyEvent(unit.AdapterAdType, AdEvent.PrepareFailure, unit.PlacementData);
                        break;
                    } else if (isCheckAvailabilityWhenPreparing && passedTimeForCheckAvailability > 2.0f) {
                        if (unit.IsAdReady) {
                            unit.AdNetwork.NotifyEvent(unit.AdapterAdType, AdEvent.Prepared, unit.PlacementData);
                            break;
                        }
                    }
                }
                yield return null;
            }

            IEnumerator ProcDeferredFetch(float delay) {
                yield return new WaitForSecondsRealtime(delay);
                m_procDeferredFetch = null;
                Fetch();
                yield return null;
            }

            void CancelWaitNetworkPrepare() {
                if (m_procWaitNetworkPrepare != null) {
                    StopCoroutine(m_procWaitNetworkPrepare);
                    m_procWaitNetworkPrepare = null;
                }
            }

            void ResetCurrentUnit(AdUnit nextUnit) {
                if (m_currUnit != null) {
                    m_currUnit.AdNetwork.OnEvent -= OnCurrentNetworkEvent;

                    if (m_adType == AdType.Banner || m_adType == AdType.Native) {
                        if (nextUnit == null ||
                            (nextUnit != null && m_currUnit.AdNetwork.m_networkName != nextUnit.AdNetwork.m_networkName)) {
                            m_currUnit.HideBannerTypeAdWithoutNotify();
                        }
                    }

                    if (m_currUnit.AdNetwork.IsPrepareWhenChangeNetwork(m_currUnit.AdapterAdType)) {
                        m_currUnit.PrepareAd();
                    }

                    m_currUnit = null;
                }
            }

            void SetCurrentUnit(AdUnit unit) {
                if (!unit.Equals(m_currUnit)) {
                    ResetCurrentUnit(unit);

                    m_currUnit = unit;

#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("AdMediator.SetCurrentUnit() Type:" + m_adType + " Placement:" + m_currUnit.PlacementName +
                        " PlacementData:" + m_currUnit.PlacementData +
                        " Network:" + unit.AdNetwork.m_networkName + " IsReady: " + unit.IsAdReady);
#endif

                    if ((m_adType == AdType.Banner || m_adType == AdType.Native) && !m_isBannerTypeAdViewVisibled) {
                        m_currUnit.HideBannerTypeAdWithoutNotify();
                    } 

                    m_currUnit.AdNetwork.OnEvent += OnCurrentNetworkEvent;
                }

                m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdEvent.Selected, m_currUnit.PlacementData);

                if (m_currUnit.IsAdReady) {
                    m_currUnit.AdNetwork.NotifyEvent(m_currUnit.AdapterAdType, AdEvent.Prepared, m_currUnit.PlacementData);
                } else {
                    RequestToPrepare(m_currUnit);
                }
            }

            void SaveLastActiveAdUnit() {
                if (m_isContinueAfterEndSession) {
                    if (m_currUnit != null && m_lastActiveUnitId >= 0) {
                        PlayerPrefs.SetInt(LastAdUnitIdSaveKey, m_lastActiveUnitId);
                    }
                }
            }

            void OnUnitEnable(AdUnit enabledUnit) {
                AddUnitToFetch(enabledUnit);
            }

            void OnUnitDisable(AdUnit disabledUnit) {
                RemoveUnitFromFetch(disabledUnit);
            }

            void OnCurrentNetworkEvent(AdNetworkAdapter network, AdType adType, AdEvent adEvent, string placement) {
                if (adType != m_currUnit.AdapterAdType) {
                    return;
                }
                else if(m_currUnit.PlacementName != placement) {
                    return;
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AdMediator.OnNetworkEvent() Type:" + m_adType + " placement: " + placement + " : " + m_currUnit.PlacementName +
                    " Intrnl Type:" + m_currUnit.AdapterAdType + " Network:" + network.m_networkName + " Event:" + adEvent);
#endif

                AdMediationSystem.NotifyAdNetworkEvent(network, m_adType, adEvent, placement);

                if (adEvent == AdEvent.PrepareFailure || adEvent == AdEvent.Hide || adEvent == AdEvent.Show) {
                    if (m_currUnit != null) {
                        m_lastActiveUnitId = m_currUnit.Index;
                    }
                }

                switch (adEvent) {
                    case AdEvent.PrepareFailure:
                        m_isLastNetworkSuccessfullyPrepared = false;
                        CancelWaitNetworkPrepare();
                        network.SaveFailedLoadTime(m_currUnit.AdapterAdType);
                        RemoveUnitFromFetch(m_currUnit);

                        if (m_fetchUnits.Count == 0) {
                            FillFetchUnits();

                            if (m_deferredFetchDelay >= 0.0f) {
                                if (m_procDeferredFetch != null) {
                                    StopCoroutine(m_procDeferredFetch);
                                }
                                m_procDeferredFetch = StartCoroutine(ProcDeferredFetch(m_deferredFetchDelay));
                            }
                        } else {
                            Fetch();
                        }

                        break;
                    case AdEvent.Prepared:
                        m_isLastNetworkSuccessfullyPrepared = true;
                        CancelWaitNetworkPrepare();
                        FillFetchUnits();

                        break;
                    case AdEvent.Hide:
                        if (m_isAutoFetchWhenHide) {
                            bool isFetchPerform = true;

                            if (m_currUnit != null && m_minDisplayTimeForBannerTypaAd > 0.1f) {
                                isFetchPerform = m_currUnit.DisplayTime >= m_minDisplayTimeForBannerTypaAd;
                                if (isFetchPerform) m_currUnit.DisplayTime = 0.0f;
                            }

                            if (isFetchPerform) {
                                Fetch();
                            }
                        }

                        break;
                }
            }

        }

        #endregion // Methods

    } // namespace AdMediation
} // namespace Virterix
