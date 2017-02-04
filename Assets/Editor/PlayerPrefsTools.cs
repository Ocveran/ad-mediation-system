using UnityEditor;
using UnityEngine;

public class PlayerPrefsTools : MonoBehaviour {

	[MenuItem ("Tools/Player Prefs/Delete All")]
	static void DeleteAll () {
		PlayerPrefs.DeleteAll ();
	}
}