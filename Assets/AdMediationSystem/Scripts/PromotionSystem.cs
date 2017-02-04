using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Virterix.AdMediation;
using Boomlagoon.JSON;
using System.IO;
using System;
using Virterix.Common;

namespace Virterix {

    public class PromotionSystem : Singleton<PromotionSystem> {

        public enum TextureOrientation {
            Landscape = 0,
            Portrait,
            Both
        }

        public TextureOrientation m_textureOrientation = TextureOrientation.Landscape;

        public static event Action OnPromotionProductAvailable;

        string m_productUrl;
        string m_productName;
        Texture2D m_landscapePromoTexture;
        Texture2D m_portraitPromoTexture;
        JSONObject m_jsonSettings;
        SavedDate m_settingsUpdateDate;
        string m_productNameCompletedPromotion;
        string m_landscapeLanguage;
        string m_portraitLanguage;

        const string _HASH_SAVE_KEY = "virterix.promo.settings.hash";
        const string _PROMO_PRODUCT_NAME_SAVE_KEY = "virterix.promo.product.name";
        const string _UPDATE_DATE_SAVE_KEY = "virterix.promo.update.date";
		const string _HASH_CRYPT_KEY = "p2n39ts";

        //===============================================================================
        #region Property
        //-------------------------------------------------------------------------------

        public Texture2D LandscapePromoTexture {
            get {
                return m_landscapePromoTexture;
            }
        }

        public Texture2D PortraitPromoTexture {
            get {
                return m_portraitPromoTexture;
            }
        }

        public string ProductUrl {
            get { return m_productUrl; }
        }

        public string ProductName {
            get { return m_productName; }
        }

        string SettingsFilePath {
            get {
                return Application.persistentDataPath + "/promotion_settings.json";
            }
        }

        string SystemLanguage {
            get {
                return Application.systemLanguage.ToString();
            }
        }

        #endregion // Property

        //===============================================================================
        #region // Methods
        //-------------------------------------------------------------------------------

        void Awake() {
            DontDestroyOnLoad(this.gameObject);
            AdMediationSystem.OnInitializeComplete += OnAdMediationInitializeComplete;
        }

        void OnAdMediationInitializeComplete() {

            if (!AdMediationSystem.Instance.CurrSettings.ContainsKey("promotionEnabled")) {
                return;
            }

            bool promotionEnabled = AdMediationSystem.Instance.CurrSettings.GetValue("promotionEnabled").Boolean;
			float promotionCheckAvailabilityPeriod = (float)AdMediationSystem.Instance.CurrSettings.GetValue("promotionCheckAvailabilityPeriod").Number;

            if(promotionEnabled) {
				m_settingsUpdateDate = new SavedDate(_UPDATE_DATE_SAVE_KEY, promotionCheckAvailabilityPeriod, SavedDate.PeriodType.Hours);
                m_productNameCompletedPromotion = PlayerPrefs.GetString(_PROMO_PRODUCT_NAME_SAVE_KEY, "");

                bool isLocalSettingsLoadedSuccessfully = LoadLocalSettings(ref m_jsonSettings);
                bool isNeedLoadRemoteSettings = !isLocalSettingsLoadedSuccessfully;

                if(isLocalSettingsLoadedSuccessfully) {
#if AD_MEDIATION_DEBUG_MODE
                    Debug.Log("[PromotionSystem.OnAdMediationInitializeComplete] Passed hours since save settings: " + m_settingsUpdateDate.PassedHoursSinceLastSave);
#endif

                    isNeedLoadRemoteSettings = m_settingsUpdateDate.IsOverPeriod();
                    if(!isNeedLoadRemoteSettings) {
                        isNeedLoadRemoteSettings = !IsValidLocalSettings(m_jsonSettings.ToString());

#if AD_MEDIATION_DEBUG_MODE
                        Debug.Log("[PromotionSystem.OnAdMediationInitializeComplete] Is valid local settins: " + !isNeedLoadRemoteSettings);
#endif
                    }
                }

                if (isNeedLoadRemoteSettings) { 
                    LoadRemoteSettings();
                } else {
                    if (isLocalSettingsLoadedSuccessfully) {
                        PreparePromotion(m_jsonSettings);
                    }
                }
            }

        }

