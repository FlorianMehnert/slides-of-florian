using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class PresenterBehaviour : MonoBehaviour
{
    public int currSlide;
    public List<GameObject> slides = new();
    private GameObject[] _sections;
    private int _slidesNum;
    public virtual void Start()
    {
        // set the camera field of view to be compatible with the screen and keep the FOVhorizontal constant
        this.GetComponent<Camera>().fieldOfView = (((GetComponent<Camera>().fieldOfView * 16) / 9) * 1) / GetComponent<Camera>().aspect;
        /*
        We use two tags to reference and organize slides (scenes) : Slide and Section

        Slides should be organized in sections. Sections are read ordered by how they appear in the
        hierarchy and then we iterate through slides inside each sectino and get them in the order they
        appear in the editor window's hierarchy

        You have to create at least 1 section and 1 slide for a slideshow
           */
        _slidesNum = GameObject.FindGameObjectsWithTag("Slide").Length;
        _sections = GameObject.FindGameObjectsWithTag("Section");

        Array.Sort(_sections, Compare);
        var i = 0;
        while (i < _sections.Length)
        {
            foreach (Transform child in _sections[i].transform)
            {
                if (child.gameObject.tag != "Slide") continue;
                child.gameObject.GetComponent<Camera>().enabled = false; // disable all cameras on start
                slides.Add(child.gameObject);
            }
            i++;
        }
    }

    public virtual void Update()
    {
        /*
        You can use the LEFT and RIGHT keys to navigate between slides
        */
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currSlide < _slidesNum - 1)
            {
                currSlide += 1; //slide number has to be in the range of slides
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currSlide < _slidesNum - 1)
            {
                currSlide += 1; //slide number has to be in the range of slides
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currSlide > 0)
            {
                currSlide -= 1; //slide number has to be in the range of slides
            }
        }
        
        if (Input.touchCount > 0) // Check if there is at least one touch
        {
            Touch touch = Input.GetTouch(0); // Get the first touch

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    break;

                case TouchPhase.Ended:
                    Vector2 touchEndPos = touch.position;
                    Vector2 swipeDelta = touchEndPos - touchStartPos;

                    if (Mathf.Abs(swipeDelta.x) > 50) // Adjust the threshold as needed
                    {
                        if (swipeDelta.x > 0)
                        {
                            if (currSlide > 0)
                            {
                                currSlide--;
                            }
                        }
                        else
                        {
                            // Swipe left: go to the next slide
                            if (currSlide < (slidesNum - 1))
                            {
                                currSlide++;
                            }
                        }
                    }
                    break;
            }
        }
        
        /*
        Slide transitions:
        Transition is either 0 or some positive float. If zero no animation happens, if the number is bigger then zero, then
        the transition time attribute of the current slide will control the speed of the transition.

        The camera that goes through the scenes will inherit some attributes of the camera of the given slides.
        Currently these are:
         	-  clipping planes
         	-  skybox

           */
        RenderSettings.skybox = ((Skybox) slides[currSlide].GetComponent<Camera>().GetComponent(typeof(Skybox))).material;
        GetComponent<Camera>().nearClipPlane = slides[currSlide].GetComponent<Camera>().nearClipPlane;
        GetComponent<Camera>().farClipPlane = slides[currSlide].GetComponent<Camera>().farClipPlane;
        var transitionTime = ((Attributes) slides[currSlide].GetComponent(typeof(Attributes))).transitionTime;
        if (transitionTime > 0)
        {
            transform.position = Vector3.Lerp(transform.position, slides[currSlide].transform.position, Time.deltaTime / transitionTime * 1.15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, slides[currSlide].transform.rotation, Time.deltaTime / transitionTime);
        }
        else
        {
            transform.position = slides[currSlide].transform.position;
            transform.rotation = slides[currSlide].transform.rotation;
        }
    }

    public virtual int Compare(GameObject go1, GameObject go2)
    {
        // compare function to sort the sections
        return string.Compare(go1.name, go2.name, StringComparison.Ordinal);
    }

}