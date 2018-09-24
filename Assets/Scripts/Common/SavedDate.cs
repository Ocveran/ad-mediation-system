using UnityEngine;
using System.Collections;
using System;

namespace Virterix {
    namespace Common {

        public class SavedDate {

            public enum PeriodType {
                Seconds,
                Minutes,
                Hours,
                Days
            }

            string m_key;
            public float m_period;
            public PeriodType m_periodType;

            DateTime m_savedDate;
            bool m_wasSaved;

            public bool WasSaved {
                get { return m_wasSaved; }
            }

            public TimeSpan PassedTimeSpanSinceLastSave {
                get {
                    TimeSpan elapsedTimeSpan;
                    if (WasSaved) {
                        elapsedTimeSpan = DateTime.Now.Subtract(m_savedDate);
                    }
                    else {
                        elapsedTimeSpan = new TimeSpan();
                    }
                    return elapsedTimeSpan;
                }
            }

            public double PassedSecondsSinceLastSave {
                get { return PassedTimeSpanSinceLastSave.TotalSeconds; }
            }

            public double PassedMinutesSinceLastSave {
                get { return PassedTimeSpanSinceLastSave.TotalMinutes; }
            }

            public double PassedHoursSinceLastSave {
                get { return PassedTimeSpanSinceLastSave.TotalHours; }
            }

            public double PassedDaysSinceLastSave {
                get { return PassedTimeSpanSinceLastSave.TotalDays; }
            }

            public bool IsPeriodOver {
                get {
                    bool isOver = false;

                    if (m_wasSaved) {
                        double elapsedPeriod = 0;
                        TimeSpan elapsedTimeSpan = PassedTimeSpanSinceLastSave;

                        switch (m_periodType) {
                            case PeriodType.Seconds:
                                elapsedPeriod = elapsedTimeSpan.TotalSeconds;
                                break;
                            case PeriodType.Minutes:
                                elapsedPeriod = elapsedTimeSpan.TotalMinutes;
                                break;
                            case PeriodType.Hours:
                                elapsedPeriod = elapsedTimeSpan.TotalHours;
                                break;
                            case PeriodType.Days:
                                elapsedPeriod = elapsedTimeSpan.TotalDays;
                                break;
                        }

                        if (elapsedPeriod > m_period) {
                            isOver = true;
                        }
                    }

                    return isOver;
                }
            }


            public SavedDate(string key, float period, PeriodType periodType = PeriodType.Hours) {
                m_key = key;
                m_period = period;
                m_periodType = periodType;
                m_wasSaved = GetSavedDateTime(ref m_savedDate);
            }

            bool GetSavedDateTime(ref DateTime date) {

                bool wasSave = false; 
                string stringDateTime = PlayerPrefs.GetString(m_key, "");

                if (stringDateTime.Length != 0) {
                    wasSave = DateTime.TryParse(stringDateTime, out date);
                    if (!wasSave) {
                        date = DateTime.Now;
                    }
                }
                return wasSave;
            }

            public void Save(bool isUpdateCurrSavedData = true) {
                DateTime currDateTime = System.DateTime.Now;
                if (isUpdateCurrSavedData) {
                    m_savedDate = currDateTime;
                }
                string currDateTimeStr = currDateTime.ToString();
                PlayerPrefs.SetString(m_key, currDateTimeStr);
            }

        }

    } // namespace Common
} // namespace Virterix