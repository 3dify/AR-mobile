using System;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Collections;
using System.Diagnostics;

public class MeshSerializer
{
	// A simple mesh saving/loading functionality.
	// This is a utility script, you don't need to add it to any objects.
	// See SaveMeshForWeb and LoadMeshFromWeb for example of use.
	//
	// Uses a custom binary format:
	//
	//    2 bytes vertex count
	//    2 bytes triangle count
	//    1 bytes vertex format (bits: 0=vertices, 1=normals, 2=tangents, 3=uvs)
	//
	//    After that come vertex component arrays, each optional except for positions.
	//    Which ones are present depends on vertex format:
	//        Positions
	//            Bounding box is before the array (xmin,xmax,ymin,ymax,zmin,zmax)
	//            Then each vertex component is 2 byte unsigned short, interpolated between the bound axis
	//        Normals
	//            One byte per component
	//        Tangents
	//            One byte per component
	//        UVs (8 bytes/vertex - 2 floats)
	//            Bounding box is before the array (xmin,xmax,ymin,ymax)
	//            Then each UV component is 2 byte unsigned short, interpolated between the bound axis
	//
	//    Finally the triangle indices array: 6 bytes per triangle (3 unsigned short indices)
	// Reads mesh from an array of bytes. [old: Can return null if the bytes seem invalid.]
	
	
	public Thread processThread;
	public byte[] modelData;
	private bool isDone;
	private int totalSteps = 0;
	private int totalStepsCompleted = 0;
	private MeshData meshData;
	public void ReadMeshASync(byte[] bytes)
	{
		this.modelData = bytes;
		isDone = false;
		meshData = new MeshData();
		processThread = new Thread(_ReadMeshThread);
		processThread.Start();
	}
	
	private void _ReadMeshThread(){
		try {
			ReadMesh( modelData, true, ref this.totalSteps, ref this.totalStepsCompleted, meshData );
		}catch(Exception e){
			UnityEngine.Debug.LogError(e.ToString());
		}
		isDone = true;
	}
	
	public bool IsDone {
		get {
			return isDone;
		}
	}
	
	public float Progress {
		get {
			return 1f*this.totalStepsCompleted/this.totalSteps;
		}	
	}
	
	public Mesh CompletedModel {
		get {
			Mesh mesh = new Mesh();
			mesh.vertices = meshData.vertices;
			mesh.normals = meshData.normals;
			mesh.tangents = meshData.tangents;
			mesh.uv = meshData.uv;
			mesh.triangles = meshData.triangles;
			return mesh;
		}
	}
	
	public static Mesh ReadMesh(byte[] bytes){
		var meshData = new MeshData();
		var mesh = new Mesh();
		int a = 0;
		int b = 0;
		ReadMesh(bytes,false,ref a,ref b, meshData);
		mesh.vertices = meshData.vertices;
		mesh.normals = meshData.normals;
		mesh.tangents = meshData.tangents;
		mesh.uv = meshData.uv;
		mesh.triangles = meshData.triangles;
		return mesh;
	}
	
	private class MeshData {
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector4[] tangents;
		public Vector2[] uv;
		public int[] triangles;
		
	}
		
