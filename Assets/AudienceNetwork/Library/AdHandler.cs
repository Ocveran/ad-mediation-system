using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace AudienceNetwork
{
    public class AdHandler : MonoBehaviour
    {
        private readonly static Queue<Action> executeOnMainThreadQueue = new Queue<Action>();

        public void executeOnMainThread (Action action)
        {
            executeOnMainThreadQueue.Enqueue(action);
        }

        public void Update () {
            // dispatch stuff on main thread
            while (executeOnMainThreadQueue.Count > 0)
            {
                executeOnMainThreadQueue.Dequeue().Invoke();
            }
        }

        public void removeFromParent () {
            #if UNITY_EDITOR
//          UnityEngine.Object.DestroyImmediate (this);
            #else
            UnityEngine.Object.Destroy (this);
            #endif
        }
    }

    public delegate void FBNativeAdHandlerValidationCallback(bool success);

    [RequireComponent (typeof (RectTransform))]
    public class NativeAdHandler : AdHandler
    {
        public int minViewabilityPercentage;
        public float minAlpha;
        public int maxRotation;
        public int checkViewabilityInterval;
        new public Camera camera;

        public FBNativeAdHandlerValidationCallback validationCallback;

        private float lastImpressionCheckTime;
        private bool impressionLogged;
        private bool shouldCheckImpression;

        public void startImpressionValidation ()
        {
            if (!this.enabled) {
                this.enabled = true;
            }
            this.shouldCheckImpression = true;
        }

        public void stopImpressionValidation ()
        {
            this.shouldCheckImpression = false;
        }

        public void Update ()
        {
            base.Update ();
            this.checkImpression ();
        }

        private bool checkImpression ()
        {
            float currentTime = Time.time;
            float secondsSinceLastCheck = currentTime - this.lastImpressionCheckTime;

            if (this.shouldCheckImpression && !this.impressionLogged && (secondsSinceLastCheck > checkViewabilityInterval)) {
                this.lastImpressionCheckTime = currentTime;

                GameObject currentObject = this.gameObject;
                Camera camera = this.camera;
                if (camera == null) {
                    camera = this.GetComponent<Camera>();
                }
                if (camera == null) {
                    camera = Camera.main;
                }

                while (currentObject != null) {
                    bool currentObjectViewable = this.checkGameObjectViewability (camera, currentObject);
                    if (!currentObjectViewable) {
                        if (this.validationCallback != null) {
                            this.validationCallback(false);
                        }
                        return false;
                    }
                    Transform transform = currentObject.transform;
                    Transform parentTransform = (transform == null) ? null : transform.parent;
                    currentObject = (parentTransform == null) ? null : parentTransform.gameObject;
                };
                if (this.validationCallback != null) {
                    this.validationCallback(true);
                }
                this.impressionLogged = true;
            }
            return this.impressionLogged;
        }

        private bool logViewability (bool success, string message)
        {
            if (!success) {
                Debug.Log ("Viewability validation failed: " + message);
            } else {
                Debug.Log ("Viewability validation success! " + message);
            }
            return success;
        }

        private bool checkGameObjectViewability (Camera camera, GameObject gameObject)
        {
            if (gameObject == null) {
                return this.logViewability (false, "GameObject is null.");
            }

            if (camera == null) {
                return this.logViewability (false, "Camera is null.");
            }

            if (!gameObject.activeInHierarchy) {
                return this.logViewability (false, "GameObject is not active in hierarchy.");
            }

            RectTransform transform = gameObject.transform as RectTransform;
            Vector3 position = transform.position;

            Vector2 rect = transform.sizeDelta;
            float width = rect.x;
            float height = rect.y;
            Vector3 positionOnScreen = this.calculateWorldPosition (position, camera); //camera.WorldToScreenPoint (position);

            // position is in center of object, adjust
            positionOnScreen.x = positionOnScreen.x - (width / 2.0f);
            positionOnScreen.y = positionOnScreen.y - (0 / 2.0f);
            Rect screenSize = camera.pixelRect;

            if (width <= 0 && height <= 0) {
                return this.logViewability (false, "GameObject's height/width is less than or equal to zero.");
            }

            CanvasGroup[] groups = gameObject.GetComponents<CanvasGroup> ();
            foreach (CanvasGroup group in groups) {
                if (group.alpha < this.minAlpha) {
                    return this.logViewability (false, "GameObject has a CanvasGroup with less than the minimum alpha required.");
                }
            }

            if ((positionOnScreen.x < 0) || (positionOnScreen.x > (screenSize.width - width))) {
                return this.logViewability (false, "GameObject is not on screen. (x axis)");
            }
            if (positionOnScreen.y < 0 || positionOnScreen.y > (screenSize.height - 0)) {
                return this.logViewability (false, "GameObject is not on screen. (y axis)");
            }

            int verticalInvisibleThreshold = (int)(height * (100 - this.minViewabilityPercentage) / 100);

            if (-positionOnScreen.y > verticalInvisibleThreshold) {
                return this.logViewability (false, "GameObject is too far offscreen.");
            }

            if ((positionOnScreen.y + height) - (screenSize.height) > verticalInvisibleThreshold) {
                return this.logViewability (false, "GameObject is too far offscreen.");
            }

            // Check rotation
            Vector3 rotation = transform.eulerAngles;
            int xRotation = Mathf.FloorToInt (rotation.x);
            int yRotation = Mathf.FloorToInt (rotation.y);
            int zRotation = Mathf.FloorToInt (rotation.z);

            int minRotation = 360 - this.maxRotation;
            int maxRotation = this.maxRotation;

            if (!(xRotation >= minRotation || xRotation <= maxRotation)) {
                return this.logViewability (false, "GameObject is rotated too much. (x axis)");
            } else if (!(yRotation >= minRotation || yRotation <= maxRotation)) {
                return this.logViewability (false, "GameObject is rotated too much. (y axis)");
            } else if (!(zRotation >= minRotation || zRotation <= maxRotation)) {
                return this.logViewability (false, "GameObject is rotated too much. (z axis)");
            }

            return this.logViewability (true, "--------------- VALID IMPRESSION REGISTERED! ----------------------");
        }

        private Vector3 calculateWorldPosition(Vector3 position, Camera camera) {
            Vector3 cameraNormal = camera.transform.forward;
            Vector3 vectorFromCamera = position - camera.transform.position;
            float cameraNormalDot = Vector3.Dot (cameraNormal, vectorFromCamera.normalized);
            if (cameraNormalDot <= 0f) {
                float camDot = Vector3.Dot (cameraNormal, vectorFromCamera);
                Vector3 proj = (cameraNormal * camDot * 1.01f);
                position = camera.transform.position + (vectorFromCamera - proj);
            }

            return position;
        }
    }
}
