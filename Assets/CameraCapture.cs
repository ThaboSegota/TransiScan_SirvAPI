using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraCapture : MonoBehaviour
{
    [Header("UI References")]
    public RawImage displayImage;
    public SirvSpinUploader uploader;   // Drag SirvSpinUploader GameObject here

    private WebCamTexture webcamTexture;

    private void Start()
    {
        StartCoroutine(StartCamera());
    }

    private IEnumerator StartCamera()
    {
        webcamTexture = new WebCamTexture();
        displayImage.texture = webcamTexture;
        displayImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();
        yield return null;
    }

    public void CaptureAndUpload()
    {
        if (!uploader.isTokenReady)
        {
            Debug.LogError("⛔ Upload blocked: token not ready.");
            return;
        }
        StartCoroutine(CapturePhoto());
    }

    private IEnumerator CapturePhoto()
    {
        yield return new WaitForEndOfFrame();

        var photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        byte[] imageBytes = photo.EncodeToJPG();
        uploader.StartUpload(imageBytes, "capturedPhoto.jpg");
    }
}
