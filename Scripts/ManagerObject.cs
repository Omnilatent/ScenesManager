using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omnilatent.ScenesManager
{
    public class ManagerObject : MonoBehaviour
    {
        public enum State
        {
            SHIELD_OFF,
            SHIELD_ON,
            SHIELD_FADE_IN,
            SHIELD_FADE_OUT,
            SCENE_LOADING
        }

        // Shield & Transition Vars
        bool m_ShieldActive;
        State m_State;
        public State GetState() { return m_State; }

        public bool ShieldActive
        {
            get
            {
                return m_ShieldActive;
            }
            protected set
            {
                m_ShieldActive = value;
                m_Shield.gameObject.SetActive(m_ShieldActive);
            }
        }

        // Shield & Transition
        [SerializeField] Image m_Shield; //block interaction
        [SerializeField] SceneTransitionShield sceneTransitionShield;
        [SerializeField] Color m_ShieldColor = new Color(0f, 0f, 0f, 0.45f);
        public Color ShieldColor { get => m_ShieldColor; set => m_ShieldColor = value; }

        [SerializeField] GameObject m_BgCamera;
        [SerializeField] Camera m_UiCamera;

        [Tooltip("When a scene is added, increase its canvas's sorting order by this amount so it's on top of older scenes")]
        [SerializeField]
        private int _layerSortOrderAddedEachCanvas = 10;

        public int LayerSortOrderAddedEachCanvas => _layerSortOrderAddedEachCanvas;

        bool createPersistentEventSystem = true;

        [Tooltip("Only run in editor, check all cameras to see if they are rendering the same layer as BG camera each time scene load.")]
        [SerializeField]
        private bool _checkCameraRenderSameLayer = true;

        private float _cameraCheckInterval = 1f;
        private float _nextCameraCheckTime;
        private int _cameraCheckCount = 2;
        private int _cameraCheckLeft;

        public Camera UICamera
        {
            get { return m_UiCamera; }
        }

        public GameObject BgCamera
        {
            get => m_BgCamera;
            set => m_BgCamera = value;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (createPersistentEventSystem)
            {
                EventSystem prefab = Resources.Load<EventSystem>(System.IO.Path.Combine(Manager.resourcePath, "PersistentEventSystem"));
                var eventSystem = Instantiate(prefab);
                DontDestroyOnLoad(eventSystem.gameObject);
            }

            SetCameraCheck();
        }

        // Scene gradually appear
        public void FadeInScene()
        {
            if (this != null)
            {
                if (Manager.SceneFadeDuration == 0)
                {
                    // ShieldOff();
                    OnFadedIn();
                }
                else
                {
                    ShieldActive = true;
                    sceneTransitionShield.Hide();
                    m_State = State.SHIELD_FADE_IN;
                    StartCoroutine(CoFadeInScene());
                }
            }
        }

        IEnumerator CoFadeInScene()
        {
            yield return new WaitForSecondsRealtime(Manager.SceneFadeDuration);
            OnFadedIn();
        }

        // Scene gradually disappear
        public void FadeOutScene()
        {
            if (this != null)
            {
                if (Manager.SceneFadeDuration == 0)
                {
                    OnFadedOut();
                    ShieldOn();
                }
                else
                {
                    ShieldActive = true;
                    sceneTransitionShield.Show();
                    m_State = State.SHIELD_FADE_OUT;
                    StartCoroutine(CoFadeOutScene());
                }
            }
        }

        IEnumerator CoFadeOutScene()
        {
            yield return new WaitForSecondsRealtime(Manager.SceneFadeDuration);
            OnFadedOut();
        }

        public void OnFadedIn()
        {
            if (this != null)
            {
                m_State = State.SHIELD_OFF;
                ShieldActive = false;
                Manager.OnFadedIn();
            }
        }

        public void OnFadedOut()
        {
            m_State = State.SCENE_LOADING;
            Manager.OnFadedOut();
            SetCameraCheck();
        }

        public void ShieldOn()
        {
            if (m_State == State.SHIELD_OFF)
            {
                m_State = State.SHIELD_ON;
                ShieldActive = true;
            }
        }

        public void ShieldOff()
        {
            if (m_State == State.SHIELD_ON)
            {
                m_State = State.SHIELD_OFF;
                ShieldActive = false;
            }
        }

        public void ToggleBackgroundCamera(bool active)
        {
            if (m_BgCamera != null && m_BgCamera.gameObject != null)
            {
                m_BgCamera.gameObject.SetActive(active);
            }
        }

        protected void Update()
        {
            #if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE
            UpdateInput();
            #endif

            #if UNITY_EDITOR
            CheckDuplicateCamera();
            #endif
        }

        void UpdateInput()
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                #else
            if (Keyboard.current is Keyboard keyboard)
                if (keyboard.escapeKey.wasPressedThisFrame)
                #endif
            {
                if (!ShieldActive)
                {
                    Controller controller = Manager.TopController();
                    if (controller != null)
                    {
                        controller.OnKeyBack();
                    }
                }
            }
        }

        void SetCameraCheck()
        {
            if (!_checkCameraRenderSameLayer) { return; }

            _cameraCheckLeft = _cameraCheckCount;
            _nextCameraCheckTime = Time.time + _cameraCheckInterval;
        }

        private void CheckDuplicateCamera()
        {
            if (_cameraCheckLeft > 0 && Time.time > _nextCameraCheckTime)
            {
                _cameraCheckLeft--;
                _nextCameraCheckTime = Time.time + _cameraCheckInterval;
                var sceneCams = FindObjectsOfType<Camera>();
                if (sceneCams.Length == 0) { return; }

                var bgCam = BgCamera.GetComponent<Camera>();
                foreach (var sceneCam in sceneCams)
                {
                    if (!sceneCam.isActiveAndEnabled || sceneCam == bgCam) { continue; }

                    if (bgCam.isActiveAndEnabled && sceneCam.isActiveAndEnabled)
                    {
                        if (AreCamerasRenderingSameLayer(sceneCam, bgCam))
                        {
                            Debug.LogWarning(
                                $"ManagerObject's BG Camera and camera '{sceneCam.name}' are both rendering the same layer. This could affect performance.");
                            _cameraCheckLeft = 0;
                        }
                    }
                }
            }

            bool AreCamerasRenderingSameLayer(Camera cam1, Camera cam2)
            {
                return (cam1.cullingMask & cam2.cullingMask) != 0;
            }
        }
    }
}