	private static void ReadMesh(byte[] bytes, bool aSync, ref int totalSteps, ref int totalStepsCompleted, MeshData mesh)
	{
		IEnumerator steps;
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		int sleep = 30;
		int timeout = 5;
		
		if (bytes == null || bytes.Length < 5)
			throw new Exception("Invalid mesh file!");
		
		var buf = new BinaryReader(new MemoryStream(bytes));
		
		// read header
		var vertCount = buf.ReadUInt16();
		var triCount = buf.ReadUInt16();
		var format = buf.ReadByte();
		
		totalStepsCompleted = 0;
		totalSteps = (int)vertCount + (((format & 2) != 0)?(int)vertCount:0) + (((format & 4) != 0)?(int)vertCount:0) + (((format & 8) != 0)?(int)vertCount:0) + (int)triCount;
		
		UnityEngine.Debug.Log (string.Format("vertCount {0}",vertCount));
		
		// sanity check
		if (vertCount < 0 || vertCount > 64000)
			throw new Exception("Invalid vertex count in the mesh data!");
		if (triCount < 0 || triCount > 64000)
			throw new Exception("Invalid triangle count in the mesh data!");
		if (format < 1 || (format & 1) == 0 || format > 15)
			throw new Exception("Invalid vertex format in the mesh data!");
		
		
		// positions
		var verts = new Vector3[vertCount];
		steps = ReadVector3Array16Bit(verts, buf);
		while(steps.MoveNext()){
			if(aSync && stopwatch.ElapsedMilliseconds>timeout){
				Thread.Sleep(sleep);
				stopwatch.Reset();
			}
			
			totalStepsCompleted++;
		}
		mesh.vertices = verts;
		
		if ((format & 2) != 0) // have normals
		{
			var normals = new Vector3[vertCount];
			
			steps = ReadVector3ArrayBytes(normals, buf);
			while(steps.MoveNext()){
				if(aSync && stopwatch.ElapsedMilliseconds>timeout){
					Thread.Sleep(sleep);
					stopwatch.Reset();
				}
				totalStepsCompleted++;
				
			} 
			mesh.normals = normals;
		}
		
		if ((format & 4) != 0) // have tangents
		{
			var tangents = new Vector4[vertCount];
			steps = ReadVector4ArrayBytes(tangents, buf);
			while(steps.MoveNext()){
				if(aSync && stopwatch.ElapsedMilliseconds>timeout){
					Thread.Sleep(sleep);
					stopwatch.Reset();
				}
				totalStepsCompleted++;
				
			}
			mesh.tangents = tangents;
		}
		
		if ((format & 8) != 0) // have UVs
		{
			var uvs = new Vector2[vertCount];
			steps = ReadVector2Array16Bit(uvs, buf);
			while(steps.MoveNext()){
				if(aSync && stopwatch.ElapsedMilliseconds>timeout){
					Thread.Sleep(sleep);
					stopwatch.Reset();
				}
				totalStepsCompleted++;
				
			}
			mesh.uv = uvs;
		}
		
		// triangle indices
		var tris = new int[triCount * 3];
		steps = ReadTris(tris,triCount,buf);
		while(steps.MoveNext()){
			if(aSync && stopwatch.ElapsedMilliseconds>timeout){
				Thread.Sleep(sleep);
				stopwatch.Reset();
			}
			totalStepsCompleted++;
			
		}
		mesh.triangles = tris;
		
		buf.Close();
		
	}
	
	static IEnumerator ReadVector3Array16Bit(Vector3[] arr, BinaryReader buf)
	{
		var n = arr.Length;
		if (n == 0)
			yield break;
		
		// read bounding box
		Vector3 bmin;
		Vector3 bmax;
		bmin.x = buf.ReadSingle();
		bmax.x = buf.ReadSingle();
		bmin.y = buf.ReadSingle();
		bmax.y = buf.ReadSingle();
		bmin.z = buf.ReadSingle();
		bmax.z = buf.ReadSingle();
		
		// decode vectors as 16 bit integer components between the bounds
				
		for (var i = 0; i < n; ++i)
		{
			ushort ix = buf.ReadUInt16();
			ushort iy = buf.ReadUInt16();
			ushort iz = buf.ReadUInt16();
			float xx = ix / 65535.0f * (bmax.x - bmin.x) + bmin.x;
			float yy = iy / 65535.0f * (bmax.y - bmin.y) + bmin.y;
			float zz = iz / 65535.0f * (bmax.z - bmin.z) + bmin.z;
			arr[i] = new Vector3(xx, yy, zz);
			
			yield return null;
		}
	}
	
	static IEnumerator ReadTris(int[] tris, int triCount, BinaryReader buf){
		if( triCount == 0 )
			yield break;
	
		for (int i = 0; i < triCount; ++i)
		{
			tris[i * 3 + 0] = buf.ReadUInt16();
			tris[i * 3 + 1] = buf.ReadUInt16();
			tris[i * 3 + 2] = buf.ReadUInt16();
			yield return null;
		}
	}
	
