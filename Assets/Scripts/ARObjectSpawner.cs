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
		
		GameObject model = GameObject.Find( targetSearchResult.TargetName );
		
		if( model != null ){
			ImageTargetBehaviour imageTargetBehaviour = 
				(ImageTargetBehaviour)tracker.TargetFinder.EnableTracking(targetSearchResult,model);       
        }
    }
	
	public void OnStateChanged(bool scanning){
		Debug.Log(scanning);
	}
	
	public void OnUpdateError(TargetFinder.UpdateState updateError){
	
	}
}
