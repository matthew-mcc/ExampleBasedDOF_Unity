using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline("CustomFeatures/CustomDOF", typeof(UniversalRenderPipeline))]
public class CustomDOF : VolumeComponent, IPostProcessComponent
{

    public ClampedFloatParameter focusDistance = new ClampedFloatParameter(1f, 1f, 20f);
    public ClampedFloatParameter aperture = new ClampedFloatParameter(1f, 1f, 30f);
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
