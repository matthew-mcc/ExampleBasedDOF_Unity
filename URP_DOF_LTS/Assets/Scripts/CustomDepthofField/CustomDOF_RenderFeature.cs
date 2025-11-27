

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomDOF_RenderFeature : ScriptableRendererFeature
{


    const int circleOfConfusionPass = 0;
    const int preFilterPass = 1;
	const int bokehPass = 2;
	const int postFilterPass = 3;


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
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            _src = renderingData.cameraData.renderer.cameraColorTarget;

            // CoC RT – can be single channel or RGBA, your call
            var cocDesc = desc;
            cocDesc.colorFormat = RenderTextureFormat.RHalf; // or ARGBHalf if you prefer
            cmd.GetTemporaryRT(_cocRTId, cocDesc, FilterMode.Bilinear);

            // DOF RTs – **must** have alpha so we can store CoC there
            var dofDesc = desc;
            dofDesc.colorFormat = RenderTextureFormat.ARGBHalf; // or ARGB32 if you’re LDR

            cmd.GetTemporaryRT(_dof0RTId, dofDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_dof1RTId, dofDesc, FilterMode.Bilinear);

            _cocRT  = new RenderTargetIdentifier(_cocRTId);
            _dof0RT = new RenderTargetIdentifier(_dof0RTId);
            _dof1RT = new RenderTargetIdentifier(_dof1RTId);
            // RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            // desc.depthBufferBits = 0;
            // _src = renderingData.cameraData.renderer.cameraColorTarget;
            // // cmd.GetTemporaryRT(_tempRTId, desc, FilterMode.Bilinear);
            // // _temp = new RenderTargetIdentifier(_tempRTId);
            

            // cmd.GetTemporaryRT(_cocRTId,  desc, FilterMode.Bilinear);
            // cmd.GetTemporaryRT(_dof0RTId, desc, FilterMode.Bilinear);
            // cmd.GetTemporaryRT(_dof1RTId, desc, FilterMode.Bilinear);

            // _cocRT  = new RenderTargetIdentifier(_cocRTId);
            // _dof0RT = new RenderTargetIdentifier(_dof0RTId);
            // _dof1RT = new RenderTargetIdentifier(_dof1RTId);
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

            Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);
            commandBuffer.SetGlobalTexture("_CoCTex", _cocRT);

            Blit(commandBuffer, _src, _dof0RT, _mat, preFilterPass);
            Blit(commandBuffer, _dof0RT, _dof1RT, _mat, bokehPass);
            Blit(commandBuffer, _dof1RT, _src, _mat, postFilterPass);

            // ===== CURRENT BLUR =====
            // (1) coc pass: source -> coc RT (pass 0 in shader)
            // === COC Greyscale ===
            // Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);
            // commandBuffer.SetGlobalTexture("_CoCTex", _cocRT);

            // // prefilter: src -> DOF0
            // Blit(commandBuffer, _src, _dof0RT, _mat, preFilterPass);

            // // DEBUG: show what preFilter wrote
            // Blit(commandBuffer, _dof0RT, _src);
            // Blit(commandBuffer, _src, _cocRT, _mat, preFilterPass); 

            // commandBuffer.Blit(_cocRT, "_CoCTex");

            // Blit(commandBuffer, _cocRT, _src);
            // Blit(commandBuffer, _src, _dof0RT, _mat, preFilterPass);

            // Blit(commandBuffer, _dof0RT, _dof1RT, _mat, bokehPass);

            // // (4) final!
            // Blit(commandBuffer, _dof1RT, _dof0RT, _mat, postFilterPass);

            // Blit(commandBuffer, _dof0RT, _src);
            // ===== END CURRENT BLUR =====

            // ===== RED / BLACK EFFECT =====
            // (1) CoC: source -> CoC RT
            // Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);

            // // DEBUG: show CoC on screen
            // Blit(commandBuffer, _cocRT, _src);

            // ===== END RED / BLACK EFFECT =====

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