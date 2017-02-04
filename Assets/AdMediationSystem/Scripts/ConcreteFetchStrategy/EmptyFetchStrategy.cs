using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Virterix {
    namespace AdMediation {

        public class EmptyFetchStrategy : IFetchStrategy {
       
            AdUnit IFetchStrategy.Fetch(AdMediator mediator, AdUnit[] units) {
                return null;
            }

            bool IFetchStrategy.IsAllowAutoFillUnits() {
                return false;
            }

            void IFetchStrategy.Reset(AdMediator mediator, AdUnit unit) {  
            }
        }
    } // namespace AdMediation
} // namespace Virterix
