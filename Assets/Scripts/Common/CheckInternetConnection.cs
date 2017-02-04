using UnityEngine;
using System;
using System.Collections;

namespace Virterix {
    namespace Common {

        public class CheckInternetConnection : MonoBehaviour {

            public Action<bool> Callback { set; get; }
            public float m_waitingTime = 5.0f;
            public string m_ip = "87.245.198.45";

            const string mc_defaultHostIp = "87.245.198.45";

            bool m_checking;
            string m_ipToCall;

            public void StartCheck() {
                if (!m_checking) {
                    m_checking = true;
                    StartCoroutine(CheckConnectionToMasterServer());
                }
            }

            IEnumerator CheckConnectionToMasterServer() {
                Ping pingMasterServer = new Ping(m_ip);
                float passsedTime = 0f;

                while (!pingMasterServer.isDone && passsedTime < m_waitingTime) {
                    yield return new WaitForEndOfFrame();
                    passsedTime += Time.unscaledDeltaTime;
                }

                if (Callback != null) {
                    Callback(pingMasterServer.isDone);
                }

                m_checking = false;
                Destroy(this.gameObject);
            }

            public static CheckInternetConnection Create(float waitingTime, string hostIp, Action<bool> callback) {
                CheckInternetConnection checker = new GameObject("CheckInternetConnection").AddComponent<CheckInternetConnection>();
                checker.m_ipToCall = hostIp;
                checker.m_waitingTime = waitingTime;
                checker.Callback = callback;
                checker.StartCheck();
                return checker;
            }

            public static CheckInternetConnection Create(float waitingTime, Action<bool> callback) {
                CheckInternetConnection checker = Create(waitingTime, mc_defaultHostIp, callback);
                return checker;
            }

        }

    } // namespace Common
} // namespace Virterix