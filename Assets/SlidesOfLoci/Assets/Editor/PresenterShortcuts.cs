#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PresenterShortcuts
{
    static PresenterBehaviour _presenter;

    static PresenterShortcuts()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (_presenter == null)
        {
            _presenter = Object.FindFirstObjectByType<PresenterBehaviour>();
            if (_presenter == null) return;
        }

        bool changed = false;
        
        if (e.keyCode == KeyCode.RightArrow)
        {
            changed = ChangeSlide(1);
        }
        else if (e.keyCode == KeyCode.LeftArrow)
        {
            changed = ChangeSlide(-1);
        }

        if (changed)
        {
            Event.current.Use(); // Mark event as handled
            SceneView.RepaintAll(); // Refresh scene
        }
    }

    private static bool ChangeSlide(int direction)
    {
        if (_presenter == null || _presenter.slides.Count == 0) return false;

        int newSlide = Mathf.Clamp(_presenter.currSlide + direction, 0, _presenter.slides.Count - 1);
        if (newSlide != _presenter.currSlide)
        {
            _presenter.currSlide = newSlide;
            Selection.activeGameObject = _presenter.slides[newSlide]; // Select the slide in Hierarchy
            MoveSceneCameraToSlide();
            EditorUtility.SetDirty(_presenter);
            return true;
        }
        return false;
    }

    private static void MoveSceneCameraToSlide()
    {
        if (_presenter == null || _presenter.currSlide >= _presenter.slides.Count) return;

        GameObject slide = _presenter.slides[_presenter.currSlide];
        Camera slideCamera = slide.GetComponent<Camera>();

        if (slideCamera != null)
        {
            SceneView.lastActiveSceneView.pivot = slideCamera.transform.position;
            SceneView.lastActiveSceneView.rotation = slideCamera.transform.rotation;
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}
#endif
