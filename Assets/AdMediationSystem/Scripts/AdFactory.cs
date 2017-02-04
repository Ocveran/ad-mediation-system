using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class AdFactory {

            public static IFetchStrategy CreateFetchStrategy(string strategyTypeName) {
                IFetchStrategy fetchStrategy = null;
                switch (strategyTypeName) {
                    case "random":
                        fetchStrategy = new RandomFetchStrategy();
                        break;
                    case "sequence":
                        fetchStrategy = new SequenceFetchStrategy();
                        break;
                }
                return fetchStrategy;
            }

            public static IFetchStrategyParams CreateFetchStrategyParams(string strategyTypeName, AdType adType, Dictionary<string, string> networkParams) {
                IFetchStrategyParams fetchStrategyParams = null;

                switch (strategyTypeName) {
                    case "random":
                        fetchStrategyParams = new RandomFetchStrategy.RandomStrategyParams();
                        try {
                            RandomFetchStrategy.SetupParameters(ref fetchStrategyParams, networkParams);
                        } catch {
                            Debug.Log("AdFactory: not found key in network parameter dictionary");
                        }
                        break;
                    case "sequence":
                        fetchStrategyParams = new SequenceFetchStrategy.SequenceStrategyParams();
                        try {
                            SequenceFetchStrategy.SetupParameters(ref fetchStrategyParams, networkParams);
                        } catch {
                            Debug.Log("AdFactory: not found key in network parameter dictionary");
                        }
                        break;
                }

                if (fetchStrategyParams != null) {
                    fetchStrategyParams.m_waitingResponseTime = (float)System.Convert.ToDouble(networkParams["waitingResponseTime"]);
                    fetchStrategyParams.m_adsType = adType;

                    if (networkParams.ContainsKey("impressionsInSession")) {
                        fetchStrategyParams.m_impressionsInSession = System.Convert.ToInt32(networkParams["impressionsInSession"]);
                    }
                }

                return fetchStrategyParams;
            }
        }

    } // namespace AdMediation
} // namespace Virterix
