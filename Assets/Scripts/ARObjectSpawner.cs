using UnityEngine;
using System.Collections;
using Vuforia;

public class ARObjectSpawner : MonoBehaviour, ICloudRecoEventHandler {

	// Use this for initialization
	void Start () {
		var cloudReco = GetComponent<CloudRecoBehaviour>();
		cloudReco.RegisterEventHandler(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void OnInitError(TargetFinder.InitState initError){
	
	}
	
	public void OnInitialized(){
	
	}
	
	public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult){
		Debug.Log("Target found "+targetSearchResult.TargetName);
		ObjectTracker tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
		ImageTargetBehaviour imageTargetBehaviour = 
			(ImageTargetBehaviour)tracker.TargetFinder.EnableTracking(
				targetSearchResult,GameObject.FindObjectOfType<ImageTargetBehaviour>().gameObject);
	}
	
	public void OnStateChanged(bool scanning){
		Debug.Log(scanning);
	}
	
	public void OnUpdateError(TargetFinder.UpdateState updateError){
	
	}
}
