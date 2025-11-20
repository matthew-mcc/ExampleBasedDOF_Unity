using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SampleDoF_RenderFeature : ScriptableRendererFeature
{

    private DoFPass dofPass;

    public override void Create()
    {
        dofPass = new DoFPass();
    }
    // Adding which passes we want to do!
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(dofPass);
    }


    class DoFPass : ScriptableRenderPass
    {

        private Material _mat;
        int dofId = Shader.PropertyToID("_Temp");
        RenderTargetIdentifier src, dof;

        public DoFPass()
        {
            if (!_mat)
            {
                _mat = CoreUtils.CreateEngineMaterial("Unlit/URPDoF"); // NOT SURE ABOUT THIS
            }
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // base.OnCameraSetup(cmd, ref renderingData);
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            src = renderingData.cameraData.renderer.cameraColorTarget;
            cmd.GetTemporaryRT(dofId, desc, FilterMode.Bilinear);
            dof = new RenderTargetIdentifier(dofId);
        }

        // Executing the pass / buffer
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer commandBuffer = CommandBufferPool.Get("SampleDoF_RenderFeature");
            VolumeStack volumes = VolumeManager.instance.stack;
            SamplingDoF dofData = volumes.GetComponent<SamplingDoF>();
            if (dofData.IsActive())
            {
                _mat.SetColor("_OverlayColor", (Color)dofData.tintColor);
                _mat.SetFloat("_Intensity", (float)dofData.tintIntensity);

                Blit(commandBuffer, src, dof, _mat, 0);
                
                Blit(commandBuffer, dof, src);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(dofId);
        }

    }
}
