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
    public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);
    public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);
    public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0, -180f, 180f);



    public bool IsActive() => true;

    public bool IsTileCompatible() => false;

}
