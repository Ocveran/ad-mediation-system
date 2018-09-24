using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;
using System.Linq;
using System.IO;

namespace Virterix {
    namespace AdMediation {

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

        public interface IPlacementParameters {
            AdType PlacementAdType { get; }
            string PlacementName { get; }
        }

        public class AdNetworkAdapter : MonoBehaviour {

            public const string _PLACEMENT_DEFAULT_NAME = "Default";
            public const string _PLACEMENT_PARAMETERS_ROOT_PATH = "Resources/AdPlacementParameters/";
            public const string _PLACEMENT_PARAMETERS_FILE_EXTENSION = ".asset";

            //_______________________________________________________________________________
            #region Classes & Structs
            //-------------------------------------------------------------------------------
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
                        bool canUsed = m_adType != AdType.Unknown && m_timeout > 0.0001f;

                        if (canUsed && m_isSetupFailedLoadTime) {
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

            public struct EventParam {
                public AdType m_adType;
                public PlacementData m_placementData;
                public AdEvent m_adEvent;
            }
            
            public enum AdState {
                Uncertain = 0,
                Loading,
                Available,
                LoadFailure
            }

            public class PlacementData {
                public PlacementData() {
                }

                public PlacementData(AdType adType, string adID, string placementName = _PLACEMENT_DEFAULT_NAME) {
                    m_adType = adType;
                    m_adID = adID;
                    PlacementName = placementName;
                    m_isDefault = PlacementName == _PLACEMENT_DEFAULT_NAME;
                }

                public string PlacementName {
                    get {
                        return m_placementName;
                    }
                    set {
                        m_placementName = value;
                        m_isDefault = m_placementName == _PLACEMENT_DEFAULT_NAME;
                    }
                }
                string m_placementName;

                public bool IsDefault {
                    get {
                        return m_isDefault;
                    }
                }
                bool m_isDefault = true;

                public AdType m_adType;
                public string m_adID;
                public bool m_isBannerAdTypeVisibled;
                public Vector2 m_bannerCoordinates;
                public AdState m_state = AdState.Uncertain;
                public bool m_lastAdPrepared;
                public bool m_enabledState;
                public object m_adView;
                public IPlacementParameters m_placementParams;
            }
            #endregion Classes & Structs

            public event Action<AdNetworkAdapter, AdType, AdEvent, string> OnEvent = delegate { };

            public string m_networkName;
            public AdParam[] m_adSupportParams;

            //_______________________________________________________________________________
            #region Properties
            //-------------------------------------------------------------------------------
            string PlacementParametersPath {
                get {
                    string path = "";
                    if (PlacementParametersFolder.Length > 0) {
                        path = _PLACEMENT_PARAMETERS_ROOT_PATH +
                        AdMediationSystem.Instance.m_projectName +
                        "/" + PlacementParametersFolder;
                    }
                    return path;
                }
            }

            protected virtual string PlacementParametersFolder {
                get {
                    return "";
                }
            }

            #endregion Properties

            bool[] m_arrLastAdPreparedState;
            bool[] m_arrEnableState;
            AdState[] m_arrAdState;

            List<EventParam> m_events = new List<EventParam>();
            TimeoutParams[] m_timeoutParameters;
            protected List<IPlacementParameters> m_placementParameters = new List<IPlacementParameters>();
            List<PlacementData> m_placementDataList = new List<PlacementData>();

            //_______________________________________________________________________________
            #region MonoBehavior Methods
            //-------------------------------------------------------------------------------
            protected void Update() {
                UpdateEvents();
            }

			protected void OnDisable() {
                UpdateEvents();
            }
            #endregion MonoBehavior Methods

            //_______________________________________________________________________________
            #region Public Methods
            //-------------------------------------------------------------------------------

            public virtual void Initialize(Dictionary<string, string> parameters = null, JSONArray placements = null) {
                if (parameters != null) {
                    InitializeParameters(parameters, placements);
                }

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AdNetworkAdapter.Initialize() Initialize network adapter: " + m_networkName + " placements:" + m_placementDataList.Count);
#endif
            }

            /// <summary>
            /// Not working
            /// </summary>
            public virtual void DisableWhenInitialize() {

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("AdNetworkAdapter.DisableWhenInitialize() " + m_networkName);
#endif

                this.enabled = false;
                int count = Enum.GetNames(typeof(AdType)).Length;
                for (int i = 0; i < count; i++) {
                    //SetEnabledState((AdType)i, false);
                }
            }

            public virtual void Prepare(AdType adType, PlacementData placement = null) { }

            public virtual bool Show(AdType adType, PlacementData placement = null) { return false; }

            public virtual void Hide(AdType adType, PlacementData placement = null) { }

            public virtual void HideBannerTypeAdWithoutNotify(AdType adType, PlacementData placement = null) {
            }

            public virtual void ResetAd(AdType adType, PlacementData placement = null) {
            }

            public virtual bool IsShouldInternetCheckBeforeShowAd(AdType adType) {
                return false;
            }

            public virtual bool IsSupported(AdType adType) {
                AdParam adSupportParam = GetAdParam(adType);
                bool isSupported = adSupportParam.m_adType != AdType.Unknown;
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

            public AdState GetAdState(AdType adType, PlacementData placement) {
                if (placement == null) {
                    return m_arrAdState[(int)adType];
                }
                else {
                    return placement.m_state;
                }
            }

            public void SetAdState(AdType adType, PlacementData placement, AdState state) {
                if (placement == null) {
                    m_arrAdState[(int)adType] = state;
                }
                else {
                    placement.m_state = state;
                }
            }

            public bool GetEnabledState(AdType adType, PlacementData placement) {
                if (placement == null) {
                    return m_arrEnableState[(int)adType];
                }
                else {
                    return placement.m_enabledState;
                }
            }

            public void SetEnabledState(AdType adType, PlacementData placement, bool state) {
                if (placement == null) {
                    m_arrEnableState[(int)adType] = state;
                }
                else {
                    placement.m_enabledState = state;
                }
            }


            public virtual bool IsReady(AdType adType, PlacementData placement = null) { return false; }

            public void AddEvent(AdType adType, AdEvent adEvent, PlacementData placement = null) {
                EventParam eventParam = new EventParam();
                eventParam.m_adType = adType;
                eventParam.m_placementData = placement;
                eventParam.m_adEvent = adEvent;
                m_events.Add(eventParam);
            }

            public bool IsTimeout(AdType adType) {
                TimeoutParams failedInfo = GetTimeoutParams(adType);
                bool state = !failedInfo.IsTimeout;
                return state;
            }

            public void NotifyEvent(AdType adType, AdEvent adEvent, PlacementData placement = null) {
                string placementName = placement != null ? placement.PlacementName : _PLACEMENT_DEFAULT_NAME;

                if (adEvent == AdEvent.PrepareFailure || adEvent == AdEvent.Prepared) {
                    if (placement != null) {
                        placement.m_lastAdPrepared = adEvent == AdEvent.Prepared;
                    }
                    else {
                        m_arrLastAdPreparedState[(int)adType] = adEvent == AdEvent.Prepared;
                    }
                }

                OnEvent(this, adType, adEvent, placementName);
            }

            public bool GetLastAdPreparedStatus(AdType adType, PlacementData placement = null) {
                if (placement == null) {
                    return m_arrLastAdPreparedState[(int)adType];
                }
                else {
                    return placement.m_lastAdPrepared;
                }
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
                    if (m_timeoutParameters[i].m_adType == adType) {
                        m_timeoutParameters[i].FailedLoadTime = Time.realtimeSinceStartup;
                        break;
                    }
                }
            }

            public IPlacementParameters GetPlacementParams(AdType adType, string placement) {
                IPlacementParameters foundParams = null;
                foreach (IPlacementParameters itemParameters in m_placementParameters) {
                    if (itemParameters.PlacementAdType == adType && itemParameters.PlacementName == placement) {
                        foundParams = itemParameters;
                    }
                }
                return foundParams;
            }

            /// <summary>
            /// For editor mode
            /// </summary>
            public static string CreateAdPlacementsDirectory(string specificPath) {
                string fullPath = Application.dataPath + "/" + _PLACEMENT_PARAMETERS_ROOT_PATH + specificPath;
                if (!Directory.Exists(fullPath)) {
                    Directory.CreateDirectory(fullPath);
                }
                return fullPath;
            }

            public void AddPlacement(PlacementData placement) {
                m_placementDataList.Add(placement);
            }

            public PlacementData GetPlacementData(AdType adType, string placement) {
                PlacementData foundData = null;

                if (m_placementDataList.Count > 0) {
                    foreach(PlacementData data in m_placementDataList) {
                        if (data.m_adType == adType && data.PlacementName == placement) {
                            foundData = data;
                            break;
                        }
                    }
                }
                return foundData;
            }

            #endregion Public Methods

            //_______________________________________________________________________________
            #region Internal Methods
            //-------------------------------------------------------------------------------

            void UpdateEvents() {
                if (m_events.Count > 0) {
                    EventParam[] eventParams = m_events.ToArray();
                    m_events.Clear();
                    foreach (EventParam eventParam in eventParams) {
                        NotifyEvent(eventParam.m_adType, eventParam.m_adEvent, eventParam.m_placementData);
                    }
                }
            }

            protected virtual void InitializePlacementData(PlacementData placement, JSONValue jsonPlacementData) {
                placement.PlacementName = jsonPlacementData.Obj.GetString("name");
                placement.m_adType = AdTypeConvert.StringToAdType(jsonPlacementData.Obj.GetString("adType"));
                placement.m_adID = jsonPlacementData.Obj.GetString("id");
                placement.m_placementParams = GetPlacementParams(placement.m_adType, placement.PlacementName);
                m_placementDataList.Add(placement);
            }

            protected virtual void InitializeParameters(Dictionary<string, string> parameters, JSONArray jsonPlacements) {

                InitializePlacementParameters();

                if (jsonPlacements != null) {
                    foreach (JSONValue jsonPlacementData in jsonPlacements) {
                        PlacementData placementData = CreatePlacementData(jsonPlacementData);
                        InitializePlacementData(placementData, jsonPlacementData);
                    }
                }

                m_timeoutParameters = new TimeoutParams[m_adSupportParams.Length];
                int count = Enum.GetNames(typeof(AdType)).Length;
                m_arrLastAdPreparedState = new bool[count];
                m_arrAdState = new AdState[count];
                m_arrEnableState = new bool[count];

                if (this.enabled) {
                    for (int i = 0; i < count; i++) {
                        m_arrEnableState[i] = true;
                    }
                }

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

            /// <summary>
            /// Should implementation in inheritors (Fabric method)
            /// </summary>
            protected virtual PlacementData CreatePlacementData(JSONValue jsonPlacementData) {
                return null;
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

            void InitializePlacementParameters() {
                if (PlacementParametersPath.Length > 0) {
                    string path = PlacementParametersPath.Remove(0, 10);
                    UnityEngine.Object[] parameters = Resources.LoadAll(path);

                    foreach(UnityEngine.Object itemParameters in parameters) {
                        IPlacementParameters placementParameters = itemParameters as IPlacementParameters;
                        if (placementParameters != null) {
                            m_placementParameters.Add(placementParameters);
                        }
                    }
                }
            }

            #endregion Internal Methods

        }

    } // namespace AdMediation
} // namespace Virterix
