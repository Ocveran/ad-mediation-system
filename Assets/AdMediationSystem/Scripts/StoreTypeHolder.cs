using UnityEngine;
using System.Collections;
using Virterix.Common;

namespace Virterix {

    public enum StoreType {
        Unknown = 0,
        GooglePlay,
        iOSStore,
        Samsung,
        Amazon,
        WindowsStore
    }

    public class StoreTypeHolder : Singleton<StoreTypeHolder> {

        public StoreType StoreType {
            get { return m_storeType; }
        }

        public bool m_autoDetection;

        public string StoreName {
            get { return m_storeType.ToString(); }
        }

        [SerializeField]
        StoreType m_storeType;

        void Awake() {
            DontDestroyOnLoad(this.gameObject);
            if (m_autoDetection) {
                switch (Application.platform) {
                    case RuntimePlatform.Android:
                        m_storeType = StoreType.GooglePlay;
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        m_storeType = StoreType.iOSStore;
                        break;
                }
            }
        }
		
		public void SetStoreType(StoreType type) {
            m_storeType = type;
        }
    }

}

