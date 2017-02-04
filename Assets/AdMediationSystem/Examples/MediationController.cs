using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Virterix.AdMediation;

public class MediationController : MonoBehaviour {

    public Text m_interstitialInfoText;
    public Text m_rewardVideoInfoText;
    public Text m_bannerInfoText;
    public Text m_eventLogText;
    public Text m_adInterstitialCountText;

    int m_adInterstitialCount;

	// Use this for initialization
	void Awake () {
        AdMediationSystem.OnInitializeComplete += OnMediationSystemInitializeComplete;
        AdMediationSystem.OnAdNetworkEvent += OnAdNetworkEvent;
	}
		
    void OnMediationSystemInitializeComplete() {
        AdMobAdapter adMobNetwork = AdMediationSystem.Instance.GetNetwork("admob") as AdMobAdapter;
        adMobNetwork.BannerPosition = GoogleMobileAds.Api.AdPosition.Bottom;
        adMobNetwork.BannerSize = GoogleMobileAds.Api.AdSize.SmartBanner;

        AdMediationSystem.Fetch(AdType.Interstitial);
        AdMediationSystem.Fetch(AdType.Incentivized);
        AdMediationSystem.Fetch(AdType.Banner);
    }

    public void FetchInterstitial() {
        AdMediationSystem.Fetch(AdType.Interstitial);
        UpdateAdInfo(AdType.Interstitial);
    }

    public void FetchRewardVideo() {
        AdMediationSystem.Fetch(AdType.Incentivized);
        UpdateAdInfo(AdType.Incentivized);
    }

    public void FetchBanner() {
        AdMediationSystem.Fetch(AdType.Banner);
        UpdateAdInfo(AdType.Banner);
    }

    public void ShowInterstitial() {
        AdMediationSystem.Show(AdType.Interstitial);
    }

    public void ShowRewardVideo() {
        AdMediationSystem.Show(AdType.Incentivized);
    }

    public void ShowBanner() {
        AdMediationSystem.Show(AdType.Banner);
    }

    public void HideBanner() {
        AdMediationSystem.Hide(AdType.Banner);
    }

    void OnAdNetworkEvent(AdNetworkAdapter network, AdType adType, AdNetworkAdapter.AdEvent adEvent) {
        UpdateAdInfo(adType);
        m_eventLogText.text = adType.ToString() + " " + network.m_networkName + " " + adEvent.ToString() + "\n" + m_eventLogText.text;

        if (adEvent == AdNetworkAdapter.AdEvent.Show && 
            (adType == AdType.Interstitial || adType == AdType.Incentivized)) {
            m_adInterstitialCount++;
        }
        m_adInterstitialCountText.text = m_adInterstitialCount.ToString();
    }

    void UpdateAdInfo(AdType adType) {
        Text guiText = null;
        switch(adType) {
            case AdType.Interstitial:
                guiText = m_interstitialInfoText;
                break;
            case AdType.Incentivized:
                guiText = m_rewardVideoInfoText;
                break;
            case AdType.Banner:
                guiText = m_bannerInfoText;
                break;
        }
        
        AdMediator mediator = AdMediationSystem.Instance.GetMediator(adType);
        string networkName = "";
        bool isReady = false;
        if (mediator != null) {
            networkName = mediator.CurrentNetworkName;
            isReady = mediator.IsCurrentNetworkReadyToShow;
        }

        if (guiText != null) {
            guiText.text = "network:" + networkName + "\n" + "ready:" + isReady.ToString();
        }
    }

}
