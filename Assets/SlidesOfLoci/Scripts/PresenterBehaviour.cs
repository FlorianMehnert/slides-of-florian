using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class PresenterBehaviour : MonoBehaviour
{
    public int currSlide;
    public List<GameObject> slides = new();
    private GameObject[] _sections;
    private int _slidesNum;
    private Camera _mainCamera;

    // Preview settings
    [Header("Preview Settings")]
    public bool showPreview = true;
    public Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    
    private const float PreviewNearSize = 0.5f;
    private Vector3[] _nearCorners = new Vector3[4];
    private Vector3[] _farCorners = new Vector3[4];

    private void Start()
    {
        _mainCamera = GetComponent<Camera>();
        
        // Adjust camera FOV for an aspect ratio
        if (_mainCamera != null)
        {
            _mainCamera.fieldOfView = _mainCamera.fieldOfView * 16f / 9f * (1f / _mainCamera.aspect);
        }

        InitializeSlides();
    }

    private void InitializeSlides()
    {
        _sections = GameObject.FindGameObjectsWithTag("Section");
        _slidesNum = GameObject.FindGameObjectsWithTag("Slide").Length;

        // Clear existing slides
        slides.Clear();

        #if UNITY_EDITOR
        // In the editor, add all slides
        foreach (GameObject slide in GameObject.FindGameObjectsWithTag("Slide"))
        {
            slides.Add(slide);
            var slideCamera = slide.GetComponent<Camera>();
            if (slideCamera != null) slideCamera.enabled = false;
        }
        #else
        // In runtime, organize slides by sections
        Array.Sort(_sections, Compare);
        
        foreach (var section in _sections)
        {
            foreach (Transform child in section.transform)
            {
                if (!child.CompareTag("Slide")) continue;
                slides.Add(child.gameObject);
                var slideCamera = child.GetComponent<Camera>();
                if (slideCamera != null) slideCamera.enabled = false;
            }
        }
        #endif

        // Enable first slide's settings
        if (slides.Count > 0)
        {
            UpdateSlideSettings(slides[0]);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateCurrentSlide();
    }

    private void HandleInput()
    {
        var shouldAdvance = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space);
        var shouldReverse = Input.GetKeyDown(KeyCode.LeftArrow);

        if (shouldAdvance && currSlide < _slidesNum - 1)
        {
            currSlide++;
        }
        else if (shouldReverse && currSlide > 0)
        {
            currSlide--;
        }
    }

    private void UpdateCurrentSlide()
    {
        if (slides.Count == 0 || currSlide < 0 || currSlide >= slides.Count) return;

        var currentSlideObj = slides[currSlide];
        var slideCamera = currentSlideObj.GetComponent<Camera>();
        var slideAttributes = currentSlideObj.GetComponent<Attributes>();

        if (!slideCamera || !_mainCamera) return;
        // Update camera settings
        UpdateSlideSettings(currentSlideObj);

        // Handle transitions
        var transitionTime = slideAttributes != null ? slideAttributes.transitionTime : 0f;
            
        if (transitionTime > 0)
        {
            // Smooth transition
            transform.position = Vector3.Lerp(transform.position, slideCamera.transform.position, 
                Time.deltaTime / transitionTime * 1.15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, slideCamera.transform.rotation, 
                Time.deltaTime / transitionTime);
        }
        else
        {
            // Instant transition
            transform.position = slideCamera.transform.position;
            transform.rotation = slideCamera.transform.rotation;
        }

#if UNITY_EDITOR
        // Update Scene view camera in editor
        if (!Application.isPlaying && SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.pivot = transform.position;
            SceneView.lastActiveSceneView.rotation = transform.rotation;
            SceneView.lastActiveSceneView.Repaint();
        }
        #endif
    }

    private void UpdateSlideSettings(GameObject slide)
    {
        if (!_mainCamera) return;
        
        var slideCamera = slide.GetComponent<Camera>();
        if (!slideCamera) return;

        // Update camera settings
        _mainCamera.nearClipPlane = slideCamera.nearClipPlane;
        _mainCamera.farClipPlane = slideCamera.farClipPlane;

        // Update skybox if available
        var slideSkybox = slide.GetComponent<Skybox>();
        if (slideSkybox && slideSkybox.material != null)
        {
            RenderSettings.skybox = slideSkybox.material;
        }
    }

    public virtual int Compare(GameObject go1, GameObject go2)
    {
        return string.Compare(go1.name, go2.name, StringComparison.Ordinal);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdateCurrentSlide();
        }
    }
    private void OnDrawGizmos()
    {
        if (!showPreview || slides.Count == 0 || currSlide >= slides.Count) return;

        var currentSlide = slides[currSlide];
        if (currentSlide == null) return;

        var slideCamera = currentSlide.GetComponent<Camera>();
        if (slideCamera == null) return;

        // Set the gizmo color
        Gizmos.color = previewColor;

        // Calculate frustum corners
        CalculateFrustumCorners(slideCamera);

        // Draw the camera frustum
        DrawCameraFrustum();

        // Draw the aspect ratio guide at the near plane
        DrawAspectRatioGuide();
    }

    private void CalculateFrustumCorners(Camera camera)
    {
        var near = camera.nearClipPlane;
        var far = camera.farClipPlane;
        
        // Get the corners of the near plane
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            near,
            Camera.MonoOrStereoscopicEye.Mono,
            _nearCorners
        );

        // Get the corners of the far plane
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            far,
            Camera.MonoOrStereoscopicEye.Mono,
            _farCorners
        );

        // Transform corners to world space
        for (var i = 0; i < 4; i++)
        {
            _nearCorners[i] = camera.transform.TransformPoint(_nearCorners[i]);
            _farCorners[i] = camera.transform.TransformPoint(_farCorners[i]);
        }
    }

    private void DrawCameraFrustum()
    {
        // Draw near plane
        for (var i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(_nearCorners[i], _nearCorners[(i + 1) % 4]);
        }

        // Draw far plane
        for (var i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(_farCorners[i], _farCorners[(i + 1) % 4]);
        }

        // Draw lines connecting near and far planes
        for (var i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(_nearCorners[i], _farCorners[i]);
        }
    }

    private void DrawAspectRatioGuide()
    {
        if (slides.Count == 0 || currSlide >= slides.Count) return;

        var currentSlide = slides[currSlide];
        if (currentSlide == null) return;

        var slideCamera = currentSlide.GetComponent<Camera>();
        if (slideCamera == null) return;

        // Draw a small rectangle at the camera position to indicate the center
        var pos = slideCamera.transform.position;

        Gizmos.DrawWireCube(pos, new Vector3(PreviewNearSize, PreviewNearSize * (1f / slideCamera.aspect), 0.01f));
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PresenterBehaviour))]
    public class PresenterBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PresenterBehaviour presenter = (PresenterBehaviour)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Slide Info", EditorStyles.boldLabel);
            
            if (presenter.slides.Count > 0 && presenter.currSlide < presenter.slides.Count)
            {
                var currentSlide = presenter.slides[presenter.currSlide];
                if (currentSlide != null)
                {
                    var camera = currentSlide.GetComponent<Camera>();
                    if (camera != null)
                    {
                        EditorGUILayout.LabelField($"Aspect Ratio: {camera.aspect:F2}");
                        EditorGUILayout.LabelField($"Field of View: {camera.fieldOfView}°");
                        EditorGUILayout.LabelField($"Near Clip: {camera.nearClipPlane}");
                        EditorGUILayout.LabelField($"Far Clip: {camera.farClipPlane}");
                    }
                }
            }
        }
    }
#endif
}