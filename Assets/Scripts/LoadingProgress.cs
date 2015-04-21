using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LoadingProgress : MonoBehaviour {

	public Slider progressSlider;

	public float progress;

	void Start () {
		
		/*
		Canvas canvas = GameObject.FindObjectOfType<Canvas>();
		
		
		transform.SetParent( canvas.transform, false );
		RectTransform progressUIPos = this.GetComponent<RectTransform>();
		
		this.transform.localScale = Vector3.one;            
		progressUIPos.sizeDelta = Vector2.zero;
		progressUIPos.anchoredPosition = Vector2.zero;
		progressUIPos.anchorMin = new Vector2(0,0);
		progressUIPos.anchorMax = new Vector2(1,1);
		*/
		/*		
		
		
		progressUIPos.localPosition = new Vector2(0,0);
        progressUIPos.anchoredPosition = new Vector2(0,-50);
        progressUIPos.position = new Vector2(0,0);
		progressUIPos.pivot = new Vector2(0,0);
		*/
	}
	
	// Update is called once per frame
	void Update () {
		progressSlider.value = progress;
	}
}
