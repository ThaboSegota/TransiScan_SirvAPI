using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class SirvSpinUploader : MonoBehaviour
{
    [Header("Sirv API Credentials")]
    public string clientID = "YOUR_NEW_CLIENT_ID";
    public string clientSecret = "YOUR_NEW_CLIENT_SECRET";

    [Header("UI")]
    public Button uploadButton;             // Drag your Scan & Upload Button here

    [HideInInspector] public bool isTokenReady = false;

    private string accessToken;
    private const string authURL = "https://api.sirv.com/v2/token";

    private void Start()
    {
        if (uploadButton != null)
            uploadButton.interactable = false;

        StartCoroutine(GetAccessToken());
    }

    private IEnumerator GetAccessToken()
    {
        var body    = $"{{\"clientId\":\"{clientID}\",\"clientSecret\":\"{clientSecret}\"}}";
        var raw     = Encoding.UTF8.GetBytes(body);

        using (var req = new UnityWebRequest(authURL, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<TokenResponse>(req.downloadHandler.text);
                accessToken  = resp.access_token;
                isTokenReady = true;
                if (uploadButton != null) uploadButton.interactable = true;
                Debug.Log("✅ Access token acquired");
            }
            else
            {
                Debug.LogError("❌ GetAccessToken failed: " + req.error);
            }
        }
    }

    public void StartUpload(byte[] imageData, string fileName)
    {
        if (!isTokenReady)
        {
            Debug.LogError("❌ Token not ready. Waiting...");
            StartCoroutine(WaitAndRetryUpload(imageData, fileName));
            return;
        }
        StartCoroutine(UploadImageCoroutine(imageData, fileName));
    }

    private IEnumerator WaitAndRetryUpload(byte[] imageData, string fileName)
    {
        yield return new WaitUntil(() => isTokenReady);
        StartCoroutine(UploadImageCoroutine(imageData, fileName));
    }

    private IEnumerator UploadImageCoroutine(byte[] imageData, string fileName)
    {
        // Direct PUT into your MyUploads folder
        string putUrl = $"https://api.sirv.com/v2/files/upload/MyUploads/{fileName}";

        using (var req = UnityWebRequest.Put(putUrl, imageData))
        {
            req.SetRequestHeader("Content-Type", "image/jpeg");
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Image uploaded via PUT: " + req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"❌ PUT upload failed {req.responseCode} – {req.error}\n{req.downloadHandler.text}");
            }
        }
    }

    [System.Serializable]
    private class TokenResponse
    {
        public string access_token;
    }
}
