
#define _MS_IRONSOURCE

#if _MS_IRONSOURCE

using UnityEngine;
using System.Collections;

namespace Virterix {
	namespace AdMediation {
		
		public class SupersonicAdapter : AdNetworkAdapter {

			public string m_defaultAndroidAppKey = "";
			public string m_defaultIOSAppKey = "";
            public bool m_isValidateIntegrationCall = false;

            void SubscribeEvents() {
				SupersonicEvents.onInterstitialInitSuccessEvent += OnInterstitialInitSuccessEvent;
				SupersonicEvents.onInterstitialInitFailedEvent += OnInterstitialInitFailEvent; 
				SupersonicEvents.onInterstitialReadyEvent += OnInterstitialReadyEvent;
				SupersonicEvents.onInterstitialLoadFailedEvent += OnInterstitialLoadFailedEvent;
				SupersonicEvents.onInterstitialShowSuccessEvent += OnInterstitialShowSuccessEvent; 
				SupersonicEvents.onInterstitialShowFailedEvent += OnInterstitialShowFailEvent; 
				SupersonicEvents.onInterstitialClickEvent += OnInterstitialAdClickedEvent;
				SupersonicEvents.onInterstitialOpenEvent += OnInterstitialAdOpenedEvent;
				SupersonicEvents.onInterstitialCloseEvent += OnInterstitialAdClosedEvent;

				SupersonicEvents.onRewardedVideoInitSuccessEvent += OnRewardedVideoInitSuccessEvent;
				SupersonicEvents.onRewardedVideoInitFailEvent += OnRewardedVideoInitFailEvent;
				SupersonicEvents.onRewardedVideoAdOpenedEvent += OnRewardedVideoAdOpenedEvent;
				SupersonicEvents.onRewardedVideoAdRewardedEvent += OnRewardedVideoAdRewardedEvent;
				SupersonicEvents.onRewardedVideoAdClosedEvent += OnRewardedVideoAdClosedEvent;
				SupersonicEvents.onVideoAvailabilityChangedEvent += OnVideoAvailabilityChangedEvent;
				SupersonicEvents.onVideoStartEvent += OnVideoStartEvent;
				SupersonicEvents.onVideoEndEvent += OnVideoEndEvent;
			}

			void UnsubscribeEvents() {
				SupersonicEvents.onInterstitialInitSuccessEvent -= OnInterstitialInitSuccessEvent;
				SupersonicEvents.onInterstitialInitFailedEvent -= OnInterstitialInitFailEvent; 
				SupersonicEvents.onInterstitialReadyEvent -= OnInterstitialReadyEvent;
				SupersonicEvents.onInterstitialLoadFailedEvent -= OnInterstitialLoadFailedEvent;
				SupersonicEvents.onInterstitialShowSuccessEvent -= OnInterstitialShowSuccessEvent; 
				SupersonicEvents.onInterstitialShowFailedEvent -= OnInterstitialShowFailEvent; 
				SupersonicEvents.onInterstitialClickEvent -= OnInterstitialAdClickedEvent;
				SupersonicEvents.onInterstitialOpenEvent -= OnInterstitialAdOpenedEvent;
				SupersonicEvents.onInterstitialCloseEvent -= OnInterstitialAdClosedEvent;

				SupersonicEvents.onRewardedVideoInitSuccessEvent -= OnRewardedVideoInitSuccessEvent;
				SupersonicEvents.onRewardedVideoInitFailEvent -= OnRewardedVideoInitFailEvent;
				SupersonicEvents.onRewardedVideoAdOpenedEvent -= OnRewardedVideoAdOpenedEvent;
				SupersonicEvents.onRewardedVideoAdRewardedEvent -= OnRewardedVideoAdRewardedEvent;
				SupersonicEvents.onRewardedVideoAdClosedEvent -= OnRewardedVideoAdClosedEvent;
				SupersonicEvents.onVideoAvailabilityChangedEvent -= OnVideoAvailabilityChangedEvent;
				SupersonicEvents.onVideoStartEvent -= OnVideoStartEvent;
				SupersonicEvents.onVideoEndEvent -= OnVideoEndEvent;
			}

			void OnEnable() {
                SubscribeEvents ();
			}

			new void OnDisable() {
                base.OnDisable();
				UnsubscribeEvents ();
			}
            
			void OnApplicationPause(bool isPaused) { 

				if (isPaused) {
					Supersonic.Agent.onPause ();
				}else{                   
					Supersonic.Agent.onResume ();
				}
			}

