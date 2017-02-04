using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Virterix {
    namespace AdMediation {

        public enum AdType {
            None = 0,
            Banner = 1,
            Interstitial = 2,
            Video = 3,
            Incentivized = 4
        }

        public static class AdTypeConvert {

            public static AdType StringToAdType(string adTypeName) {

                AdType adType = AdType.None;
                switch (adTypeName) {
                    case "banner":
                        adType = AdType.Banner;
                        break;
                    case "interstitial":
                        adType = AdType.Interstitial;
                        break;
                    case "video":
                        adType = AdType.Video;
                        break;
                    case "incentivized":
                        adType = AdType.Incentivized;
                        break;
                }

                return adType;
            }

            public static string AdTypeToString(AdType adType) {

                string adTypeName = "";
                switch (adType) {
                    case AdType.Banner:
                        adTypeName = "banner";
                        break;
                    case AdType.Interstitial:
                        adTypeName = "interstitial";
                        break;
                    case AdType.Video:
                        adTypeName = "video";
                        break;
                    case AdType.Incentivized:
                        adTypeName = "incentivized";
                        break;
                }

                return adTypeName;
            }
        }

    } // namespace AdMediation
} // namespace Virterix