        bool LoadLocalSettings(ref JSONObject jsonSettings) {
            bool isLoadedSuccessfully = false;

            if(File.Exists(SettingsFilePath)) {
                string content = File.ReadAllText(SettingsFilePath);
                jsonSettings = JSONObject.Parse(content);
                isLoadedSuccessfully = jsonSettings != null;
            }
            return isLoadedSuccessfully;
        }

        bool ParseSettings(string text, ref JSONObject jsonSettings) {
            jsonSettings = JSONObject.Parse(text.Trim());
            bool isParsedSuccessfully = jsonSettings != null;
            return isParsedSuccessfully;
        }

        void SaveCurrSettings() {
            string settingStr = m_jsonSettings.ToString();
            string hashSettings = CryptString.Encode(AdUtils.GetHash(settingStr), _HASH_CRYPT_KEY);
            PlayerPrefs.SetString(_HASH_SAVE_KEY, hashSettings);

            File.WriteAllText(this.SettingsFilePath, settingStr);
			m_settingsUpdateDate.Save();
        }

        bool IsValidLocalSettings(string settings) {
            bool isValid = false;
            string savedHash = PlayerPrefs.GetString(_HASH_SAVE_KEY, "");
            if (savedHash.Length > 0) {
                savedHash = CryptString.Decode(savedHash, _HASH_CRYPT_KEY);
                string hashSettings = AdUtils.GetHash(settings);
                isValid = hashSettings == savedHash;
            }
            return isValid;
        }

        void LoadRemoteSettings() {
            string requestUrl = AdMediationSystem.Instance.m_apiUrl + "promotion.get?" +
                "&project=" + AdMediationSystem.Instance.m_projectName +
                "&platform=" + AdMediationSystem.Instance.PlatfomName +
                "&v=" + AdMediationSystem._API_VERSION;

#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[PromotionSystem.LoadRemoteSettings] Request url: " + requestUrl);
#endif
            RemoteLoader.Load(requestUrl, 60f, RemoteLoader.CheckMode.EveryFrame, RemoteLoader.DestroyMode.DestroyObject, OnRemoteSettingsLoadingComplete);
        }

		void OnRemoteSettingsLoadingComplete(bool success, UnityWebRequest www) {
            if(success) {
				bool isParsedSuccessfully = ParseSettings(www.downloadHandler.text, ref m_jsonSettings);
                if(isParsedSuccessfully) {
                    SaveCurrSettings();
                    PreparePromotion(m_jsonSettings);
                }
            } else {
                Debug.Log("[PromotionSystem.OnRemoteLoadingComplete] Not loaded settings");
            }
        }

        void PreparePromotion(JSONObject jsonSettings) {
            try {
                bool enabled = jsonSettings.GetValue("enabled").Boolean;
                if(!enabled) {
                    return;
                }

                m_productUrl = jsonSettings.GetValue(StoreTypeHolder.Instance.StoreName).Str;
                m_productName = jsonSettings.GetValue("productName").Str;

                if (m_productName == m_productNameCompletedPromotion) {
                    return;
                }

                if(m_textureOrientation == TextureOrientation.Landscape || m_textureOrientation == TextureOrientation.Both) {
                    string landscapeImageUrl = GetImageUrlFromSettings("landscapeImages", m_jsonSettings, ref m_landscapeLanguage);
                    LoadImage(TextureOrientation.Landscape, landscapeImageUrl);
                }

                if (m_textureOrientation == TextureOrientation.Portrait || m_textureOrientation == TextureOrientation.Both) {
                    string portraitImageUrl = GetImageUrlFromSettings("portraitImages", m_jsonSettings, ref m_portraitLanguage);
                    LoadImage(TextureOrientation.Portrait, portraitImageUrl);
                }

            } catch {

            }
        }

