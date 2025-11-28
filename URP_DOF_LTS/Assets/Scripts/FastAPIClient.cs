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
public class SampleResponseDTO
{
    public string status;
    public string sampler;
    public int points;
    public string message;
}

public class FastAPIClient : MonoBehaviour
{
    // IMPORTANT: use the voyager IP
    public string serverUrl = "http://10.0.0.251:8765/sample";

    public string samplerName = "LDBN";
    public int points = 256;

    void Start()
    {
        StartCoroutine(SendTestRequest());
    }

    private IEnumerator SendTestRequest()
    {
        Debug.Log("[Unity] Sending POST to: " + serverUrl);

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
            if (resp != null)
            {
                Debug.Log($"[Unity] Parsed: status={resp.status}, sampler={resp.sampler}, points={resp.points}, msg={resp.message}");
            }
            else
            {
                Debug.LogWarning("[Unity] Could not parse response JSON.");
            }
        }
    }
}