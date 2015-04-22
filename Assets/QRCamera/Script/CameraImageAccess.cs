using UnityEngine;
using System.Collections;
using Vuforia;
using ZXing;
using ZXing.QrCode;


public class CameraImageAccess : MonoBehaviour {
	
	public StringEvent QRFoundEvent;
	
	private bool isFrameFormatSet;
	
	private float lastTime;
	
	private Image cameraFeed;
	private string tempText;
	private string qrText;	
	private BarcodeReader reader = new BarcodeReader();
	void Start () {
		QCARBehaviour qcarBehaviour = GetComponent<QCARBehaviour>();
		
		if (qcarBehaviour) {
			qcarBehaviour.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);
		}
		
		
	}
	
	void Update () {
	}
	
	public void OnTrackablesUpdated () {
		
		float elapsed = Time.time - lastTime;
		
		if(!isFrameFormatSet) {
			isFrameFormatSet = CameraDevice.Instance.SetFrameFormat(Image.PIXEL_FORMAT.GRAYSCALE, true);
        }
        		
		if( elapsed < 0.5 ) return;
		
		lastTime = Time.time;
		
		if( isFrameFormatSet ) {
			cameraFeed = CameraDevice.Instance.GetCameraImage(Image.PIXEL_FORMAT.GRAYSCALE);
			
			if( cameraFeed == null ){
				Debug.Log ("Camera Feed was null");
				return;
			}
			if( cameraFeed.Pixels == null ){
				Debug.Log ("Camera Pixels was null");
				return;
                
			}
			
			//Debug.Log ("Before Decode "+Time.time);
			Result result = null;
			
			result = reader.Decode( cameraFeed.Pixels, cameraFeed.BufferWidth, cameraFeed.BufferHeight,RGBLuminanceSource.BitmapFormat.Gray8);
			if( result != null ){
				
				tempText = result.Text;
				Debug.Log ("QR Decode returned "+tempText);
			}
            
            if(!string.IsNullOrEmpty(tempText)) {
                qrText = tempText;
                QRFoundEvent.Invoke(tempText);
                tempText = "";
			}
			
		}
	}
}
