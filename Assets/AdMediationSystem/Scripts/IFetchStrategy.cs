
namespace Virterix {
    namespace AdMediation {

        public class IFetchStrategyParams {
            public IFetchStrategyParams() {
                m_waitingResponseTime = 60f;
            }
            public AdType m_adsType;
            public float m_waitingResponseTime;
            public int m_impressionsInSession;
        }

        public interface IFetchStrategy {
            /// <summary>
            /// Makes fetch from the array of units
            /// </summary>
            AdUnit Fetch(AdMediator mediator, AdUnit[] units);
            /// <summary>
            /// Resets to a start state
            /// </summary>
            /// <param name="unit">New current ad unit</param>
            void Reset(AdMediator mediator, AdUnit unit);
            bool IsAllowAutoFillUnits();
        }

    } // namespace AdMediation
} // namespace Virterix