using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Virterix {
    namespace AdMediation {

        public class AudienceNetworkBannerPlacementParameters : ScriptableObject, IPlacementParameters {

            public const string _PARAMETERS_FILE_NAME = "AN_BannerPlacementParameters";
            
            public string m_placementName = AdNetworkAdapter._PLACEMENT_DEFAULT_NAME;
            public AudienceNetworkAdapter.AudienceNetworkBannerSize m_bannerSize;
            public AudienceNetworkAdapter.AudienceNetworkBannerPosition m_bannerPosition;

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
            [MenuItem("Tools/Ad Mediation/Audience Network/Create Banner Placement Parameters")]
            public static void CreateParameters() {
                string fullPath = AdNetworkAdapter.CreateAdPlacementsDirectory(AudienceNetworkAdapter._PLACEMENT_PARAMETERS_FOLDER);
                string searchPattern = "*" + AdNetworkAdapter._PLACEMENT_PARAMETERS_FILE_EXTENSION;
                string[] files = Directory.GetFiles(fullPath, searchPattern, SearchOption.TopDirectoryOnly); 
                string path = "Assets/" + AdNetworkAdapter._PLACEMENT_PARAMETERS_ROOT_PATH + 
                    AudienceNetworkAdapter._PLACEMENT_PARAMETERS_FOLDER + "/" + _PARAMETERS_FILE_NAME;
                path += (files.Length == 0 ? "" : " " + (files.Length + 1)) + AdNetworkAdapter._PLACEMENT_PARAMETERS_FILE_EXTENSION;

                AudienceNetworkBannerPlacementParameters asset = ScriptableObject.CreateInstance<AudienceNetworkBannerPlacementParameters>();
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

