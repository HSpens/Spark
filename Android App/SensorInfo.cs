using UnityEngine;
using System.Collections;

// Class for serializing info from a single node
[System.Serializable]
public class SensorInfo {
	public int id;
	public bool occupied;
	public int node;
    public int parking;
    public bool faulty;
    public float sinceLastUpdate;

    public static SensorInfo CreateFromJSON(string jsonString)
	{
		return JsonUtility.FromJson<SensorInfo> (jsonString);
	}
}