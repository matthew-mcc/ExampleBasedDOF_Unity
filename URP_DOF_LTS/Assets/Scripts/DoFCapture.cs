
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DoFCapture : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Volume that contains the CustomDOF component.")]
    public Volume volume;

    [Tooltip("Root object of your UI (e.g., the Canvas or parent). " +
             "This will be disabled for one frame during capture.")]
    public GameObject uiRoot;

    [Tooltip("FastAPI client to read sampler + sample count from.")]
    public FastAPIClient apiClient;

    private CustomDOF dofSettings;

    [Header("Output Settings")]
    public string outputFolderName = "DoF_Captures"; 
    public int superSize = 1; 

    void Start()
    {
        if (volume == null)
        {
            Debug.LogError("[DoFCapture] Volume reference missing.");
            return;
        }

        if (!volume.profile.TryGet(out dofSettings))
        {
            Debug.LogError("[DoFCapture] CustomDOF not found in Volume Profile.");
        }

        if (uiRoot == null)
        {
            Debug.LogWarning("[DoFCapture] uiRoot not assigned; UI will appear in captures.");
        }
    }


    public void CaptureLabeledScreenshot()
    {
        if (dofSettings == null)
        {
            Debug.LogError("[DoFCapture] No CustomDOF settings available.");
            return;
        }

        StartCoroutine(CaptureCoroutine());
    }

    private System.Collections.IEnumerator CaptureCoroutine()
    {
        // --- 1. Gather metadata ---
        string sampler = (apiClient != null) ? apiClient.samplerName : "UnknownSampler";
        int samples = (apiClient != null) ? apiClient.points : MLKernelStore.KernelCount;

        float aperture = dofSettings.aperture.value;
        float focusDistance = dofSettings.focusDistance.value;
        float focalLength = dofSettings.focalLength.value;

        string samplerSafe = sampler.Replace(" ", "");

        string baseName = $"{samples}_samples_{samplerSafe}" + $"_ap{aperture:0.00}_fd{focusDistance:0.00}_fl{focalLength:0.00}";

#if UNITY_EDITOR
        string folder = Path.Combine(Application.dataPath, "..", outputFolderName);
#else
        string folder = Path.Combine(Application.persistentDataPath, outputFolderName);
#endif
        Directory.CreateDirectory(folder);

        string fullPath = Path.Combine(folder, baseName + ".png");

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fullPath = Path.Combine(folder, $"{baseName}_{counter}.png");
            counter++;
        }
        
        Debug.Log($"[DoFCapture] Capturing PNG to: {fullPath}");

        // --- 2. Hide UI for one frame ---
        bool uiWasActive = (uiRoot != null) && uiRoot.activeSelf;
        if (uiRoot != null)
            uiRoot.SetActive(false);

        // Wait for the frame to render WITHOUT UI
        yield return new WaitForEndOfFrame();

        // --- 3. Capture the screen (from whichever camera is active) ---
        ScreenCapture.CaptureScreenshot(fullPath, superSize);

        // --- 4. Restore UI state ---
        if (uiRoot != null)
            uiRoot.SetActive(uiWasActive);

        Debug.Log("[DoFCapture] Screenshot requested (UI hidden for capture).");
    }
}
