using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomTint_RenderFeature : ScriptableRendererFeature
{

    private TintPass tintPass;

    public override void Create()
    {
        tintPass = new TintPass();
    }
    // Adding which passes we want to do!
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(tintPass);
    }


    class TintPass : ScriptableRenderPass
    {

        private Material _mat;
        int tintId = Shader.PropertyToID("_Temp");
        RenderTargetIdentifier src, tint;

        public TintPass()
        {
            if (!_mat)
            {
                _mat = CoreUtils.CreateEngineMaterial("Unlit/CustomTintShader"); // NOT SURE ABOUT THIS
            }
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // base.OnCameraSetup(cmd, ref renderingData);
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            src = renderingData.cameraData.renderer.cameraColorTarget;
            cmd.GetTemporaryRT(tintId, desc, FilterMode.Bilinear);
            tint = new RenderTargetIdentifier(tintId);
        }

        // Executing the pass / buffer
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer commandBuffer = CommandBufferPool.Get("CustomTint_RenderFeature");
            VolumeStack volumes = VolumeManager.instance.stack;
            CustomTint tintData = volumes.GetComponent<CustomTint>();
            if (tintData.IsActive())
            {
                _mat.SetColor("_OverlayColor", (Color)tintData.tintColor);
                _mat.SetFloat("_Intensity", (float)tintData.tintIntensity);

                Blit(commandBuffer, src, tint, _mat, 0);
                
                Blit(commandBuffer, tint, src);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(tintId);
        }

    }
}
