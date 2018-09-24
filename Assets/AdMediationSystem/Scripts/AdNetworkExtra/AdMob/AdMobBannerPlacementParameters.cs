using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Virterix {
    namespace AdMediation {

        public class AdMobBannerPlacementParameters : ScriptableObject, IPlacementParameters {

            public const string _PARAMETERS_FILE_NAME = "AdMob_BannerPlacementParameters";

            public string m_placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME;
            public AdMobAdapter.AdMobBannerSize m_bannerSize;
            public AdMobAdapter.AdMobBannerPosition m_bannerPosition;

            public string PlacementName {
                get {
                    return m_placementName;
                }
            }

            public AdType PlacementAdType {
                get {
                    return AdType.Banner;
                }
            }

#if UNITY_EDITOR
            [MenuItem("Tools/Ad Mediation/AdMob/Create Banner Placement Parameters")]
            public static void CreateParameters() {
                string fullPath = AdNetworkAdapter.CreateAdPlacementsDirectory(AdMobAdapter._PLACEMENT_PARAMETERS_FOLDER);
                string searchPattern = "*" + AdNetworkAdapter._PLACEMENT_PARAMETERS_FILE_EXTENSION;
                string[] files = Directory.GetFiles(fullPath, searchPattern, SearchOption.TopDirectoryOnly);
                string path = "Assets/" + AdNetworkAdapter._PLACEMENT_PARAMETERS_ROOT_PATH +
                    AdMobAdapter._PLACEMENT_PARAMETERS_FOLDER + "/" + _PARAMETERS_FILE_NAME;
                path += (files.Length == 0 ? "" : " " + (files.Length + 1)) + AdNetworkAdapter._PLACEMENT_PARAMETERS_FILE_EXTENSION;

                AdMobBannerPlacementParameters asset = ScriptableObject.CreateInstance<AdMobBannerPlacementParameters>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;

                AssetDatabase.Refresh();
            }
#endif
        }

    } // namespace AdMediation
} // namespace Virterix
