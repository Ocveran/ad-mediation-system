
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Boomlagoon.JSON;
using Virterix.Common;

namespace Virterix {
    namespace AdMediation {

        public class AdMediationSystem : Singleton<AdMediationSystem> {

            public enum AppPlatform {
                Android,
                iOS
            }

            enum SetupSettingsState {
                Successful,
				RequiredCheckUpdate,
				Failure
            }

            struct NetworkParams {
                public Dictionary<string, string> m_parameters;
                public JSONArray m_placements;
            }

            //===============================================================================
            #region Configuration variables
            //-------------------------------------------------------------------------------

            public const string _API_VERSION = "1.24";
            const string _CHECK_UPDATE_DATA_SAVE_KEY = "adm_check_update_date";
            const string _HASH_SAVE_KEY = "adm_settings_hash";
			const float _LOAD_SETTINGS_WAITING_TIME = 30.0f;
            const string _SETTINGS_VERSION_PARAM_KEY = "adm_settings_version";

            #endregion // Configuration variables

            //===============================================================================
            #region Variables
            //-------------------------------------------------------------------------------

            public string m_apiUrl = "http://api.virterix.com/";
            public string m_projectName;
            
            public bool m_isLoadOnlyDefaultSettings = true;
            [Tooltip("Compare settings only loaded from server")]
            public bool m_isCompareSettingsByHash = true;
            public AppPlatform m_defaultPlatformName;
            public string m_hashCryptKey;
            [SerializeField]
            private bool m_isInitializeOnStart = true;

            public static event Action OnInitializeComplete = delegate { };
			public static event Action<AdNetworkAdapter, AdType, AdEvent, string> OnAdNetworkEvent = delegate { };

            Hashtable m_userParameters = new Hashtable();
			SavedDate m_сheckUpdateData;
            AdNetworkAdapter[] m_networkAdapters;
            List<AdMediator> m_mediators = new List<AdMediator>();
            JSONObject m_currSettings;

            public JSONObject CurrSettings {
                get { return m_currSettings; }
            }

            public string PlatfomName {
                get {
                    string platformName = m_defaultPlatformName.ToString();

                    switch (Application.platform) {
                        case RuntimePlatform.Android:
                            platformName = AppPlatform.Android.ToString();
                            break;
                        case RuntimePlatform.IPhonePlayer:
                            platformName = AppPlatform.iOS.ToString();
                            break;
                    }
                    return platformName;
                }
            }

            public InternetChecker InternetChecker {
                get {
                    if (m_internetChecker == null) {
                        m_internetChecker = InternetChecker.Create();
                    }
                    return m_internetChecker;
                }
            }
            InternetChecker m_internetChecker;

            string SettingsFileName {
                get { return PlatfomName + "_settings"; }
            }

            string DefaultSettingsFilePathInResources {
                get {
                    string settingsFilePath = "AdSettings/" + m_projectName + "/" + SettingsFileName;
                    return settingsFilePath;
                }
            }

			// Path to settings file
            string SettingsFilePath {
                get {
                    string settingsFilePath = Application.persistentDataPath + "/" + SettingsFileName + ".json";
                    return settingsFilePath;
                }
            }

			// Returns settings version
            int CurrSettingsVersion {
                get {
                    int settingsVersion = -1;
                    if (m_currSettings != null) {
                        if (m_currSettings.ContainsKey(_SETTINGS_VERSION_PARAM_KEY)) {
                            settingsVersion = Convert.ToInt32(m_currSettings.GetValue(_SETTINGS_VERSION_PARAM_KEY).Number);
                        }
                    }
                    return settingsVersion;
                }
            }


            #endregion // Variables

            //===============================================================================
            #region MonoBehaviour methods
            //-------------------------------------------------------------------------------

            void Awake() {
                DontDestroyOnLoad(this.gameObject);
            }

            void Start() {
                if (m_isInitializeOnStart) {
                    Initialize();
                }
            }

            #endregion MonoBehaviour methods

            //===============================================================================
            #region Get configure parameters
            //-------------------------------------------------------------------------------

            public bool GetUserParam<T>(string key, ref T value) {
                if (m_userParameters.ContainsKey(key)) {
                    value = (T)m_userParameters[key];
                    return true;
                }
                return false;
            }

            public bool GetUserIntParam(string key, ref int value) {
                if (m_userParameters.ContainsKey(key)) {
                    try {
                        double val = (double)m_userParameters[key];
                        value = Convert.ToInt32(val);
                        return true;
                    } 
                    catch {
                        return false;
                    }
                }
                return false;
            }

