using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InternetChecker : MonoBehaviour {

    public float m_waitTime = 2.5f;
    public string[] m_urls;
    public bool m_startOnAwake = false;
    public bool m_destoryWhenFinishCheck = false;


    void Awake() {
        DontDestroyOnLoad(this.gameObject);
        if (m_startOnAwake) {
            StartCheck();
        }
    }

    IEnumerator CheckConnectionToMasterServer(Action<bool> callback) {

        bool successfulConnection = false;

        for (int i = 0; i < m_urls.Length; i++) {
            UnityWebRequest www = UnityWebRequest.Get(m_urls[i]);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError) {
                continue;
            } else {
                successfulConnection = true;
                //byte[] results = www.downloadHandler.data;
                break;
            }
        }

        if (callback != null) {
            callback(successfulConnection);
        }

        if (m_destoryWhenFinishCheck) {
            Destroy(this.gameObject);
        }

        yield return null;
    }

    public static InternetChecker Create(float waitTime= 2.5f) {
        InternetChecker component = new GameObject("InternetChecker").AddComponent<InternetChecker>();
        component.m_waitTime = waitTime;
        component.m_urls = new string[] { "www.google.com" };
        return component;
    }

    public void StartCheck(Action<bool> callback = null) {
        StartCoroutine(CheckConnectionToMasterServer(callback));
    }

}
