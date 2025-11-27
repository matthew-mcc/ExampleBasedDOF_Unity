using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline("CustomFeatures/CustomDOF", typeof(UniversalRenderPipeline))]
public class CustomDOF : VolumeComponent, IPostProcessComponent
{

    public ClampedFloatParameter focusDistance = new ClampedFloatParameter(10f, 0.1f, 100f);
    public ClampedFloatParameter aperture = new ClampedFloatParameter(3f, 0.1f, 100f);
    public ClampedFloatParameter focalLength = new ClampedFloatParameter(4f, 1f, 10f);



    public bool IsActive() => true;
    // public bool IsActive()
    // {
        // if (!active)
        // {
            // return false;
        // }
        
        // return aperture.value > 0.001f;

    // }

    public bool IsTileCompatible() => false;

}
