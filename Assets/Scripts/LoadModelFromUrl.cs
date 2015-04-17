using UnityEngine;
using System.Collections;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

public class LoadModelFromUrl : MonoBehaviour {

	public void Load(string url){
		StartCoroutine(_Load(url));
	}
	
	IEnumerator _Load(string url) {
		// Start a download of the given URL
		WWW www = WWW.LoadFromCacheOrDownload (url, 1);
		
		// Wait for download to complete
		yield return www;
		
		// Load and retrieve the AssetBundle
		AssetBundle bundle = www.assetBundle;
		
		// Load the TextAsset object
		//TextAsset txt = bundle.Load("myBinaryAsText", typeof(TextAsset)) as TextAsset;
		
		// Retrieve the binary data as an array of bytes
		byte[] bytes = www.bytes;
		
		MemoryStream fileStream = new MemoryStream(bytes);
		using( ZipInputStream zipStream = new ZipInputStream(fileStream) ){
			ZipEntry zipEntry;
			while( ( zipEntry = zipStream.GetNextEntry() )!=null ){
				string fileName      = Path.GetFileName(zipEntry.Name);
				string ext = Path.GetExtension(fileName);
				
            }
        }
        
        
        
    }
}
