using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omnilatent.ScenesManager
{
    public class Manager
    {
        public class SceneData
        {
            public object data;
            public Action onShown;
            public Action onHidden;
            public Scene scene;
            public LoadSceneMode loadSceneMode = LoadSceneMode.Single; //popup or single scene
            public int canvasSortOrder;

            public enum State { Loading, Loaded, Showing, Unloaded }
            State sceneState;
            public State SceneState => sceneState;

            public SceneData()
            {
            }

            public SceneData(object data, Action onShown, Action onHidden, LoadSceneMode loadSceneMode)
            {
                this.data = data;
                this.onShown = onShown;
                this.onHidden = onHidden;
                this.loadSceneMode = loadSceneMode;
            }

            public void SetCanvasSortOrder(int canvasOrder) { canvasSortOrder = canvasOrder; }
        }

        public static Color ShieldColor; //shield under the popup scene
        public static float SceneAnimationDuration;
        public static ManagerObject Object;

        static string m_MainSceneName;
        static Controller m_MainController;

        /// <summary>
        /// Get current active main scene controller
        /// </summary>
        public static Controller MainController
        {
            get => m_MainController;
        }

        static float sceneFadeDuration;
        public static float SceneFadeDuration { get => sceneFadeDuration; set => sceneFadeDuration = value; }

        static LinkedList<Controller> m_ControllerList = new LinkedList<Controller>();

        //Store temporary data passing between scenes
        static Dictionary<string, SceneData> interSceneDatas = new Dictionary<string, SceneData>();

        //Load Scene Async
        static bool loadingSceneAsync; //set to true when LoadAsync is used
        static AsyncOperation loadSceneOperation;
        //static Action<AsyncOperation> onNextSceneAsyncLoaded;
        //static Action<float> onSceneLoadProgressUpdate;
        public const string resourcePath = "ScenesManager";

        public delegate void SceneChangeStartDelegate(string targetSceneName);
        public delegate void SceneChangedDelegate(Controller sender);
        public static SceneChangeStartDelegate onSceneLoadStart;
        public static SceneChangedDelegate onSceneLoaded;
        public static SceneChangedDelegate onSceneHidden;        
        public static SceneChangedDelegate onSceneAdded;


        static Manager()
        {
            Application.targetFrameRate = 60;

            SceneManager.sceneLoaded += OnUnitySceneLoaded;

            // ShieldColor = new Color(0f, 0f, 0f, 0.45f);
            SceneFadeDuration = 0.15f;
            SceneAnimationDuration = 0.283f;

            string managerObjectName = "ManagerObject";
#if USING_URP
            managerObjectName = "ManagerObjectURP";
#endif
            Object = ((GameObject)GameObject.Instantiate(Resources.Load(System.IO.Path.Combine(resourcePath, managerObjectName)))).GetComponent<ManagerObject>();
            ShieldColor = Object.ShieldColor;
        }

        private static void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var data = GetSceneData(scene.name, true);
            data.scene = scene;
            if (string.IsNullOrEmpty(m_MainSceneName))
            {
                //This is first loaded scene, set it as main controller
                m_MainSceneName = scene.name;
                m_MainController = GetController(scene);
            }
        }

        public static void OnSceneLoaded(Controller sender)
        {
            if (string.IsNullOrEmpty(m_MainSceneName) || sender.SceneName() == m_MainSceneName)
            {
                m_MainController = sender;
            }

            SceneData data = GetSceneData(sender.SceneName(), true);

            // Single Mode automatically destroy all scenes, so we have to clear the list.
            if (data.loadSceneMode == LoadSceneMode.Single)
            {
                m_ControllerList.Clear();
            }

            m_ControllerList.AddFirst(sender);

            sender.SceneData = data;
            sender.SetupCanvas(data.canvasSortOrder);
            sender.CreateShield();
            sender.OnActive(data.data);
            // Animation
            if (m_ControllerList.Count == 1)
            {
                // Own Camera
                if (sender.Camera != null)
                {
                    // Object.ToggleBackgroundCamera(false);

                    if (sender.Camera.GetComponent<ActivateManagerCameraOnDestroy>() == null)
                    {
                        sender.Camera.gameObject.AddComponent<ActivateManagerCameraOnDestroy>();
                    }
                }
                else if (!Manager.Object.BgCamera.activeInHierarchy)
                {
                    Debug.LogWarning("Controller does not have camera and default camera is inactive. Add component 'ActivateManagerCameraOnDestroy' to custom camera to fix this.");
                }

                // Main Scene
                m_MainController = sender;

                // Fade
                Object.FadeInScene();
                onSceneLoaded?.Invoke(sender);
            }
            else
            {
                // Popup Scene
                sender.Show();
                onSceneAdded?.Invoke(sender);
            }
        }

        /// <summary>
        /// Add inter-scene data to dictionary. If a scene data for that scene already exist, overwrite it with new data.
        /// </summary>
        static void AddSceneData(string sceneName, SceneData sceneData)
        {
            if (interSceneDatas.ContainsKey(sceneName))
            {
                Debug.LogWarning($"A scene data for scene '{sceneName}' already exist. This might lead to errors. Did you load 1 scene multiple times?");
                interSceneDatas[sceneName] = sceneData;
            }
            else interSceneDatas.Add(sceneName, sceneData);
        }

        static SceneData GetSceneData(string sceneName, bool createIfNotExist)
        {
            if (!interSceneDatas.TryGetValue(sceneName, out var sceneData) && createIfNotExist)
            {
                //Debug.Log($"Scene data for '{sceneName}' does not exist, new data will be created.");
                AddSceneData(sceneName, new SceneData());
                sceneData = interSceneDatas[sceneName];
            }
            return sceneData;
        }

        public static void Add(string sceneName, object data = null, Action onShown = null, Action onHidden = null)
        {
            Object.ShieldOn();
            SceneData sceneData = new SceneData(data, onShown, onHidden, LoadSceneMode.Additive);
            sceneData.SetCanvasSortOrder(GetTopSceneSortOrder() + Object.LayerSortOrderAddedEachCanvas);
            AddSceneData(sceneName, sceneData);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Fade out current scene and load new scene. Call <see cref="OnFadedOut()"/> after scene is faded out.
        /// </summary>
        /// <param name="data">Data pass to new scene</param>
        public static void Load(string sceneName, object data = null)
        {
            loadingSceneAsync = false;
            interSceneDatas.Clear();
            SceneData sceneData = new SceneData(data, null, null, LoadSceneMode.Single);
            AddSceneData(sceneName, sceneData);
            m_MainSceneName = sceneName;
            Object.FadeOutScene();
            onSceneLoadStart?.Invoke(sceneName);
        }

        public static async void LoadAsync(string sceneName, object data = null, Action onSceneLoaded = null, Action<float> onProgressUpdate = null, int minimumLoadTimeMilisec = 0)
        {
            if (loadingSceneAsync)
            {
                Debug.LogException(new Exception("Loading multiple scenes async at the same time is not supported."));
            }
            loadingSceneAsync = true;
            interSceneDatas.Clear();
            SceneData sceneData = new SceneData(data, null, null, LoadSceneMode.Single);
            AddSceneData(sceneName, sceneData);
            onSceneLoadStart?.Invoke(sceneName);
            loadSceneOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            loadSceneOperation.allowSceneActivation = false;
            //loadSceneOperation.completed += (AsyncOperation asyncOp) => { OnSceneAsyncLoaded(sceneName, asyncOp, onSceneLoaded); };
            while (loadSceneOperation.progress < 0.9f)
            {
                onProgressUpdate?.Invoke(loadSceneOperation.progress);
                await Task.Delay(20);
            }
            if (minimumLoadTimeMilisec > 0) { await Task.Delay(minimumLoadTimeMilisec); }
            OnSceneAsyncLoaded(sceneName, loadSceneOperation, onSceneLoaded);
        }

        static void OnSceneAsyncLoaded(string sceneName, AsyncOperation asyncOperation, Action onSceneLoaded)
        {
            onSceneLoaded?.Invoke();
            m_MainSceneName = sceneName;
            Object.FadeOutScene();
        }
        public static void OnFadedIn()
        {
            m_MainController.OnShown();
        }

        public static void OnFadedOut()
        {
            if (m_MainController != null)
            {
                m_MainController.OnHidden();
            }

            if (loadingSceneAsync)
            {
                loadSceneOperation.allowSceneActivation = true;
                loadingSceneAsync = false;
            }
            else
                SceneManager.LoadScene(m_MainSceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// Called when a popup scene finish show animation
        /// </summary>
        /// <param name="controller"></param>
        public static void OnShown(Controller controller)
        {
            /*if (controller.FullScreen && m_ControllerStack.Count > 1)
            {
                ActivatePreviousController(controller, false);
            }*/

            controller.OnShown();
            SceneData sceneData = GetSceneData(controller.SceneName(), true);
            sceneData.onShown?.Invoke();

            Object.ShieldOff();
        }

        public static Controller TopController()
        {
            if (m_ControllerList.Count > 0)
            {
                return m_ControllerList.First.Value;
            }

            return null;
        }

        public static int GetTopSceneSortOrder()
        {
            var topController = TopController();
            if (topController != null)
            {
                var sceneData = GetSceneData(topController.SceneName(), false);
                if (sceneData != null)
                {
                    return sceneData.canvasSortOrder;
                }
            }
            return 0;
        }

        public static void Close(Controller sender)
        {
            if (sender == m_MainController)
            {
                Debug.Log("Only added scene can be closed."); return;
            }

            if (!m_ControllerList.Contains(sender)) { Debug.LogError($"Close failed. Scene {sender} is already closed, this can lead to error."); }

            m_ControllerList.Remove(sender);
            Object.ShieldOn();
            sender.Hide();
        }

        public static void OnHidden(Controller sender)
        {
            interSceneDatas.Remove(sender.SceneName());
            sender.OnHidden();
            sender.SceneData.onHidden?.Invoke();
            onSceneHidden?.Invoke(sender);
            Unload(sender);
            TopController().OnReFocus();
            Object.ShieldOff();
        }

        static void Unload(Controller controller)
        {
            SceneManager.UnloadSceneAsync(controller.SceneData.scene);
        }

        static Controller GetController(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var controller = roots[i].GetComponent<Controller>();
                if (controller != null)
                {
                    return controller;
                }
            }
            return null;
        }
    }
}