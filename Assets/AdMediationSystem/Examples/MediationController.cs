using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Virterix.AdMediation;

public class MediationController : MonoBehaviour {

    public Text m_interstitialInfoText;
    public Text m_rewardVideoInfoText;
    public Text m_nativeInfoText;
    public Text m_bannerInfoText;
    public Text m_bannerTopInfoText;
    public Text m_eventLogText;
    public Text m_adInterstitialCountText;
    public AudienceNetworkNativeAdPanel m_nativeAdPanel;

    int m_adInterstitialCount;

	// Use this for initialization
	void Awake () {
        AdMediationSystem.OnInitializeComplete += OnMediationSystemInitializeComplete;
        AdMediationSystem.OnAdNetworkEvent += OnAdNetworkEvent;
	}
		
    void OnMediationSystemInitializeComplete() {
        AudienceNetworkAdapter audienceNetwork = AdMediationSystem.Instance.GetNetwork("audienceNetwork") as AudienceNetworkAdapter;
        audienceNetwork.SetNativePanel(m_nativeAdPanel);

        AdMediationSystem.Fetch(AdType.Interstitial);
        AdMediationSystem.Fetch(AdType.Incentivized);
        AdMediationSystem.Fetch(AdType.Banner);
        AdMediationSystem.Fetch(AdType.Banner, "Top");
        AdMediationSystem.Fetch(AdType.Native);
    }

    public void FetchInterstitial() {
        AdMediationSystem.Fetch(AdType.Interstitial);
        UpdateAdInfo(AdType.Interstitial, AdNetworkAdapter._PLACEMENT_DEFAULT_NAME);
    }

    public void FetchRewardVideo() {
        AdMediationSystem.Fetch(AdType.Incentivized);
        UpdateAdInfo(AdType.Incentivized, AdNetworkAdapter._PLACEMENT_DEFAULT_NAME);
    }

    public void FetchNative(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Fetch(AdType.Native, placement);
        UpdateAdInfo(AdType.Native, placement);
    }

    public void FetchBanner(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Fetch(AdType.Banner, placement);
        UpdateAdInfo(AdType.Banner, placement);
    }

    public void ShowInterstitial() {
        AdMediationSystem.Show(AdType.Interstitial);
    }

    public void ShowRewardVideo() {
        AdMediationSystem.Show(AdType.Incentivized);
    }

    public void ShowBanner(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Show(AdType.Banner, placement);
    }

    public void HideBanner(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Hide(AdType.Banner, placement);
    }

    public void ShowNative(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Show(AdType.Native, placement);
    }

    public void HideNative(string placement = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
        AdMediationSystem.Hide(AdType.Native, placement);
    }

    void OnAdNetworkEvent(AdNetworkAdapter network, AdType adType, AdEvent adEvent, string placement) {
        UpdateAdInfo(adType, placement);
        m_eventLogText.text = adType.ToString() + " placement:" + placement + " " + network.m_networkName + " " + adEvent.ToString() + "\n" + m_eventLogText.text;

        if (adEvent == AdEvent.Show && 
            (adType == AdType.Interstitial || adType == AdType.Incentivized)) {
            m_adInterstitialCount++;
        }
        m_adInterstitialCountText.text = m_adInterstitialCount.ToString();
    }

    void UpdateAdInfo(AdType adType, string placement) {

        Text guiText = null;
        switch(adType) {
            case AdType.Interstitial:
                guiText = m_interstitialInfoText;
                break;
            case AdType.Incentivized:
                guiText = m_rewardVideoInfoText;
                break;
            case AdType.Banner:
                if (placement == AdNetworkAdapter._PLACEMENT_DEFAULT_NAME) {
                    guiText = m_bannerInfoText;
                }
                else {
                    guiText = m_bannerTopInfoText;
                }
                break;
            case AdType.Native:
                guiText = m_nativeInfoText;
                break;
        }
        
        AdMediator mediator = AdMediationSystem.Instance.GetMediator(adType, placement);
        string networkName = "";
        bool isReady = false;
        if (mediator != null) {
            networkName = mediator.CurrentNetworkName;
            isReady = mediator.IsCurrentNetworkReadyToShow;
        }

        if (guiText != null) {
            guiText.text = "network: " + networkName + "\n" + 
                "ready curr network: " + isReady.ToString() + "\n" +
                "ready mediator: " + mediator.IsReadyToShow.ToString();
        }
    }

}
