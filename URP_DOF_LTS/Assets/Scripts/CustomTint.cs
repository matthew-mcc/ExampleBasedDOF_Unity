using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline ("CustomFeatures/SamplingDoF", typeof(UniversalRenderPipeline))]
public class CustomTint : VolumeComponent, IPostProcessComponent
{

    public FloatParameter tintIntensity = new FloatParameter(1);
    public ColorParameter tintColor = new ColorParameter(Color.white);
    
    public bool IsActive() => true;

    public bool IsTileCompatible() => true;

}
