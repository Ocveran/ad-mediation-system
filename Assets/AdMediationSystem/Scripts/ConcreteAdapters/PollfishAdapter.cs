
//#define _MS_POLLFISH

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class PollfishAdapter : AdNetworkAdapter {

            public string m_apiKey;
            public bool m_debugMode = true;
            public int m_padding = 10;

#if _MS_POLLFISH

            public Position m_pollfishPosition = Position.BOTTOM_RIGHT;

            bool m_customMode;

            bool m_isSurveyReceived;
            SavedDate m_unavailableDate;

            int m_closedCount;

            void Awake() {
                m_customMode = true;
#if !UNITY_EDITOR
                Pollfish.SetEventObjectPollfish(this.gameObject.name);
#endif
            }

            void OnApplicationPause(bool pause) {
                if (pause) {
                    //m_isSurveyReceived = false;
                }
                else {
                    //InitPollfish();
                }
            }

            protected override void InitializeParameters(Dictionary<string, string> parameters) {
                base.InitializeParameters(parameters);

                float periodInHours = (float)System.Convert.ToDouble(parameters["inactivePeriodInMinutes"]);

                m_unavailableDate = new SavedDate("virterix.ms.pollfish.unvailable.date", periodInHours, SavedDate.PeriodType.Minutes);

                //Uncomment and set your own custom attribtues
                /*
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("facebook_id", "YOUR_FACEBOOK_ID");
                dict.Add("twitter_id", "YOUR_TWITTER_ID");
                Pollfish.SetAttributesPollfish(dict);*/
            }

            public override void Initialize() {
                if (!m_unavailableDate.IsOverPeriod() && m_unavailableDate.WasSaved) {
                    this.enabled = false;
                }
                InitPollfish();
            }

            public override void Prepare(AdType adType) {
            }

            public override bool Show(AdType adType) {
                if (IsReady(adType)) {
                    switch (adType) {
                        case AdType.Incentivized:
                            Pollfish.ShowPollfish();
                            break;
                    }
                    return true;
                }
                return false;
            }

            public override void Hide(AdType adType) {
                switch (adType) {
                    case AdType.Incentivized:
                        Pollfish.HidePollfish();
                        break;
                }
            }

            public override bool IsReady(AdType adType) {
                bool isReady = false;
                switch (adType) {
                    case AdType.Incentivized:
                        isReady = m_isSurveyReceived;
                        break;
                }
                return isReady;
            }

            public override void SetEnabled(bool state) {
                bool canEnabled = true;

                m_isSurveyReceived = false;
                if (m_unavailableDate.WasSaved && state) {
                    canEnabled = m_unavailableDate.IsOverPeriod();
                }

                if (canEnabled) {
                    base.SetEnabled(state);
                }
            }

            void InitPollfish() {
#if !UNITY_EDITOR
                if(this.enabled) {
                    Pollfish.PollfishInitFunction((int)m_pollfishPosition, m_padding, m_apiKey, m_debugMode, m_customMode);
                    Pollfish.HidePollfish();
                }
#endif
            }

            public void surveyCompleted(string playfulSurveyAndSurveyPrice) {
                string[] surveyCharacteristics = playfulSurveyAndSurveyPrice.Split(',');
                if (surveyCharacteristics.Length >= 2) {
                    Debug.Log("PollfishEventListener: Survey was completed - Playful Survey: " + surveyCharacteristics[0] + " with survey Price: " + surveyCharacteristics[1]);
                }

                m_isSurveyReceived = false;
                OnEvent(this, AdType.Incentivized, AdEvent.IncentivizedComplete);

                SetEnabled(false);
                m_unavailableDate.Save();
            }

            public void surveyReceived(string playfulSurveyAndSurveyPrice) {
                string[] surveyCharacteristics = playfulSurveyAndSurveyPrice.Split(',');
                if (surveyCharacteristics.Length >= 2) {
                    Debug.Log("PollfishEventListener: Survey was received - Playful Survey: " + surveyCharacteristics[0] + " with survey Price: " + surveyCharacteristics[1]);
                }
                m_isSurveyReceived = true;
                OnEvent(this, AdType.Incentivized, AdEvent.Prepared);
            }

            public void surveyOpened() {
                Debug.Log("PollfishEventListener: Survey was opened");

                OnEvent(this, AdType.Incentivized, AdEvent.Show);
                // pause scene 
            }

            public void surveyClosed() {
                Debug.Log("PollfishEventListener: Survey was closed");

                m_closedCount++;
                Pollfish.ShouldQuit();

                if (m_closedCount >= 3) {
                    SetEnabled(false);
                    m_unavailableDate.Save();
                }

                OnEvent(this, AdType.Incentivized, AdEvent.Hide);
                // resume scene 
            }

            public void surveyNotAvailable() {
                Debug.Log("PollfishEventListener: Survey not available");
                m_isSurveyReceived = false;
                OnEvent(this, AdType.Incentivized, AdEvent.PrepareFailure);
                SetEnabled(false);
            }

            public void userNotEligible() {
                Debug.Log("PollfishEventListener: User not eligible");
                m_isSurveyReceived = false;
                OnEvent(this, AdType.Incentivized, AdEvent.PrepareFailure);

                SetEnabled(false);
                m_unavailableDate.Save();
            }

#endif // _MS_POLLFISH

        }

    } // namespace AdMediation
} // namespace Virterix

