#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PresenterBehaviour : MonoBehaviour
{
    public int currSlide;
    public List<GameObject> slides = new();
    private int _slidesNum;

#if UNITY_EDITOR
    private int _lastSlideIndex = -1;
#endif

    public virtual void Start()
    {
        _slidesNum = GameObject.FindGameObjectsWithTag("Slide").Length;

        foreach (GameObject slide in GameObject.FindGameObjectsWithTag("Slide"))
        {
            slides.Add(slide);
        }

#if UNITY_EDITOR
        EditorApplication.update += EditorUpdate;
#endif
    }

    public virtual void Update()
    {
#if UNITY_EDITOR
        HandleInput(true);
#else
        HandleInput();
#endif

        UpdateCameraTransform();
    }

    private void HandleInput(bool editor = false)
    {
        if (editor)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space))
            {
                if (currSlide < _slidesNum - 1) currSlide++;
            }

            if (!Input.GetKeyDown(KeyCode.A)) return;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space))
            {
                if (currSlide < _slidesNum - 1) currSlide++;
            }

            if (!Input.GetKeyDown(KeyCode.LeftArrow)) return;
        }

        if (currSlide > 0) currSlide--;
    }

    private void UpdateCameraTransform()
    {
        if (slides.Count == 0 || currSlide < 0 || currSlide >= slides.Count) return;

        var targetSlide = slides[currSlide];
        var targetCam = targetSlide.GetComponent<Camera>();

        if (targetCam)
        {
            Camera mainCam = GetComponent<Camera>();
            mainCam.transform.position = targetCam.transform.position;
            mainCam.transform.rotation = targetCam.transform.rotation;
        }
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (Application.isPlaying || _lastSlideIndex == currSlide) return;
        UpdateSceneViewCamera();
        _lastSlideIndex = currSlide;
    }

    private void UpdateSceneViewCamera()
    {
        if (slides.Count == 0 || currSlide < 0 || currSlide >= slides.Count) return;

        var targetSlide = slides[currSlide];
        var slideCam = targetSlide.GetComponent<Camera>();

        if (slideCam && SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.pivot = slideCam.transform.position;
            SceneView.lastActiveSceneView.rotation = slideCam.transform.rotation;
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdateSceneViewCamera();
        }
    }

    private void OnDestroy()
    {
        EditorApplication.update -= EditorUpdate;
    }
#endif
}