            public bool GetUserBooleanParam(string key, ref bool value) {
                if (m_userParameters.ContainsKey(key)) {
                    try {
                        value = (bool)m_userParameters[key];
                        return true;
                    }
                    catch {
                        return false;
                    }
                }
                return false;
            }

            public bool GetUserDoubleParam(string key, ref double value) {
                if (m_userParameters.ContainsKey(key)) {
                    try {
                        value = (double)m_userParameters[key];
                        return true;
                    }
                    catch {
                        return false;
                    }
                }
                return false;
            }

            public string GetUserParam(string key) {
                string result = "";
                if (m_userParameters.ContainsKey(key)) {
                    result = m_userParameters[key].ToString();
                }
                return result;
            }

            #endregion // Get configure parameters

            //===============================================================================
            #region Mediation ad networks
            //-------------------------------------------------------------------------------

            public AdNetworkAdapter GetNetwork(string networkName) {
                AdNetworkAdapter foundNetwork = null;
                foreach (AdNetworkAdapter networkAdapter in m_networkAdapters) {
                    if (networkAdapter.m_networkName.Equals(networkName)) {
                        foundNetwork = networkAdapter;
                        break;
                    }
                }
                return foundNetwork;
            }

            public AdMediator GetMediator(AdType adType, string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
                AdMediator foundMediator = null;
                foreach (AdMediator mediator in m_mediators) {
                    if (mediator.m_adType == adType && mediator.m_placementName == placementName) {
                        foundMediator = mediator;
                        break;
                    }
                }
                return foundMediator;
            }

            public static void Fetch(AdType adType, string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME, Hashtable parameters = null) {
                AdMediator mediator = Instance.GetMediator(adType, placementName);
                if (mediator != null) {
                    mediator.Fetch();
                } else {
                    Debug.Log("AdMediationSystem.Fetch() Not found mediator: " + adType.ToString());
                }
            }

            public static void Show(AdType adType, string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME, Hashtable parameters = null) {
                AdMediator mediator = Instance.GetMediator(adType, placementName);
                if (mediator != null) {
                    mediator.Show();
                } else {
                    Debug.Log("AdMediationSystem.Fetch() Not found mediator: " + adType.ToString());
                }
            }

            public static void Hide(AdType adType, string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
                AdMediator mediator = Instance.GetMediator(adType, placementName);
                if (mediator != null) {
                    mediator.Hide(placementName);
                } else {
                    Debug.Log("AdMediationSystem.Hide() Not found mediator " + adType.ToString());
                }
            }

