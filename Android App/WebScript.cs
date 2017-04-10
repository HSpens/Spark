using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WebScript : MonoBehaviour {

	private string url = "website/parking";
    private bool[] prevStatuses = { true, true, true, true };
    public Text uiText;
    private int free;
    private int parkingSpaces;
    private int sensorNumber;
    Color green = Color.green;
    Color occupiedRed = new Color(0.6f, 0f, 0f);
    Color faultyYellow = new Color(1f, 1f, 0f);
    Color newColor;
    public int lotNumber;

    void Start () {
        // Start polling
        lotNumber = PlayerPrefs.GetInt("ParkingIndex");
        WWW www = new WWW(url);
        StartCoroutine(StartShow(www));
		InvokeRepeating ("Poll", 2f, 5.0f);
        uiText.text = "Free [ .. ]";
    }

	void Poll() {		
		// Start request
		WWW www = new WWW(url);
		// Wait for response
		StartCoroutine(WaitForRequest(www));
	}

	// Function for changing color depending on whether occupied or not
	void ChangeColor(GameObject sensor, bool occupied, bool faulty) {       
		if (occupied) {
            newColor = occupiedRed;
            sensor.GetComponent<Renderer>().materials[0].SetColor("_Color", newColor);
        } else {
            newColor = green;
            sensor.GetComponent<Renderer>().materials[0].SetColor("_Color", newColor);
        }
        if (faulty)
        {
            newColor = faultyYellow;
            sensor.GetComponent<Renderer>().materials[0].SetColor("_Color", newColor);
        }        	  
	}

    IEnumerator WaitForRequest(WWW www)
	{
		yield return www;

		// check for errors
		if (www.error == null)
		{
			Debug.Log("WWW Ok!: " + www.text);

			// JsonUtility can't parse top-level arrays.
			// Need to have a wrapper class
			string json = "{\"parkings\": " + www.text + "}";
			Parkings parkingsInfo = Parkings.CreateFromJSON (json);
            ParkingInfo[] parkings = parkingsInfo.parkings;
			// The app is only for one node atm...
			// Therefore, only look at first node
			SensorInfo[] sensors = parkings[lotNumber].sensors;

            free = 0;
            parkingSpaces = 0;
            GameObject sensorObject;
			foreach (SensorInfo sensor in sensors) {
                sensorNumber = sensor.id + 4 * (sensor.node - 1);
                sensorObject = GameObject.Find("Sensor " + sensorNumber.ToString());
                ChangeColor(sensorObject, sensor.occupied, sensor.faulty);
                if (!sensor.occupied)
                {
                    free++;
                }
                parkingSpaces++;
                // Save statuses
            }
            uiText.text = "Free spaces [ " + free.ToString() + "/" + parkingSpaces.ToString() + " ]";
        } else {
			Debug.Log("WWW Error: "+ www.error);
		}    
	}

    IEnumerator StartShow(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Ok!: " + www.text);

            // JsonUtility can't parse top-level arrays.
            // Need to have a wrapper class
            string json = "{\"parkings\": " + www.text + "}";
            Parkings parkingsInfo = Parkings.CreateFromJSON(json);
            ParkingInfo[] parkings = parkingsInfo.parkings;
            // The app is only for one node atm...
            // Therefore, only look at first node
            SensorInfo[] sensors = parkings[lotNumber].sensors;
            GameObject sensorObject;
            foreach (SensorInfo sensor in sensors)
            {
                sensorNumber = sensor.id + 4 * (sensor.node - 1);
                sensorObject = GameObject.Find("Sensor " + sensorNumber.ToString());
                sensorObject.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        else
        {
            Debug.Log("WWW Error: " + www.error);
        }
    }
}