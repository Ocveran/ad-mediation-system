using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdBuddizRewardedVideoManager : MonoBehaviour {

	public void Awake() {
		DontDestroyOnLoad(this);
	}

	// Fired when an incentivized video has been fetched
	public delegate void DidFetch();
	public static event DidFetch didFetch;

	// Fired when an ad can't be fetched or shown
	public delegate void DidFail(string adBuddizError);
	public static event DidFail didFail;

	// Fired when the user has fully watched an advertised video
	public delegate void DidComplete();
	public static event DidComplete didComplete;

	//Fired when the user has leaved the video before completion.
	public delegate void DidNotComplete();
	public static event DidNotComplete didNotComplete;

	public void OnDidFetch() {
		if (didFetch != null) {
			didFetch();
		}
	}

	public void OnDidFail(string adBuddizError) {
		if (didFail != null) {
			didFail(adBuddizError);
		}
    }

	public void OnDidComplete() {
		if (didComplete != null) {
			didComplete();
		}
	}

	public void OnDidNotComplete() {
		if (didNotComplete != null) {
			didNotComplete();
		}
	}
}