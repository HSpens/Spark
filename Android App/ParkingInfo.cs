using UnityEngine;
using System.Collections;

// Class for serializing info from multiple nodes
[System.Serializable]
public class ParkingInfo {
    public int id;
    public string name;
    public float lng;
    public float lat;
	public SensorInfo[] sensors;

	public static ParkingInfo CreateFromJSON(string jsonString)
	{
		return JsonUtility.FromJson<ParkingInfo> (jsonString);
	}
}