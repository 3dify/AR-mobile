using UnityEngine;
using System.Collections;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Vuforia;

public class LoadModelFromUrl : MonoBehaviour {

	public GameObject modelTemplate;
	public GameObject loadingUITemplate;
	//private List<string> loading = new List<string>();
	private HashSet<string> loading = new HashSet<string>();

	public void Load(string url){
		StartCoroutine(_Load(url));
	}
	
	IEnumerator _Load(string url) {
		System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
		
		timer.Start();
		string urlHash = CalculateMD5Hash(url);
        Debug.Log (url);
		Debug.Log (urlHash);
        GameObject existingModel = GameObject.Find(urlHash);
        if( existingModel != null || loading.Contains(urlHash) ){
        	yield break;
        }
		loading.Add(urlHash);
            
        // Start a download of the given URL
		WWW www = new WWW(url);
		
		var tracker = GameObject.FindObjectOfType<QCARBehaviour>() as QCARBehaviour;
		//tracker.enabled = false;
		Debug.Log ("Loading "+url);
		
		LoadingProgress progressUI = loadingUITemplate.GetComponent<LoadingProgress>();
		//(Instantiate( loadingUITemplate ) as GameObject ).GetComponent<LoadingProgress>();
		progressUI.gameObject.SetActive( true );
		
		// Wait for download to complete
		while( !www.isDone ){
			progressUI.progress = www.progress;
			yield return null;
		}
		
		progressUI.gameObject.SetActive( false );
		
		// Load and retrieve the AssetBundle
		//AssetBundle bundle = www.assetBundle;
		
		// Load the TextAsset object
		//TextAsset txt = bundle.Load("myBinaryAsText", typeof(TextAsset)) as TextAsset;
		
		// Retrieve the binary data as an array of bytes
		byte[] bytes = www.bytes;
		
		MemoryStream zipFileStream = new MemoryStream(bytes);
		
		GameObject model = Instantiate( modelTemplate ) as GameObject;
		model.name = urlHash.ToLower();
		
		using( ZipInputStream zipStream = new ZipInputStream(zipFileStream) ){
			ZipEntry zipEntry;
			byte[] fileBytes = new byte[1<<20];
            while( ( zipEntry = zipStream.GetNextEntry() )!=null ){
            	
				string fileName = zipEntry.Name;
				string ext = Path.GetExtension(fileName);
				Debug.Log (string.Format("{0} is {1}",fileName,ext.ToLower()));
				
				if( !fileName.Contains("__") && ext.ToLower() == ".bin" ){
					int l=0;
					byte[] modelBytes;
					using ( MemoryStream objStream = new MemoryStream() ) {
						while((l=zipStream.Read(fileBytes,0,fileBytes.Length))>0){
							objStream.Write(fileBytes,0,l);		
							
						}
						objStream.Position = 0;
						modelBytes = objStream.ToArray();
					}
					yield return StartCoroutine(LoadBinaryModel(modelBytes,model));	
				}
			}
        }
        
        /*
        Debug.Log (string.Format("obj is {0} bytes",objStream.Length));
        
		objStream.Position = 0;
        
		if( objStream.Length > 0 ){
		
			var importThread = new System.Threading.Thread(()=>{
		
				Mesh import = ObjImporter.ImportFile( objStream );
				
				GameObject model = Instantiate( modelTemplate ) as GameObject;
				model.name = urlHash.ToLower();
				model.transform.GetChild(0).GetComponent<MeshFilter>().mesh = import;        
			
			});
			
			
		}
		*/
		
		tracker.enabled = true;
    }
    
	private IEnumerator LoadBinaryModel(byte[] modelData,GameObject model){
    	Mesh outputMesh = MeshSerializer.ReadMesh( modelData );	
		model.transform.GetChild(0).GetComponent<MeshFilter>().mesh = outputMesh;
		yield return null;
    }
    
	private static string CalculateMD5Hash(string input)
	{
		// step 1, calculate MD5 hash from input
		MD5 md5 = System.Security.Cryptography.MD5.Create();
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		byte[] hash = md5.ComputeHash(inputBytes);
		
		// step 2, convert byte array to hex string
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
    
	private static void Report(string msg, System.Diagnostics.Stopwatch timer){
    	Debug.Log(string.Format("{0}\t {1}",msg,timer.Elapsed.TotalSeconds));
    }
}
