using UnityEngine;
using System.Collections;

// Class for serializing info from a single node
[System.Serializable]
public class NodeInfo
{
    public int id;
    public string name;
    public float lng;
    public float lat;
    public SensorInfo[] sensors;

    public static NodeInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<NodeInfo>(jsonString);
    }
}