	static void WriteVector3Array16Bit(Vector3[] arr, BinaryWriter buf)
	{
		if (arr.Length == 0)
			return;
		
		// calculate bounding box of the array
		var bounds = new Bounds(arr[0], new Vector3(0.001f, 0.001f, 0.001f));
		foreach (var v in arr)
			bounds.Encapsulate(v);
		
		// write bounds to stream
		var bmin = bounds.min;
		var bmax = bounds.max;
		buf.Write(bmin.x);
		buf.Write(bmax.x);
		buf.Write(bmin.y);
		buf.Write(bmax.y);
		buf.Write(bmin.z);
		buf.Write(bmax.z);
		
		// encode vectors as 16 bit integer components between the bounds
		foreach (var v in arr)
		{
			var xx = Mathf.Clamp((v.x - bmin.x) / (bmax.x - bmin.x) * 65535.0f, 0.0f, 65535.0f);
			var yy = Mathf.Clamp((v.y - bmin.y) / (bmax.y - bmin.y) * 65535.0f, 0.0f, 65535.0f);
			var zz = Mathf.Clamp((v.z - bmin.z) / (bmax.z - bmin.z) * 65535.0f, 0.0f, 65535.0f);
			var ix = (ushort)xx;
			var iy = (ushort)yy;
			var iz = (ushort)zz;
			buf.Write(ix);
			buf.Write(iy);
			buf.Write(iz);
		}
	}
	static IEnumerator ReadVector2Array16Bit(Vector2[] arr, BinaryReader buf)
	{
		var n = arr.Length;
		if (n == 0)
			yield break;
		
		// Read bounding box
		Vector2 bmin;
		Vector2 bmax;
		bmin.x = buf.ReadSingle();
		bmax.x = buf.ReadSingle();
		bmin.y = buf.ReadSingle();
		bmax.y = buf.ReadSingle();
		
		// Decode vectors as 16 bit integer components between the bounds
		for (var i = 0; i < n; ++i)
		{
			ushort ix = buf.ReadUInt16();
			ushort iy = buf.ReadUInt16();
			float xx = ix / 65535.0f * (bmax.x - bmin.x) + bmin.x;
			float yy = iy / 65535.0f * (bmax.y - bmin.y) + bmin.y;
			arr[i] = new Vector2(xx, yy);
			yield return null;
		}
	}
	static void WriteVector2Array16Bit(Vector2[] arr, BinaryWriter buf)
	{
		if (arr.Length == 0)
			return;
		
		// Calculate bounding box of the array
		Vector2 bmin = arr[0] - new Vector2(0.001f, 0.001f);
		Vector2 bmax = arr[0] + new Vector2(0.001f, 0.001f);
		foreach (var v in arr)
		{
			bmin.x = Mathf.Min(bmin.x, v.x);
			bmin.y = Mathf.Min(bmin.y, v.y);
			bmax.x = Mathf.Max(bmax.x, v.x);
			bmax.y = Mathf.Max(bmax.y, v.y);
		}
		
		// Write bounds to stream
		buf.Write(bmin.x);
		buf.Write(bmax.x);
		buf.Write(bmin.y);
		buf.Write(bmax.y);
		
		// Encode vectors as 16 bit integer components between the bounds
		foreach (var v in arr)
		{
			var xx = (v.x - bmin.x) / (bmax.x - bmin.x) * 65535.0f;
			var yy = (v.y - bmin.y) / (bmax.y - bmin.y) * 65535.0f;
			var ix = (ushort)xx;
			var iy = (ushort)yy;
			buf.Write(ix);
			buf.Write(iy);
			
		}
	}
	
