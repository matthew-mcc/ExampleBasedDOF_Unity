using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;   // for Slider

public class DoFUI_Controller : MonoBehaviour
{
    [Header("Volume / DOF")]
    public Volume volume;       // assign your Volume in the inspector

    private CustomDOF dof;      // runtime reference

    [Header("UI Sliders")]
    public Slider focusDistanceSlider;
    public Slider apertureSlider;
    public Slider focalLengthSlider;

    public List<GameObject> sceneCams; 
    private int currCam = 0;

    public GameObject controls;

    public GameObject freeCam;


    void Start()
    {
        if (volume == null)
        {
            Debug.LogError("[DOFUI] Volume reference is missing.");
            return;
        }

        if (!volume.profile.TryGet(out dof))
        {
            Debug.LogError("[DOFUI] CustomDOF not found in Volume Profile.");
            return;
        }

        // Optional: sync slider ranges to your parameter ranges
        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.minValue = dof.focusDistance.min;
            focusDistanceSlider.maxValue = dof.focusDistance.max;
            focusDistanceSlider.value    = dof.focusDistance.value;
            focusDistanceSlider.onValueChanged.AddListener(OnFocusDistanceChanged);
        }

        if (apertureSlider != null)
        {
            apertureSlider.minValue = dof.aperture.min;
            apertureSlider.maxValue = dof.aperture.max;
            apertureSlider.value    = dof.aperture.value;
            apertureSlider.onValueChanged.AddListener(OnApertureChanged);
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.minValue = dof.focalLength.min;
            focalLengthSlider.maxValue = dof.focalLength.max;
            focalLengthSlider.value    = dof.focalLength.value;
            focalLengthSlider.onValueChanged.AddListener(OnFocalLengthChanged);
        }

        int temp = 0;
        foreach (var cam in sceneCams)
        {   
            if (sceneCams[temp].activeInHierarchy)
            {
                currCam = temp;
            }
            temp++;
        }
        freeCam.SetActive(false);
    }

    void OnFocusDistanceChanged(float v)
    {
        if (dof != null)
            dof.focusDistance.value = v;
    }

    void OnApertureChanged(float v)
    {
        if (dof != null)
            dof.aperture.value = v;
    }

    void OnFocalLengthChanged(float v)
    {
        if (dof != null)
            dof.focalLength.value = v;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            

            sceneCams[currCam].SetActive(false);
            currCam = (currCam + 1) % sceneCams.Count;
            sceneCams[currCam].SetActive(true);


        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (controls.activeInHierarchy)
            {
                controls.SetActive(false);
            }
            else
            {
                controls.SetActive(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!freeCam.activeInHierarchy)
            {
                sceneCams[currCam].SetActive(false);
                freeCam.SetActive(true);
            }
            else
            {
                sceneCams[currCam].SetActive(true);
                freeCam.SetActive(false);
            }
        }
    }
}
