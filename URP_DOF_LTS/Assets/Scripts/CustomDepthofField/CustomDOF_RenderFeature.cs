

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CustomDOF_RenderFeature : ScriptableRendererFeature
{


    const int circleOfConfusionPass = 0;
    const int preFilterPass = 1;
	const int bokehPass = 2;
	const int postFilterPass = 3;


    const int MaxKernelSize = 256;
    private static Vector4[] s_Kernel;
    private static int s_KernelCount;


    public static Vector2[] LoadNpyFile(string filepath)
    {
        try
        {
            Debug.Log($"[LoadNpyFile] Reading file: {filepath}");
            byte[] bytes = File.ReadAllBytes(filepath);
            Debug.Log($"[LoadNpyFile] bytes.Length = {bytes.Length}");
            // --- 1. Basic header parsing (NPY v1.0) ---
            // magic string: \x93NUMPY
            if (bytes.Length < 10 ||
                bytes[0] != 0x93 || bytes[1] != (byte)'N' || bytes[2] != (byte)'U' ||
                bytes[3] != (byte)'M' || bytes[4] != (byte)'P' || bytes[5] != (byte)'Y')
            {
                Debug.LogError("Not a valid .npy file (missing magic header).");
                return null;
            }

            byte major = bytes[6];
            byte minor = bytes[7];
            if (major != 1 || minor != 0)
            {
                Debug.LogWarning($"NPY version {major}.{minor} not explicitly handled, trying anyway.");
            }

            // header length (little endian UInt16)
            int headerLen = bytes[8] | (bytes[9] << 8);
            int headerStart = 10;
            string header = Encoding.ASCII.GetString(bytes, headerStart, headerLen);

            // Optional: log header for debugging
            Debug.Log($"NPY header: {header}");

            // --- 2. (Optional) parse dtype & shape from header ---
            var descrMatch = Regex.Match(header, @"'descr':\s*'([^']+)'");
            string descr = descrMatch.Success ? descrMatch.Groups[1].Value : "";
            if (descr != "<f8")
            {
                Debug.LogWarning($"Expected '<f8' dtype, got '{descr}'. Loader assumes float64.");
            }

            var shapeMatch = Regex.Match(header, @"'shape':\s*\(([^)]*)\)");
            int[] shape = null;
            if (shapeMatch.Success)
            {
                string[] parts = shapeMatch.Groups[1].Value.Split(',');
                List<int> dims = new List<int>();
                foreach (var p in parts)
                {
                    if (int.TryParse(p.Trim(), out int v))
                        dims.Add(v);
                }
                shape = dims.ToArray();
                Debug.Log($"NPY shape: ({string.Join(", ", shape)})");
            }

            // --- 3. Read raw float64 data ---
            int dataOffset = headerStart + headerLen;
            int dataBytes = bytes.Length - dataOffset;
            int elementSize = 8; // float64
            int doubleCount = dataBytes / elementSize;

            double[] values = new double[doubleCount];
            Buffer.BlockCopy(bytes, dataOffset, values, 0, dataBytes);

            // Our file is (1, 256, 2) → flatten to 256 Vector2
            int numVec2 = doubleCount / 2;
            Vector2[] samples = new Vector2[numVec2];
            for (int i = 0; i < numVec2; i++)
            {
                float x = (float)values[2 * i + 0];
                float y = (float)values[2 * i + 1];
                samples[i] = new Vector2(x, y);
            }

            // --- 4. Debug print a few samples ---
            Debug.Log($"Loaded {samples.Length} samples from {filepath}");
            int toPrint = Mathf.Min(8, samples.Length);
            for (int i = 0; i < toPrint; i++)
            {
                Debug.Log($"Sample {i}: {samples[i]}");
            }
            int n = Mathf.Min(samples.Length, MaxKernelSize);
            s_Kernel = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                // Your data is in [0,1] — remap to [-1,1]
                float nx = (samples[i].x - 0.5f) * 2.0f;
                float ny = (samples[i].y - 0.5f) * 2.0f;
                s_Kernel[i] = new Vector4(nx, ny, 0f, 0f);
            }
            s_KernelCount = n;

            Debug.Log($"Kernel prepared with {s_KernelCount} samples.");

            return samples;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading .npy file: {ex.Message}");
            return null;
        }
}

    [System.Serializable]
    public class DOFSettings
    {
        public Shader dofShader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public DOFSettings settings = new DOFSettings();
    private DOFPass dofPass;
    // private Material dofMat;

    public string npyRelativePath = "Owen_16x16.npy"; // TODO: move to inspector..
    private static Vector2[] _poissonSamples;

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

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, npyRelativePath);
        Debug.Log($"[CustomDOF] Trying to load NPY from: {fullPath}");
        _poissonSamples = LoadNpyFile(fullPath);


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

            // if(s_Kernel != null && s_KernelCount > 0)
            // {
                // _mat.SetInt("_KernelCount", s_KernelCount);
                // _mat.SetVectorArray("_Kernel", s_Kernel);
            // }
            // else
            // {
                // Debug.Log("S_kernel doesn't exist, or count < 0");
                // _mat.SetInt("_KernelCount", 0);
            // }

            if (MLKernelStore.Kernel != null && MLKernelStore.Kernel.Length > 0)
            {
                int count = MLKernelStore.Kernel.Length;
                _mat.SetInt("_KernelCount", count);
                _mat.SetVectorArray("_Kernel", MLKernelStore.Kernel);
            }
            else if (s_Kernel != null && s_KernelCount > 0)
            {
                _mat.SetInt("_KernelCount", s_KernelCount);
                _mat.SetVectorArray("_Kernel", s_Kernel);
            }
            else
            {
                Debug.Log("No kernel available (MLKernelStore and s_Kernel both empty)");
                _mat.SetInt("_KernelCount", 0);
            }

            Blit(commandBuffer, _src, _cocRT, _mat, circleOfConfusionPass);
            commandBuffer.SetGlobalTexture("_CoCTex", _cocRT);

            Blit(commandBuffer, _src, _dof0RT, _mat, preFilterPass);
            Blit(commandBuffer, _dof0RT, _dof1RT, _mat, bokehPass);
            Blit(commandBuffer, _dof1RT, _src, _mat, postFilterPass);


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