            protected override void InitializeParameters(System.Collections.Generic.Dictionary<string, string> parameters) {
				base.InitializeParameters(parameters);

                string isInterstitialInitKey = "isInterstitialInit";
                string isIncentivizedInitKey = "isIncentivizedInit";

                GameObject go = new GameObject ("SupersonicEvents");
				go.AddComponent<SupersonicEvents> ();
				go.transform.parent = this.transform.parent;

				string appKey = "";
				string uniqueUserId = SystemInfo.deviceUniqueIdentifier;
                bool isInterstitialInit = true;
                bool isIncentivizedInit = true;

                if (parameters != null) {
					appKey = parameters["appKey"];

                    if (parameters.ContainsKey(isInterstitialInitKey)) {
                        try {
                            isInterstitialInit = System.Convert.ToBoolean(parameters[isInterstitialInitKey]);
                        } 
                        catch {
                            isInterstitialInit = true;
                        }
                    }
                    if (parameters.ContainsKey(isIncentivizedInitKey)) {
                        try {
                            isIncentivizedInit = System.Convert.ToBoolean(parameters[isIncentivizedInitKey]);
                        }
                        catch {
                            isIncentivizedInit = true;
                        }
                    }
                } else {
#if UNITY_ANDROID
					appKey = m_defaultAndroidAppKey;
#elif UNITY_IOS
					appKey = m_defaultIOSAppKey;
#endif
				}

                Supersonic.Agent.start();
                if (isInterstitialInit) {
                    Supersonic.Agent.initInterstitial(appKey, uniqueUserId);
                }
                if (isIncentivizedInit) {
                    Supersonic.Agent.initRewardedVideo(appKey, uniqueUserId);
                }

                if (m_isValidateIntegrationCall) {
                    Supersonic.Agent.validateIntegration();
                }
            }

			public override void Prepare(AdType adType) {
				if (IsSupported(adType)) {
					if (adType == AdType.Interstitial) {
						Supersonic.Agent.loadInterstitial();
					}
					else {

                    }
				}
			}

			public override bool Show(AdType adType) {
				if (IsReady(adType)) {
					switch (adType) {
					case AdType.Interstitial:
						Supersonic.Agent.showInterstitial ();
						break;
					case AdType.Incentivized:
						Supersonic.Agent.showRewardedVideo ();
						break;
					}
					return true;
				} else {
					return false;
				}
			}

			public override void Hide(AdType adType) {
			}

			public override bool IsReady(AdType adType) {
				if (IsSupported(adType)) {
					switch (adType) {
					case AdType.Interstitial:
						return Supersonic.Agent.isInterstitialReady();
						break;
					case AdType.Incentivized:
						return Supersonic.Agent.isRewardedVideoAvailable();
						break;
					}
				}
				return false;
			}


			void OnInterstitialInitSuccessEvent() {
			}

			void OnInterstitialInitFailEvent(SupersonicError error) {
				Debug.Log ("[SupersonicAdapter.OnInterstitialInitFailEvent] code: " + error.getCode()+ ", description : " +error.getDescription());
			}

			void OnInterstitialReadyEvent() {
				AddEvent(AdType.Interstitial, AdEvent.Prepared);
			}

			void OnInterstitialLoadFailedEvent(SupersonicError error) {
				Debug.Log ("[SupersonicAdapter.OnInterstitialLoadFailedEvent] code: " + error.getCode()+ ", description : " +error.getDescription());
				AddEvent(AdType.Interstitial, AdEvent.PrepareFailure);
			}

			void OnInterstitialShowSuccessEvent() {
			}

			void OnInterstitialShowFailEvent(SupersonicError error) {
			}

			void OnInterstitialAdClickedEvent() {
				AddEvent(AdType.Interstitial, AdEvent.Click);
			}

			void OnInterstitialAdOpenedEvent() {
				AddEvent(AdType.Interstitial, AdEvent.Show);
			}

			void OnInterstitialAdClosedEvent() {
				AddEvent(AdType.Interstitial, AdEvent.Hide);
			}
				
			void OnRewardedVideoInitSuccessEvent() {
			}

			void OnRewardedVideoInitFailEvent(SupersonicError error) {
                if (error != null) {
                    Debug.Log("[SupersonicAdapter.OnRewardedVideoInitFailEvent] code: " + error.getCode() + ", description : " + error.getDescription());
                }
            }

			void OnRewardedVideoAdOpenedEvent() {
				AddEvent(AdType.Incentivized, AdEvent.Show);
			}

			void OnRewardedVideoAdRewardedEvent(SupersonicPlacement placement) {
                AddEvent(AdType.Incentivized, AdEvent.IncentivizedComplete);
			}

			void OnRewardedVideoAdClosedEvent() {
				AddEvent(AdType.Incentivized, AdEvent.Hide);
			}

			void OnVideoAvailabilityChangedEvent(bool available) {
			}

			void OnVideoStartEvent() {
			}

			void OnVideoEndEvent() {
				
			}
		}

    } // namespace AdMediation
} // namespace Virterix

#endif // _MS_IRONSOURCE