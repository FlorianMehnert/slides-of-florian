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

    private void Start()
    {
        _mainCamera = GetComponent<Camera>();
        
        // Adjust camera FOV for aspect ratio
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
        // In editor, simply add all slides
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
                if (child.CompareTag("Slide"))
                {
                    slides.Add(child.gameObject);
                    var slideCamera = child.GetComponent<Camera>();
                    if (slideCamera != null) slideCamera.enabled = false;
                }
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
        bool shouldAdvance = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space);
        bool shouldReverse = Input.GetKeyDown(KeyCode.LeftArrow);

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

        if (slideCamera != null && _mainCamera != null)
        {
            // Update camera settings
            UpdateSlideSettings(currentSlideObj);

            // Handle transitions
            float transitionTime = slideAttributes != null ? slideAttributes.transitionTime : 0f;
            
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
        if (_mainCamera == null) return;
        
        var slideCamera = slide.GetComponent<Camera>();
        if (slideCamera == null) return;

        // Update camera settings
        _mainCamera.nearClipPlane = slideCamera.nearClipPlane;
        _mainCamera.farClipPlane = slideCamera.farClipPlane;

        // Update skybox if available
        var slideSkybox = slide.GetComponent<Skybox>();
        if (slideSkybox != null && slideSkybox.material != null)
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
}