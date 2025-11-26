
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomDOF_RenderFeature : ScriptableRendererFeature
{


    const int circleOfConfusionPass = 0;
	const int bokehPass = 1;
	const int postFilterPass = 2;


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

        /*
        var camData = renderingData.cameraData;

        // Optional: avoid running on SceneView / preview / reflection cameras
        if (camData.isSceneViewCamera ||
            camData.cameraType == CameraType.Preview ||
            camData.cameraType == CameraType.Reflection)
            return;

        // Optional: only when post-processing is enabled on this camera
        if (!camData.postProcessEnabled)
            return;
        */
        renderer.EnqueuePass(dofPass);
    }


    class DOFPass : ScriptableRenderPass
    {
        private Material _mat;
        // private RenderTargetIdentifier _src;
        // private RenderTargetIdentifier _temp;
        // private int _tempRTId = Shader.PropertyToID("_CustomDOF_Temp");

        private int _cocRTId  = Shader.PropertyToID("_CustomDOF_CoC");
        private int _dof0RTId = Shader.PropertyToID("_CustomDOF_DOF0");
        private int _dof1RTId = Shader.PropertyToID("_CustomDOF_DOF1");

        private RenderTargetIdentifier _src;
        private RenderTargetIdentifier _cocRT;
        private RenderTargetIdentifier _dof0RT;
        private RenderTargetIdentifier _dof1RT;

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
            // cmd.GetTemporaryRT(_tempRTId, desc, FilterMode.Bilinear);
            // _temp = new RenderTargetIdentifier(_tempRTId);

            cmd.GetTemporaryRT(_cocRTId,  desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_dof0RTId, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_dof1RTId, desc, FilterMode.Bilinear);

            _cocRT  = new RenderTargetIdentifier(_cocRTId);
            _dof0RT = new RenderTargetIdentifier(_dof0RTId);
            _dof1RT = new RenderTargetIdentifier(_dof1RTId);
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
            _mat.SetFloat("_Aperture", dofSettings.aperture.value);

            // ===== CURRENT BLUR =====
            // (1) coc pass: source -> coc RT (pass 0 in shader)
            Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);

            // (DOESNT WORK: ) _mat.SetTexture("_CoCTex", _cocRT);
            
            // (2) Bokeh pass: source (color) -> DOF0 RT (pass 1 in shader)
            Blit(commandBuffer, _src, _dof0RT, _mat, bokehPass);

            // (3) post-filter pass: DOF0 -> DOF1 (pass 2 in shader)
            Blit(commandBuffer, _dof0RT, _dof1RT, _mat, postFilterPass);

            // (4) final!
            Blit(commandBuffer, _dof1RT, _src);
            // // first, blur into temp
            // Blit(commandBuffer, _src, _temp, _mat, 2); // switch between different passes in shader

            // ===== END CURRENT BLUR =====

            // ===== RED / BLACK EFFECT =====
            // (1) CoC: source -> CoC RT
            Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);

            // DEBUG: show CoC on screen
            Blit(commandBuffer, _cocRT, _src);

            // ===== END RED / BLACK EFFECT =====

            // // next, composite back to source // NOT ENTIRELY SURE IF THIS IS CORRECT
            // Blit(commandBuffer, _temp, _src);

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_cocRTId);
            cmd.ReleaseTemporaryRT(_dof0RTId);
            cmd.ReleaseTemporaryRT(_dof1RTId);
        }

    }
}
