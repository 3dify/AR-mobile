using UnityEngine;
using System.Collections;

using com.google.zxing.qrcode;
using Vuforia;

public class CameraImageAccess : MonoBehaviour {
	
	public StringEvent QRFoundEvent;
	
	private bool isFrameFormatSet;
	
	private Image cameraFeed;
	private string tempText;
	private string qrText;	
	
	void Start () {
		QCARBehaviour qcarBehaviour = GetComponent<QCARBehaviour>();
		
		if (qcarBehaviour) {
			qcarBehaviour.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);
		}
		
		
	}
	
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			Application.Quit();
		}
	}
	
	public void OnTrackablesUpdated () {
		try {
			if(!isFrameFormatSet) {
				isFrameFormatSet = CameraDevice.Instance.SetFrameFormat(Image.PIXEL_FORMAT.GRAYSCALE, true);
			}
			
			cameraFeed = CameraDevice.Instance.GetCameraImage(Image.PIXEL_FORMAT.GRAYSCALE);
			//tempText = new QRCodeReader().decode(cameraFeed.Pixels, cameraFeed.BufferWidth, cameraFeed.BufferHeight).Text;
		}
		catch {
			// Fail detecting QR Code!
		}
		finally {
			if(!string.IsNullOrEmpty(tempText)) {
				qrText = tempText;
				QRFoundEvent.Invoke(tempText);
				tempText = "";
			}
		}
	}
}