        string GetImageUrlFromSettings(string key, JSONObject jsonSettings, ref string imageLanguage) {
            JSONObject jsonImages = jsonSettings.GetValue(key).Obj;
            string lang = "Default";
            string imageUrl = jsonImages["Default"].Str;
            if(jsonImages.ContainsKey(SystemLanguage)) {
                imageUrl = jsonImages[SystemLanguage].Str;
                lang = SystemLanguage;
            }
            imageLanguage = lang;
            return imageUrl;
        }

        void LoadImage(TextureOrientation textureOrientation, string imageUrl) {

            string imageFilePath = GetImageFilePath(textureOrientation);
            Texture2D texture = null;

            if (File.Exists(imageFilePath)) {
                byte[] imageBytes = File.ReadAllBytes(imageFilePath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);

#if AD_MEDIATION_DEBUG_MODE
                Debug.Log("[PromotionSystem.LoadImage] Orientation: " + textureOrientation + " Texture: " + texture);
#endif
            }

            if (texture == null) {
				Action<bool, UnityWebRequest> RemoteLoaderCallback = null;
                switch (textureOrientation) {
                    case TextureOrientation.Landscape:
                        RemoteLoaderCallback = OnLandscapeImageLoaded;
                        break;
                    case TextureOrientation.Portrait:
                        RemoteLoaderCallback = OnPortraitImageLoaded;
                        break;
                }
                RemoteLoader.Load(imageUrl, 90f, RemoteLoader.CheckMode.EveryFrame, RemoteLoader.DestroyMode.DestroyObject, RemoteLoaderCallback);
            }
            else {
                switch (textureOrientation) {
                    case TextureOrientation.Landscape:
                        m_landscapePromoTexture = texture;
                        break;
                    case TextureOrientation.Portrait:
                        m_portraitPromoTexture = texture;
                        break;
                }

                NotifyPromotionProducAvailable();
            }
        }

		void OnLandscapeImageLoaded(bool success, UnityWebRequest www) {

#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[PromotionSystem.OnLandscapeImageLoaded] Success: " + success);
#endif

            if (success) {
				m_landscapePromoTexture = new Texture2D(2, 2);
				m_landscapePromoTexture.LoadImage(www.downloadHandler.data);
                File.WriteAllBytes(GetImageFilePath(TextureOrientation.Landscape), m_landscapePromoTexture.EncodeToPNG());
                NotifyPromotionProducAvailable();
            }
        }

		void OnPortraitImageLoaded(bool success, UnityWebRequest www) {

#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("PromotionSystem.OnPortraitImageLoaded ~ success: " + success);
#endif

            if (success) {
				m_portraitPromoTexture = new Texture2D(2, 2);
				m_portraitPromoTexture.LoadImage(www.downloadHandler.data);
                File.WriteAllBytes(GetImageFilePath(TextureOrientation.Portrait), m_portraitPromoTexture.EncodeToPNG());
                NotifyPromotionProducAvailable();
            }
        }

        string GetImageFileName(TextureOrientation textureOrientation) {

            string language = "";
            if(textureOrientation == TextureOrientation.Landscape) {
                language = m_landscapeLanguage;
            } else if (textureOrientation == TextureOrientation.Portrait) { 
                language = m_portraitLanguage;
            }

            string fileName = "Promo" + textureOrientation.ToString() + ProductName + language + ".png";
            return fileName;
        }

        string GetImageFilePath(TextureOrientation textureOrientation) {
            return Application.persistentDataPath + "/" + GetImageFileName(textureOrientation);
        }

        void NotifyPromotionProducAvailable() {
            if (OnPromotionProductAvailable != null) {
                switch (m_textureOrientation) {
                    case TextureOrientation.Landscape:
                        if (m_landscapePromoTexture == null) {
                            return;
                        }
                        break;
                    case TextureOrientation.Portrait:
                        if (m_portraitPromoTexture == null) {
                            return;
                        }
                        break;
                    case TextureOrientation.Both:
                        if (m_landscapePromoTexture == null || m_portraitPromoTexture == null) {
                            return;
                        }
                        break;
                }

                OnPromotionProductAvailable();
            }
        }
        
        public void CompletePromotion() {
            PlayerPrefs.SetString(_PROMO_PRODUCT_NAME_SAVE_KEY, m_productName);
        }

 #endregion // Methods
    }

} // Virterix
