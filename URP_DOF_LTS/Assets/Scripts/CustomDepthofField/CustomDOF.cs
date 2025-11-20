using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline("CustomFeatures/CustomDOF", typeof(UniversalRenderPipeline))]
public class CustomDOF : VolumeComponent, IPostProcessComponent
{

    public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);
    public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);
    public ClampedFloatParameter focalLength = new ClampedFloatParameter(50, 1, 300f);




    public bool IsActive() => true;

    public bool IsTileCompatible() => false;

}
