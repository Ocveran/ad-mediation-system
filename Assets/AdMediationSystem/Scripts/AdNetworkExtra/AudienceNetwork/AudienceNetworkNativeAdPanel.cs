
#define _MS_AUDIENCE_NETWORK

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if _MS_AUDIENCE_NETWORK
using AudienceNetwork;
#endif

namespace Virterix {
    namespace AdMediation {

        public class AudienceNetworkNativeAdPanel : MonoBehaviour {

            public GameObject m_root;

            // UI elements in scene
            [Header("Text:")]
            public Text m_title;
            public Text m_socialContext;
 
            [Header("Images:")]
            public Image m_coverImage;
            public Image m_iconImage;

            [Header("Buttons:")]
            public Text m_callToAction;
            public Button m_callToActionButton;

            [Header("Ad Choices:")]
            [SerializeField]
            private AdChoices m_adChoices;

            public Button[] CallToActionButtons {
                get {
                    return new Button[] { m_callToActionButton };
                }
            }

            CanvasGroup CanvasGroup {
                get {
                    if (m_canvasGroup == null) {
                        m_canvasGroup = GetComponent<CanvasGroup>();
                    }
                    return m_canvasGroup;
                }
            }
            CanvasGroup m_canvasGroup;

            Coroutine m_procVisibleAnimation;
            bool m_isVisible;
            bool m_isPanelVisible;
            bool m_isAnimationInstant;

#if _MS_AUDIENCE_NETWORK
            NativeAd m_nativeAd;

            public void SetNativeAd(NativeAd nativeAd) {
                m_nativeAd = nativeAd;
                if (m_nativeAd == null) {
                    AnimateHide();
                }
                else {
                    RefreshTexts();
                    m_nativeAd.RegisterGameObjectForImpression(this.gameObject, CallToActionButtons);
                }
            }
#endif

            public void RefreshTexts() {
                if (m_nativeAd != null) {
                    m_title.text = m_nativeAd.Title;
                    m_socialContext.text = m_nativeAd.SocialContext;
                    m_callToAction.text = m_nativeAd.CallToAction;
                }
            }

            void OnGUI() {
#if _MS_AUDIENCE_NETWORK
                // Update GUI from native ad
                if (m_nativeAd != null && m_nativeAd.CoverImage != null) {
                    m_coverImage.sprite = m_nativeAd.CoverImage;
                }
                if (m_nativeAd != null && m_nativeAd.IconImage != null) {
                    m_iconImage.sprite = m_nativeAd.IconImage;
                }
                if (m_nativeAd != null && m_nativeAd.AdChoicesImage != null) {
                    m_adChoices.SetNativeAd(m_nativeAd);
                }
#endif
            }

            private void OnEnable() {
                if (m_isVisible) {
                    Show(false);
                }
                StartCoroutine(ProcCheckLoading());
            }

            private void OnDisable() {
                if (!m_isVisible) {
                    Hide(true);
                }
            }

            void AnimateShow(bool instant = false) {
                if (m_root != null) {
                    m_root.SetActive(true);
                }

                if (instant) {
                    CanvasGroup.alpha = 1.0f;
                }
                else {
                    if (m_procVisibleAnimation != null) {
                        StopCoroutine(m_procVisibleAnimation);
                    }
                    m_procVisibleAnimation = StartCoroutine(ProcAnimateVisible(true));
                }
            }

            void AnimateHide(bool instant = false) {
                if (instant) {
                    CanvasGroup.alpha = 0.0f;
                    if (m_root != null) {
                        m_root.SetActive(false);
                    }
                }
                else {
                    if (this.gameObject.activeInHierarchy && this.gameObject.activeSelf) {
                        if (m_procVisibleAnimation != null) {
                            StopCoroutine(m_procVisibleAnimation);
                        }
                        m_procVisibleAnimation = StartCoroutine(ProcAnimateVisible(false));
                    }
                    else {
                        Hide(true);
                    }
                }
            }

            public void Show(bool instant = false) {
                if (m_root != null) {
                    m_root.SetActive(true);
                }
                m_isVisible = true;
                m_isAnimationInstant = instant;
            }

            public void Hide(bool instant = false) {
                m_isVisible = false;
                m_isPanelVisible = false;
                AnimateHide(instant);
            }

            IEnumerator ProcCheckLoading() {
                while (true) {
                    yield return new WaitForSecondsRealtime(0.1f);
                    
                    if (m_nativeAd != null) {
                        if (!m_isPanelVisible && m_isVisible) {
                            if (m_nativeAd.CoverImage != null && m_nativeAd.IconImage != null && m_nativeAd.AdChoicesImage != null) {
                                m_isPanelVisible = true;
                                AnimateShow(m_isAnimationInstant);
                            }
                        }
                    }
                    else {
                        if (m_isPanelVisible) {
                            m_isPanelVisible = false;
                            AnimateHide();
                        }
                    }
                }
            }

            IEnumerator ProcAnimateVisible(bool visible) {

                bool animating = true;

                while(animating) {
                    yield return new WaitForEndOfFrame();
                    float alpha = CanvasGroup.alpha;
                    if (visible) {
                        alpha = alpha + 5.0f * Time.unscaledDeltaTime;
                        if (alpha >= 1.0f) {
                            alpha = 1.0f;
                            animating = false;
                        }

                    }
                    else {
                        alpha = alpha - 5.0f * Time.unscaledDeltaTime;
                        if (alpha <= 0.0f) {
                            alpha = 0.0f;
                            animating = false;
                            if (m_root != null) {
                                m_root.SetActive(false);
                            }
                        }
                    }
                    CanvasGroup.alpha = alpha;
                }

                yield break;
            }
        }

    } // namespace AdMediation
} // namespace Virterix