	static IEnumerator ReadVector3ArrayBytes(Vector3[] arr, BinaryReader buf)
	{
		// decode vectors as 8 bit integers components in -1.0f .. 1.0f range
		var n = arr.Length;
		for (var i = 0; i < n; ++i)
		{
			byte ix = buf.ReadByte();
			byte iy = buf.ReadByte();
			byte iz = buf.ReadByte();
			float xx = (ix - 128.0f) / 127.0f;
			float yy = (iy - 128.0f) / 127.0f;
			float zz = (iz - 128.0f) / 127.0f;
			arr[i] = new Vector3(xx, yy, zz);
			yield return null;
		}
	}
	static void WriteVector3ArrayBytes(Vector3[] arr, BinaryWriter buf)
	{
		// encode vectors as 8 bit integers components in -1.0f .. 1.0f range
		foreach (var v in arr)
		{
			var ix = (byte)Mathf.Clamp(v.x * 127.0f + 128.0f, 0.0f, 255.0f);
			var iy = (byte)Mathf.Clamp(v.y * 127.0f + 128.0f, 0.0f, 255.0f);
			var iz = (byte)Mathf.Clamp(v.z * 127.0f + 128.0f, 0.0f, 255.0f);
			buf.Write(ix);
			buf.Write(iy);
			buf.Write(iz);
		}
	}
	
	static IEnumerator ReadVector4ArrayBytes(Vector4[] arr, BinaryReader buf)
	{
		// Decode vectors as 8 bit integers components in -1.0f .. 1.0f range
		var n = arr.Length;
		for (var i = 0; i < n; ++i)
		{
			byte ix = buf.ReadByte();
			byte iy = buf.ReadByte();
			byte iz = buf.ReadByte();
			byte iw = buf.ReadByte();
			float xx = (ix - 128.0f) / 127.0f;
			float yy = (iy - 128.0f) / 127.0f;
			float zz = (iz - 128.0f) / 127.0f;
			float ww = (iw - 128.0f) / 127.0f;
			arr[i] = new Vector4(xx, yy, zz, ww);
			yield return null;
		}
	}
	static void WriteVector4ArrayBytes(Vector4[] arr, BinaryWriter buf)
    {
        // Encode vectors as 8 bit integers components in -1.0f .. 1.0f range
        foreach (var v in arr)
        {
            var ix = (byte)Mathf.Clamp(v.x * 127.0f + 128.0f, 0.0f, 255.0f);
            var iy = (byte)Mathf.Clamp(v.y * 127.0f + 128.0f, 0.0f, 255.0f);
            var iz = (byte)Mathf.Clamp(v.z * 127.0f + 128.0f, 0.0f, 255.0f);
            var iw = (byte)Mathf.Clamp(v.w * 127.0f + 128.0f, 0.0f, 255.0f);
            buf.Write(ix);
            buf.Write(iy);
            buf.Write(iz);
            buf.Write(iw);
        }
    }
    
    // Writes mesh to an array of bytes.
    public static byte[] WriteMesh(Mesh mesh, bool saveTangents)
    {
        if (!mesh)
            throw new Exception("No mesh given!");
        
        var verts = mesh.vertices;
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        var uvs = mesh.uv;
        var tris = mesh.triangles;
        
        // figure out vertex format
        byte format = 1;
        if (normals.Length > 0)
            format |= 2;
        if (saveTangents && tangents.Length > 0)
            format |= 4;
        if (uvs.Length > 0)
            format |= 8;
        
        var stream = new MemoryStream();
        var buf = new BinaryWriter(stream);
        
        // write header
        var vertCount = (ushort)verts.Length;
        var triCount = (ushort)(tris.Length / 3);
        buf.Write(vertCount);
        buf.Write(triCount);
        buf.Write(format);
        // vertex components
        WriteVector3Array16Bit(verts, buf);
        WriteVector3ArrayBytes(normals, buf);
        if (saveTangents)
            WriteVector4ArrayBytes(tangents, buf);
        WriteVector2Array16Bit(uvs, buf);
        // triangle indices
        foreach (var idx in tris)
        {
            var idx16 = (ushort)idx;
            buf.Write(idx16);
        }
        buf.Close();
        
        return stream.ToArray();
    }
}