            public static void NotifyAdNetworkEvent(AdNetworkAdapter network, AdType adType, AdEvent adEvent, 
                string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {

                OnAdNetworkEvent(network, adType, adEvent, placement);
            }

            #endregion // Mediation ad networks

            //===============================================================================
            #region Other internal methods
            //-------------------------------------------------------------------------------

            string GetCustomizationRequestUrl(string methodName) {
                string requestUrl = m_apiUrl + "customization." + methodName + "?" +
                    "platform=" + PlatfomName +
                    "&project=" + m_projectName +
                    "&v=" + _API_VERSION;
                return requestUrl;
            }

            void CalculateAndSaveSettingsHash(string settings) {
                string hash = AdUtils.GetHash(settings);
                string encodedHash = CryptString.Encode(hash, m_hashCryptKey);
				PlayerPrefs.SetString(_HASH_SAVE_KEY, encodedHash);
            }

            void SaveSettingsHash(string settingsHash) {
                string encodedHash = CryptString.Encode(settingsHash, m_hashCryptKey);
				PlayerPrefs.SetString(_HASH_SAVE_KEY, encodedHash);
            }

            bool IsSettingsHashValid(string settings) {
				string encodedHash = PlayerPrefs.GetString(_HASH_SAVE_KEY, "");
                string savedHash = CryptString.Decode(encodedHash, m_hashCryptKey);
                string currHash = AdUtils.GetHash(settings);
                bool isValid = currHash == savedHash;
                return isValid;
            }
            
            string JsonValueToString(JSONValue jsonValue) {
                string valueStr = "";
                if (jsonValue.Type == JSONValueType.String) {
                    valueStr = jsonValue.Str;
                }
                else {
                    valueStr = jsonValue.ToString();
                }
                return valueStr;
            }

            AdMediator GetOrCreateMediator(AdType adType, string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
                AdMediator foundMediator = null;
                foreach (AdMediator mediator in m_mediators) {
                    if (mediator.m_adType == adType && mediator.m_placementName == placementName) {
                        foundMediator = mediator;
                        break;
                    }
                }

                if (foundMediator == null) {
                    AdMediator createdMediator = this.gameObject.AddComponent<AdMediator>();
                    createdMediator.m_adType = adType;
                    createdMediator.m_placementName = placementName;
                    m_mediators.Add(createdMediator);
                    foundMediator = createdMediator;
                }
                return foundMediator;
            }

            void NotifyInitializeComplete() {
                OnInitializeComplete();
            }

            bool IsRequiredCheckUpdateSettingsFile(Boomlagoon.JSON.JSONObject jsonSettings) {
                bool isRequiredCheckUpdate = false;
				m_сheckUpdateData.m_period = (float)jsonSettings.GetNumber("checkUpdatePeriod");
                isRequiredCheckUpdate = m_сheckUpdateData.IsPeriodOver || !m_сheckUpdateData.WasSaved;

#if AD_MEDIATION_DEBUG_MODE
				Debug.Log("AdMediationSystem.IsRequiredCheckUpdateSettingsFile() Elapsed hours:" + m_сheckUpdateData.PassedHoursSinceLastSave + 
					" requiredHours:" + m_сheckUpdateData.m_period);
#endif

                return isRequiredCheckUpdate;
            }

            void SaveCheckUpdateDateTimeSettings() {
				m_сheckUpdateData.Save();
            }

            #endregion // Other internal methods

            //===============================================================================
            #region Initialize
            //-------------------------------------------------------------------------------

            public void Initialize() {					 
				m_сheckUpdateData = new SavedDate(_CHECK_UPDATE_DATA_SAVE_KEY, 72, SavedDate.PeriodType.Hours);
                m_networkAdapters = GetComponentsInChildren<AdNetworkAdapter>(true);   
                AdMediator[] mediators = GetComponentsInChildren<AdMediator>();
                m_mediators.AddRange(mediators);
                StartInitializeSettings();
            }

            void StartInitializeSettings() {
                bool isLoaded = LoadJsonSettingsFromFile(ref m_currSettings, m_isLoadOnlyDefaultSettings);
                bool isLoadingFromServer = false;

				if (isLoaded && !m_isLoadOnlyDefaultSettings) {
					if (IsRequiredCheckUpdateSettingsFile(m_currSettings)) {
                        isLoadingFromServer = true;
                        StartLoadSettingsFromServer();
                    } 
                }

                if (!isLoadingFromServer) {
                    SetupCurrentSettings();
                }
            }

			/// <summary>
            /// Setup settings from json object
            /// </summary>
            bool SetupSettings(JSONObject jsonSettings) {

                bool setupSettingsSuccess = false;

                string userParametersKey = "userParameters";
                string mediatorsKey = "mediators";
                string adTypeKey = "adType";
                string mediatorPlacementNameKey = "placement";
                string networkPlacementsNameKey = "placements";
                string strategyKey = "strategy";
                string defaultWaitingResponseTimeKey = "defaultWaitingResponseTime";
                string typeInStrategyKey = "type";
                string networkUnitsKey = "units";
                string networkNameInUnitKey = "networkName";
                string unitEnabledKey = "enabled";
                string internalAdTypeKey = "internalAdType";
                string waitingResponseTimeKey = "waitingResponseTime";

                string networksKey = "networks";
                string networkNameKey = "name";
                string networkTimeoutsKey = "timeouts";

                Dictionary<AdNetworkAdapter, NetworkParams> dictNetworks = new Dictionary<AdNetworkAdapter, NetworkParams>();
                Dictionary<AdMediator, List<AdUnit>> dictMediators = new Dictionary<AdMediator, List<AdUnit>>();

                try {
                    if (jsonSettings.ContainsKey(userParametersKey)) {
                        JSONArray userParametersJsonArray = jsonSettings.GetArray(userParametersKey);
                        foreach (JSONValue jsonParams in userParametersJsonArray) {
                            string key = jsonParams.Obj["key"].Str;
                            object paramValue = null;
                            JSONValue jsonValue = jsonParams.Obj["value"];
                            switch (jsonValue.Type) {
                                case JSONValueType.Boolean:
                                    paramValue = jsonValue.Boolean;
                                    break;
                                case JSONValueType.Number:
                                    paramValue = jsonValue.Number;
                                    break;
                                case JSONValueType.String:
                                    paramValue = jsonValue.Str;
                                    break;
                            }
                            m_userParameters[key] = paramValue;
                        }
                    }

                    // Initializing networks

                    JSONArray jsonArrNetwork = jsonSettings.GetArray(networksKey);
                    
                    foreach (JSONValue jsonValNetworkParams in jsonArrNetwork) {
                        string networkName = jsonValNetworkParams.Obj.GetValue(networkNameKey).Str;
                        AdNetworkAdapter networkAdapter = GetNetwork(networkName);

                        if (networkAdapter != null) {
                            if (jsonValNetworkParams.Obj.ContainsKey("enabled")) {
                                if (!jsonValNetworkParams.Obj.GetBoolean("enabled")) {
                                    networkAdapter.enabled = false;
                                    continue;
                                }
                            }
                            Dictionary<string, string> dictNetworkParams = new Dictionary<string, string>();

                            // Parse parameters
                            foreach (KeyValuePair<string, JSONValue> pairValue in jsonValNetworkParams.Obj) {
                                if (pairValue.Key == networkTimeoutsKey) {
                                    ParseNetworkTimeoutParameters(pairValue.Value.Array, ref dictNetworkParams, "timeout-");
                                }
                                else {
                                    dictNetworkParams.Add(pairValue.Key, JsonValueToString(pairValue.Value));
                                }
                            }

                            NetworkParams networkParams = new NetworkParams();
                            networkParams.m_parameters = dictNetworkParams;

                            if (jsonValNetworkParams.Obj.ContainsKey(networkPlacementsNameKey)) {
                                networkParams.m_placements = jsonValNetworkParams.Obj.GetArray(networkPlacementsNameKey);
                            }

                            dictNetworks.Add(networkAdapter, networkParams);
                        }
                        else {
                            Debug.LogWarning("AdMediationSystem.SetupNetworkParameters() Initializing networks. Not found Ad network adapter with name: " + networkName);
                        }
                    }

                    // Initialized mediators

                    JSONArray jsonArrMediators = jsonSettings.GetArray(mediatorsKey);
                    Dictionary<string, string> dictUnitParams = new Dictionary<string, string>();

                    foreach (JSONValue jsonMediationParams in jsonArrMediators) {
                        string adTypeName = jsonMediationParams.Obj.GetValue(adTypeKey).Str;
                        JSONObject jsonStrategy = jsonMediationParams.Obj.GetValue(strategyKey).Obj;
                        string defaultWaitingResponseTime = System.Convert.ToInt32(jsonMediationParams.Obj.GetNumber(defaultWaitingResponseTimeKey)).ToString();
                        string strategyTypeName = jsonStrategy.GetValue(typeInStrategyKey).Str;
                        JSONArray jsonArrUnits = jsonStrategy.GetArray(networkUnitsKey);
                        AdType adType = AdTypeConvert.StringToAdType(adTypeName);
                        string placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME;
                        if (jsonMediationParams.Obj.ContainsKey(mediatorPlacementNameKey)) {
                            placementName = jsonMediationParams.Obj.GetValue(mediatorPlacementNameKey).Str;
                        }
                        AdMediator mediator = GetOrCreateMediator(adType, placementName);
                        List<AdUnit> units = new List<AdUnit>();

                        mediator.FetchStrategy = AdFactory.CreateFetchStrategy(strategyTypeName);

                        // Pass by ad units of mediator strategy
                        foreach (JSONValue jsonNetworkUnits in jsonArrUnits) {
                            string networkName = jsonNetworkUnits.Obj.GetValue(networkNameInUnitKey).Str;
                            AdNetworkAdapter networkAdapter = GetNetwork(networkName);
                            AdType internalAdType = adType;

                            if (networkAdapter == null || !networkAdapter.enabled) {
                                continue;
                            }

                            // Check unit enabled
                            bool unitEnabled = true;
                            if (jsonNetworkUnits.Obj.ContainsKey(unitEnabledKey)) {
                                unitEnabled = jsonNetworkUnits.Obj.GetBoolean(unitEnabledKey);
                            }

                            // Internal ad type
                            string internalAdTypeName = "";
                            if (jsonNetworkUnits.Obj.ContainsKey(internalAdTypeKey)) {
                                internalAdTypeName = jsonNetworkUnits.Obj.GetString(internalAdTypeKey);
                                AdType convertedAdType = AdTypeConvert.StringToAdType(internalAdTypeName);
                                internalAdType = convertedAdType != AdType.Unknown ? convertedAdType : internalAdType;
                            }

                            // If the network enabled and support this type of advertising then add it to list 
                            if (networkAdapter != null) {
                                // Parse ad unit parameters
                                foreach (KeyValuePair<string, JSONValue> pairValue in jsonNetworkUnits.Obj) {
                                    dictUnitParams.Add(pairValue.Key, JsonValueToString(pairValue.Value));
                                }
                                dictUnitParams["index"] = units.Count.ToString();
                                if (!dictUnitParams.ContainsKey(waitingResponseTimeKey)) {
                                    dictUnitParams.Add(waitingResponseTimeKey, defaultWaitingResponseTime);
                                }

                                // Create strategy parameters
                                IFetchStrategyParams fetchStrategyParams = AdFactory.CreateFetchStrategyParams(strategyTypeName, internalAdType, dictUnitParams);
                                if (fetchStrategyParams == null) {
                                    Debug.LogWarning("AdMediationSystem.SetupNetworkParameters() Not found fetch strategy parameters");
                                }

                                // Create ad unit
                                AdUnit unit = new AdUnit(placementName, internalAdType, networkAdapter, fetchStrategyParams, unitEnabled);
                                units.Add(unit);
                            }
                            else {
                                Debug.LogWarning("AdMediationSystem.SetupNetworkParameters() Not found network adapter: " + networkName);
                            }
                            dictUnitParams.Clear();
                        }

                        dictMediators.Add(mediator, units);
                    }

                    setupSettingsSuccess = true;
                }
                catch(Exception e) {
                    Debug.LogWarning("AdMediationSystem.SetupSettings() Parse settings failed! Catch exception when setup settings. Message: " + e.Message + " __StackTrace__: " + e.StackTrace);
                }

                if (setupSettingsSuccess) {
                    // Initialization networks
                    foreach (KeyValuePair<AdNetworkAdapter, NetworkParams> pair in dictNetworks) {
                        AdNetworkAdapter netwrok = pair.Key;
                        Dictionary<string, string> networkParameters = (Dictionary<string, string>)pair.Value.m_parameters;
                        netwrok.Initialize(networkParameters, pair.Value.m_placements);
                    }

                    // Initialization mediators
                    foreach (KeyValuePair<AdMediator, List<AdUnit>> pair in dictMediators) {
                        AdMediator mediator = pair.Key;
                        mediator.Initialize(pair.Value.ToArray());
                    }
                } else {
                    m_userParameters = new Hashtable();
                    foreach (AdMediator mediator in m_mediators) {
                        mediator.FetchStrategy = new EmptyFetchStrategy();
                    }
                }

                return setupSettingsSuccess;
            }

            void ParseNetworkTimeoutParameters(JSONArray array, ref Dictionary<string, string> dictParams, string keyPrefix) {
                foreach (JSONValue jsonParams in array) {
                    string key = keyPrefix + jsonParams.Obj["adType"].Str;
                    string timeoutParam = jsonParams.Obj["time"].ToString();
                    dictParams.Add(key, timeoutParam);
                }
            }

            void SetupCurrentSettings() {
                if (m_currSettings != null) {
                    bool setupSuccess = SetupSettings(m_currSettings);
                    if (!setupSuccess) {
                        DeleteSavedJsonSettings();
                        bool isLoadedDefaultSettings = LoadJsonSettingsFromFile(ref m_currSettings, true);
                        if (isLoadedDefaultSettings) {
                            SetupSettings(m_currSettings);
                        }
                    }
                }
                NotifyInitializeComplete();
            }

            void DeleteSavedJsonSettings() {
                if (File.Exists(SettingsFilePath)) {
                    File.Delete(SettingsFilePath);
                }
            }

            #endregion // Initialize

            //===============================================================================
            #region Load
            //-------------------------------------------------------------------------------

            bool LoadJsonSettingsFromFile(ref JSONObject resultSettings, bool ignoreLoadedSettings = false) {

                JSONObject settings = null;
                bool isLoadSuccessfully = false;

                if (!ignoreLoadedSettings && File.Exists(SettingsFilePath)) {
                    string jsonString = File.ReadAllText(SettingsFilePath);

                    if (IsSettingsHashValid(jsonString)) {
                        settings = JSONObject.Parse(jsonString);
                        isLoadSuccessfully = settings != null;
                    }	  
                    
                    if(!isLoadSuccessfully) {
                        File.Delete(SettingsFilePath);
                    }

#if AD_MEDIATION_DEBUG_MODE
					Debug.Log("AdMediationSystem.LoadJsonSettingsFromFile() " + (isLoadSuccessfully ? " Valid settings" : " Not valid settings"));
#endif
                }

                if (!isLoadSuccessfully) {
                    TextAsset textAsset = Resources.Load<TextAsset>(DefaultSettingsFilePathInResources);
                    if (textAsset != null) {
                        string jsonString = textAsset.text;
                        settings = JSONObject.Parse(jsonString);

#if AD_MEDIATION_DEBUG_MODE
                        Debug.Log("AdMediationSystem.LoadJsonSettingsFromFile() Loaded default settings file");
#endif
                    }
                }

                resultSettings = settings;
                isLoadSuccessfully = resultSettings != null;

                return isLoadSuccessfully;
            }
            
            void StartLoadSettingsFromServer() {
				string requestUrl = GetCustomizationRequestUrl("get");

#if AD_MEDIATION_DEBUG_MODE
				Debug.Log("[AdMediationSystem.StartLoadSettingsFromServer] Request url:" + requestUrl);
#endif

                RemoteLoader.Load(requestUrl,
                    _LOAD_SETTINGS_WAITING_TIME,
                    RemoteLoader.CheckMode.EveryFrame,
                    RemoteLoader.DestroyMode.DestroyObject,
                    OnLoadingCompleteSettingsFromServer);
            }

			void OnLoadingCompleteSettingsFromServer(bool success, UnityWebRequest www) {
          
                if (success) {
					string receivedContent = www.downloadHandler.text.Trim();
                    JSONObject remoteJsonSettings = JSONObject.Parse(receivedContent);

                    if (remoteJsonSettings != null) {
						SaveCheckUpdateDateTimeSettings();
                        bool isModifiedRemoteSettings = false;
                        string currSettingsStr = CurrSettings != null ? CurrSettings.ToString() : "";
                            
                        string remoteHash = "";
						string localHash = "";
						int localVersion = CurrSettingsVersion; 
						int remoteVersion = -1;

						if (m_isCompareSettingsByHash) {
                            localHash = AdUtils.GetHash(currSettingsStr);
                            remoteHash = AdUtils.GetHash(remoteJsonSettings.ToString());
                            isModifiedRemoteSettings = localHash != remoteHash;
                        }
                        else {
                            if (remoteJsonSettings.ContainsKey(_SETTINGS_VERSION_PARAM_KEY)) {
                                remoteVersion = Convert.ToInt32(remoteJsonSettings.GetValue(_SETTINGS_VERSION_PARAM_KEY).Number);
                            }
                            isModifiedRemoteSettings = remoteVersion > localVersion;                      
                        }

#if AD_MEDIATION_DEBUG_MODE
						if(m_isCompareSettingsByHash)
							Debug.Log("[AdMediationSystem.OnLoadingCompleteSettingsFromServer] Compare by hash. Is identically:" + (localHash == remoteHash));
						else 
							Debug.Log("[AdMediationSystem.OnLoadingCompleteSettingsFromServer] Compare by version local:" + localVersion + " remote:" + remoteVersion);
#endif

                        if (isModifiedRemoteSettings) {
                            if(remoteHash.Length == 0) {
                                remoteHash = AdUtils.GetHash(remoteJsonSettings.ToString());
                            }
                            SaveSettingsHash(remoteHash);

#if AD_MEDIATION_DEBUG_MODE
                            Debug.Log("[AdMediationSystem.OnLoadingCompleteSettingsFromServer] Save file: " + SettingsFilePath);
#endif
                            File.WriteAllText(this.SettingsFilePath, remoteJsonSettings.ToString());                              
                            m_currSettings = remoteJsonSettings;
                        }
                    }

#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("[AdMediationSystem.OnLoadingCompleteSettingsFromServer] Loading remote settings done. Success:" + (remoteJsonSettings != null).ToString());
#endif
                }
                else {
                    Debug.Log("[AdMediationSystem.OnLoadingCompleteSettingsFromServer] Not response from ads server. Error: " + www.error);
                }

                SetupCurrentSettings();
            }

            #endregion // Load

        }

    } // namespace AdMediation
} // namespace Virterix
