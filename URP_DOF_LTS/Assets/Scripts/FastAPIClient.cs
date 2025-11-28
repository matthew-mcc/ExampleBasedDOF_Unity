using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SampleRequestDTO
{
    public string sampler;
    public int points;
}

[System.Serializable]
public class SamplePointDTO
{
    public float x;
    public float y;
}

[System.Serializable]
public class SampleResponseDTO
{
    public string status;
    public string sampler;
    public int points;
    public string message;
    public SamplePointDTO[] samples;
}

public class FastAPIClient : MonoBehaviour
{
    public string serverUrl = "http://10.0.0.251:8765/sample";

    public string samplerName = "Owen";
    public int points = 256;

    // Connected to button
    public void RequestNewKernel()
    {
        Debug.Log("[Unity] Requesting new kernel from server...");
        StartCoroutine(SendTestRequest());
    }

    // void Start()
    // {
        // StartCoroutine(SendTestRequest());
    // }

    private IEnumerator SendTestRequest()
    {
        // Debug.Log("[Unity] Sending POST to: " + serverUrl);

        var req = new SampleRequestDTO
        {
            sampler = samplerName,
            points = points
        };

        string json = JsonUtility.ToJson(req);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[Unity] Sending POST to: " + serverUrl);
            yield return www.SendWebRequest();

            Debug.Log($"[Unity] result={www.result}, error={www.error}, code={www.responseCode}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Unity] Request failed: " + www.error);
                yield break;
            }
            string responseText = www.downloadHandler.text;
            Debug.Log("[Unity] Raw response: " + responseText);

            var resp = JsonUtility.FromJson<SampleResponseDTO>(responseText);
            if (resp == null)
            {
                Debug.LogWarning("[Unity] Could not parse response JSON.");
                yield break;
            }

            Debug.Log($"[Unity] Parsed: status={resp.status}, sampler={resp.sampler}, points={resp.points}, msg={resp.message}");

            if (resp.samples == null || resp.samples.Length == 0)
            {
                Debug.LogWarning("[Unity] No samples returned from server.");
                yield break;
            }

            // Build kernel from samples (map [0,1] to [-1,1])
            int n = Mathf.Min(resp.samples.Length, 256); // or your MaxKernelSize
            var kernel = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                float nx = (resp.samples[i].x - 0.5f) * 2.0f;
                float ny = (resp.samples[i].y - 0.5f) * 2.0f;
                kernel[i] = new Vector4(nx, ny, 0f, 0f);
            }

            MLKernelStore.Kernel = kernel;
            Debug.Log($"[Unity] MLKernelStore updated with {n} samples.");

        }

    }
}