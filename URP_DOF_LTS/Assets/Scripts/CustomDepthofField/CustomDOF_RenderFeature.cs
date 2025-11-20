using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomDOF_RenderFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class DOFSettings
    {
        public Shader dofShader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public DOFSettings settings = new DOFSettings();
    private DOFPass dofPass;
    // private Material dofMat;


    public override void Create()
    {
        // dofPass = new DOFPass();
        
        // Try to find shader if it's null
        if (settings.dofShader == null)
        {
            settings.dofShader = Shader.Find("Hidden/Custom/CustomDOF_Bokeh"); // unsure about path name here.
        }

        // Have a shader, let's initialize
        if (settings.dofShader == null)
        {
            Debug.Log("Shader init failed, returning");
            return;
        }
        // if (settings.dofShader != null)
        // {
            // dofMat = CoreUtils.CreateEngineMaterial(settings.dofShader); // seems better than hardcoding
            // dofPass = new DOFPass(dofMat) { renderPassEvent = settings.renderPassEvent }; // doing this here so we have access to settings
        // }

        Material dofMat = CoreUtils.CreateEngineMaterial(settings.dofShader);
        dofPass = new DOFPass(dofMat);
        dofPass.renderPassEvent = settings.renderPassEvent;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (dofPass == null) return;

        renderer.EnqueuePass(dofPass);
    }


    class DOFPass : ScriptableRenderPass
    {
        private Material _mat;
        private RenderTargetIdentifier _src;
        private RenderTargetIdentifier _temp;
        private int _tempRTId = Shader.PropertyToID("_CustomDOF_Temp");


        public DOFPass(Material mat)
        {
            _mat = mat;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Not sure if needed
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            _src = renderingData.cameraData.renderer.cameraColorTarget;
            cmd.GetTemporaryRT(_tempRTId, desc, FilterMode.Bilinear);
            _temp = new RenderTargetIdentifier(_tempRTId);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(_mat == null) return;

            
            CommandBuffer commandBuffer = CommandBufferPool.Get("CustomDOF");
            VolumeStack volumes = VolumeManager.instance.stack;
            CustomDOF dofSettings = volumes.GetComponent<CustomDOF>();
            if(dofSettings == null || !dofSettings.IsActive())
            {
                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
                return;
            }

            _mat.SetFloat("_FocusDistance", dofSettings.focusDistance.value);
            _mat.SetFloat("_FocusRange", dofSettings.focalLength.value);
            // _mat.SetFloat("_FocusDistance", dofSettings.focusDistance.value);
            // _mat.SetFloat("_Aperture", dofSettings.aperture.value);
            // _mat.SetFloat("_FocalLength", dofSettings.focalLength.value);

            // // Bokeh things
            // _mat.SetInt("_BladeCount", dofSettings.bladeCount.value);
            // _mat.SetFloat("_BladeCurvature", dofSettings.bladeCurvature.value);
            // _mat.SetFloat("_BladeRotation", dofSettings.bladeRotation.value * Mathf.Deg2Rad); // not sure if we need to convert here


            // first, blur into temp
            Blit(commandBuffer, _src, _temp, _mat, 0);

            // next, composite back to source // NOT ENTIRELY SURE IF THIS IS CORRECT
            Blit(commandBuffer, _temp, _src);

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_tempRTId);
        }

    }
}
