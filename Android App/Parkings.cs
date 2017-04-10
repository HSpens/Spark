using UnityEngine;
using System.Collections;

// Class for serializing info from multiple nodes
[System.Serializable]
public class Parkings
{
    public ParkingInfo[] parkings;

    public static Parkings CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Parkings>(jsonString);
    }
}