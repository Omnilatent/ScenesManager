# 1.2.2
New Features:
- Add MainCameraOverride component to correctly manage the URP camera stack when a runtime-created camera overrides the manager's background camera.
- Editor: automatically declare scripting define symbol OMNILATENT_SCENESMANAGER on installation.

Changes:
- ManagerObject: expose ShieldColor property; Manager now reads the default shield color from ManagerObject instead of a hardcoded value, so the shield color stays consistent when starting from a different scene.
- Deprecate ActivateManagerCameraOnDestroy in favor of MainCameraOverride.

Fixes:
- Guard UnityEngine.InputSystem usage behind ENABLE_INPUT_SYSTEM so the package compiles when the new Input System package isn't installed.
- Update asmdef references (SimpleAnimation, DOTween) to GUID references to fix broken/missing assembly references.

# 1.2.1
New Features:
- Add onSceneAdded event.
- LoadingAnywhere: Add GetCachedLoadingScreen() function.
- On scene load, check cameras if they render the same layers and give performance warning.
- ActivateManagerCameraOnDestroy: disable ManagerObject's BG camera on Start so you can add this component to a scene camera without having to register it in scene controller.

Changes:
- API change: Added onShown callback to Show().
- Update loading screen: remove rarely used slider component, improve default loading screen to a rotating circle. Restructure extra folder.
- Update template scene's field canvas.
- Log warning if Bg camera is not active and scene does not have any camera.

# 1.2.0
News:
- Loading screen: add action on hide parameter to Hide() function.
- LoadingScreen: Add ability to set custom minimum loading time in prefab.
- Scene animation: add toggle to allow skip show & hide animation.
- Controller: allow customizable shield as SceneAnimation object (serialize field). 
- Manager object: refactor enum ShieldFadeOut to SHIELD_FADE_OUT.

Changes:
- Log error if try to Close() a scene that was already hidden.
- Check deprecated usage of field Canvas in OnValidate instead of during runtime.
- change scene animation field from private to protected.
- LoadingAnywhere: Change default prefab path to public.

Fixes:
- Scene transition shield: fix error when DG tween is not installed.
- Fix scene transition shield not deactive correctly when not use Simple animation.
- Fix loading screen break when call hide when it was already hidden.
- Fix scene manager's OnShown not called when scene fade duration is 0.

# 1.1.0
Potential breaking changes/fixes:
- Change to Manager.Unload(Controller controller) & Manager.OnHidden(Controller sender): the unloading scene's data will be removed from interSceneDatas before calling Controller.OnHidden() to fix issue with showing the same scene during previous scene's OnHidden().

Fixes:
- Fix loading anywhere start coroutine error when trying to hide a loading screen and no loading screen is showing

# 1.0.0
News:
- Add Popup template scene. Move template scene to FullScene folder.
- Add getter for active main controller.
- Allow customize the amount of layer order addition each scene added.

Changes
- Template scene: set Canvas's Layer to UI, disable Simple Animation unnecessary call to Show() on Start() in inspector.
- No longer requires Simple Animation and DOTween to be installed first for code to compile.
- Loading anywhere hide: check cached loading screens key to prevent key not found exception.

Fixes
- Fix SceneCreatorWindow exception when generating scene.

# 0.2.1
News:
- Add callbacks onSceneLoadStart, onSceneLoaded, onSceneHidden when main scene is changing.

# 0.2.0
Changes:
- Deprecate Canvas field in Controller. Use List<Canvas> Canvases field instead to allow a scene to has multiple Canvases.
- If scene's canvas's sorting order is set to a value different from 0, its sorting order will not be changed to the scene's sorting order.

# 0.1.0
- First version.