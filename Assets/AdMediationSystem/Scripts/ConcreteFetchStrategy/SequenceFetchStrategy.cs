using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Virterix {
    namespace AdMediation {

        public class SequenceFetchStrategy : IFetchStrategy {

            public class SequenceStrategyParams : IFetchStrategyParams {
                public int m_index;
                public int m_impressions;
                public int m_skipFetchIndex;           
            }
            
            int m_currFetchCount;
            int m_currUnitIndex;
            AdUnit m_currUnit;
            
            public bool IsAllowAutoFillUnits() {
                return false;
            }

            public AdUnit Fetch(AdMediator mediator, AdUnit[] units) {
                AdUnit unit = MoveToNextUnit(mediator, units);
                return unit;
            }

            public void Reset(AdMediator mediator, AdUnit unit) {
                m_currUnit = unit;
                if (mediator != null) {
                    m_currUnitIndex = mediator.FindIndexInFetchUnits(m_currUnit);
                }
                m_currFetchCount = 1;
            }

            bool IsSkipUnit(AdUnit unit) {
                bool isSkip = false;
                SequenceStrategyParams sequenceParams = unit.FetchStrategyParams as SequenceStrategyParams;

                if (sequenceParams.m_skipFetchIndex != 0) {
                    isSkip = unit.FetchCount % sequenceParams.m_skipFetchIndex == 0;
                }
                if (!isSkip && sequenceParams.m_impressionsInSession != 0) {
                    isSkip = unit.Impressions >= sequenceParams.m_impressionsInSession;
                }

                return isSkip;
            }

            bool IsMoveNextUnit(AdUnit unit) {
                SequenceStrategyParams sequenceParams = GetStrategyParams(unit);
                bool isMoveNext = m_currFetchCount >= sequenceParams.m_impressions;
                return isMoveNext;
            }

            AdUnit MoveToNextUnit(AdMediator mediator, AdUnit[] units) {

                if (units.Length == 0) {
                    mediator.FillFetchUnits(true);
                    return null;
                }

                int nextUnitIndex = m_currUnitIndex;
                bool isNeedReset = false;

                if (m_currUnit == null) {
                    nextUnitIndex = 0;
                } else {
                    if (m_currUnit.IsContainedInFetch) {
                        if(IsMoveNextUnit(m_currUnit)) {
                            nextUnitIndex++;
                            isNeedReset = true;
                        }
                    } else {
                        isNeedReset = true;
                    }
                }

                if (nextUnitIndex >= units.Length) {
                    nextUnitIndex = 0;
                    mediator.FillFetchUnits(true);
                    units = mediator.FetchUnits.ToArray();
                    if (!isNeedReset) {
                        nextUnitIndex = FindIndex(m_currUnit, units);
                        nextUnitIndex = nextUnitIndex == -1 ? 0 : nextUnitIndex;
                    }
                }

                m_currUnitIndex = nextUnitIndex;
                m_currUnit = units[m_currUnitIndex];
                m_currUnit.IncrementFetchCount();
                m_currFetchCount++;

                if (isNeedReset) {
                    Reset(null, m_currUnit);
                }

                string networkName = "";
                if (m_currUnit != null) {
                    networkName = m_currUnit.AdNetwork.m_networkName;
                }

                if (IsSkipUnit(m_currUnit)) {
                    m_currUnit = MoveToNextUnit(mediator, units);
                }
                  
                return m_currUnit;
            }

            SequenceStrategyParams GetStrategyParams(AdUnit unit) {
                SequenceStrategyParams sequenceParams = unit.FetchStrategyParams as SequenceStrategyParams;
                return sequenceParams;
            }

            int FindIndex(AdUnit unit, AdUnit[] units) {
                int index = -1;
                for(int i = 0; i < units.Length; i++) {
                    if (units[i] == unit) {
                        index = i;
                        break;
                    }
                }
                return index;
            }

            public static void SetupParameters(ref IFetchStrategyParams strategyParams, Dictionary<string, string> networkParams) {
                SequenceStrategyParams sequenceStrategyParams = strategyParams as SequenceStrategyParams;
                sequenceStrategyParams.m_index = System.Convert.ToInt32(networkParams["index"]);

                int impressions = 1;
                if (networkParams.ContainsKey("impressions")) {
                    impressions = System.Convert.ToInt32(networkParams["impressions"]);
                }
                sequenceStrategyParams.m_impressions = impressions;

                if (networkParams.ContainsKey("skipFetchIndex")) {
                    sequenceStrategyParams.m_skipFetchIndex = System.Convert.ToInt32(networkParams["skipFetchIndex"]);
                }
            }
        }

    } // namespace AdMediation
} // namespace Virterix
