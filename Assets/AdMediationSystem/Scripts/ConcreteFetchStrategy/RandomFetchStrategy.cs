using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virterix {
    namespace AdMediation {

        public class RandomFetchStrategy : IFetchStrategy {

            struct Range {
                public int min;
                public int max;
            }

            public class RandomStrategyParams : IFetchStrategyParams {
                public int m_percentage;
            }

            public bool IsAllowAutoFillUnits() {
                return true;
            }

            public AdUnit Fetch(AdMediator mediator, AdUnit[] units) {
                AdUnit unit = null;

                int unitCount = units.Length;
                if (unitCount == 1) {
                    unit = units[0];
                    return unit;
                }

                int maxRange = 0;
                Range[] unitRanges = new Range[units.Length];

                for (int i = 0; i < unitCount; i++) {
                    RandomStrategyParams parameters = units[i].FetchStrategyParams as RandomStrategyParams;

                    Range unitRange = new Range();
                    unitRange.min = maxRange;
                    unitRange.max = maxRange + parameters.m_percentage;
                    unitRanges[i] = unitRange;

                    maxRange += parameters.m_percentage;
                }

                int randomNumber = Random.Range(0, maxRange);

                for (int i = 0; i < unitCount; i++) {
                    Range unitRange = unitRanges[i];
                    if (randomNumber >= unitRange.min && randomNumber < unitRange.max) {
                        unit = units[i];
                        break;
                    }
                }

                if(unit != null) {
                    unit.IncrementFetchCount();
                }

                return unit;
            }

            public void Reset(AdMediator mediator, AdUnit unit) {

            }

            public static void SetupParameters(ref IFetchStrategyParams strategyParams, Dictionary<string, string> networkParams) {
                RandomStrategyParams randomFetchParams = strategyParams as RandomStrategyParams;
                randomFetchParams.m_percentage = System.Convert.ToInt32(networkParams["percentage"]);
            }
        }

    } // namespace AdMediation
} // namespace Virterix