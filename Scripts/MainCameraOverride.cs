using UnityEngine;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Omnilatent.ScenesManager
{
    [RequireComponent(typeof(Camera))]
    public class MainCameraOverride : MonoBehaviour
    {
        private void Start()
        {
            Manager.Object.ToggleBackgroundCamera(false);
#if USING_URP
            var cam = GetComponent<Camera>();
            var data = cam.GetUniversalAdditionalCameraData();
            var uiCamera = Manager.Object.UICamera;
            if (!data.cameraStack.Contains(uiCamera))
                data.cameraStack.Add(uiCamera);
#endif
        }

        private void OnDestroy()
        {
            Manager.Object.ToggleBackgroundCamera(true);
#if USING_URP
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.cameraStack.Remove(Manager.Object.UICamera);
            }
#endif
        }
    }
}
