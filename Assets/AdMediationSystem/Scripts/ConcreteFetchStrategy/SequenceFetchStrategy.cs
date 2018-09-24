using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Virterix {
    namespace AdMediation {

        public class SequenceFetchStrategy : IFetchStrategy {

            public class SequenceStrategyParams : IFetchStrategyParams {
                public int m_index;
                public int m_impressions;
                public int m_skipFetchIndex;
                public AdNetworkAdapter m_replacebleNetwork;
            }
            
            int m_currFetchCount;
            int m_currUnitIndex;
            AdUnit m_currUnit;
            int m_skipCount;
            int m_maxSkipCount;

            public bool IsAllowAutoFillUnits() {
                return false;
            }

            public AdUnit Fetch(AdMediator mediator, AdUnit[] units) {
                m_skipCount = 0;
                m_maxSkipCount = 6;
                AdUnit unit = MoveToNextUnit(mediator, units);
                return unit;
            }

            /// <summary>
            /// Reset to start state
            /// </summary>
            /// <param name="mediator">Mediator for find index in fetch units array</param>
            /// <param name="unit">Set current unit</param>
            public void Reset(AdMediator mediator, AdUnit unit) {
                m_currUnit = unit;
                if (mediator != null) {
                    m_currUnitIndex = mediator.FindIndexInFetchUnits(m_currUnit);
                }
                else {
                    m_currUnitIndex = 0;
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
                    return null;
                }

                int nextUnitIndex = m_currUnitIndex;
                bool isNeedReset = false;

                if (m_currUnit == null) {
                    nextUnitIndex = 0;
                } else {
                    // If current unit not contained in fetch array then next unit index doesn't increment.
                    if (m_currUnit.IsContainedInFetch) {
                        if (IsMoveNextUnit(m_currUnit)) {
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
                    if (units.Length == 0) {
                        return null;
                    }

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
                    Reset(mediator, m_currUnit);
                }

                string networkName = "";
                if (m_currUnit != null) {
                    networkName = m_currUnit.AdNetwork.m_networkName;
                }

                bool isSkipUnit = IsSkipUnit(m_currUnit);

                if (!isSkipUnit && m_currUnit != null) {
                    SequenceStrategyParams sequenceStrategyParams = m_currUnit.FetchStrategyParams as SequenceStrategyParams;
                    if (sequenceStrategyParams.m_replacebleNetwork != null) {
                        AdNetworkAdapter.PlacementData placementData = m_currUnit != null ? m_currUnit.PlacementData : null;

                        if (sequenceStrategyParams.m_replacebleNetwork.GetEnabledState(m_currUnit.AdapterAdType, placementData)) {
                            isSkipUnit = sequenceStrategyParams.m_replacebleNetwork.GetLastAdPreparedStatus(m_currUnit.AdapterAdType, 
                                m_currUnit.PlacementData);
                        }
                        else {
                            isSkipUnit = false;
                        }
                    }
                }

                if (isSkipUnit && m_skipCount < m_maxSkipCount) {
                    m_skipCount++;
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

                if (networkParams.ContainsKey("replaceableNetwork")) {
                    sequenceStrategyParams.m_replacebleNetwork = AdMediationSystem.Instance.GetNetwork(networkParams["replaceableNetwork"]);
                }
 
            }
        }

    } // namespace AdMediation
} // namespace Virterix
