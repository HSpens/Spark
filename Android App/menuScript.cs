using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class menuScript : MonoBehaviour {

    private int counter = 0;
    public Button parkButton;
    public Canvas thisThing;
    public Canvas errorCanvas;
    public Text stuff;
    public string parkingScene;

    private int screenWidth = Screen.width;
    private int screenHeight = Screen.height;
    private float userLat;
    private float userLong;
    private float parkLat; 
    private float parkLong; 
    private float[] distVec;
    private Button[] buttonArr = new Button[6];
    private int[] parkingIndex;
    private string[] hexVal = new string[] { "b9a19d", "8da7a4", +
    "cfd0ca", "705d5f", "8d685f", "dade91" };

    private string url = "website/parking";

    void Start () {
        // Start service before querying location
        Input.location.Start();
        for (int i = 0; i < 6; i++)
        {
            var button = Instantiate(parkButton, Vector3.zero, Quaternion.identity) 
            as Button;
            
            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.SetParent(thisThing.transform);
            button.gameObject.SetActive(false);
            buttonArr[i] = button;
            // #########################################
            // First, set button size to be 20 pixels more than parent canvas 
            // and set height to be 1/7 of screen height
            // Second, set new x-position to be in middle of screen for all 
            // buttons and new y-position to be one after another
            // There are 6 buttons being displayed in total
            rectTransform.sizeDelta = new Vector2(20, (screenHeight - 250) / 6);
            button.transform.position = new Vector3(screenWidth / 2, (5 - i) * 
            (screenHeight - 250) / 6 + (screenHeight - 250) / 12, 0);
            // #########################################
            button.GetComponent<Image>().color = HexToColor(hexVal[i]);
            button.GetComponentInChildren<Text>().text = "..." ;
        }
        
        if (!Input.location.isEnabledByUser)
        {
            stuff.text = "Please enable gps";
        }
        else
        {
            stuff.text = "Searching for position...";
            InvokeRepeating("Poll", 3.0f, 5.0f);            
        }
    }

    void Poll()
    {
        // Start request
        WWW www = new WWW(url);
        // Wait for response
        StartCoroutine(WaitForRequest(www));
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    IEnumerator gpsServices()
    {
        // Wait until service initializes
        int maxWait = 5;
        while (Input.location.status == 
        LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 5 seconds
        if (maxWait < 1)
        {
            stuff.text = "Timed out";
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            stuff.text = "Unable to determine position";
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            stuff.text = "Latitude: " + Input.location.lastData.latitude + 
            ", Longitude: " + Input.location.lastData.longitude;
            userLat = Input.location.lastData.latitude;
            userLong = Input.location.lastData.longitude;
            print("Location: " + Input.location.lastData.latitude + " " + 
            Input.location.lastData.longitude + " " + 
            Input.location.lastData.altitude + 
            " " + Input.location.lastData.horizontalAccuracy + " " + 
            Input.location.lastData.timestamp);
        }
    }

    IEnumerator WaitForRequest(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error == null)
        {
            thisThing.gameObject.SetActive(true);
            errorCanvas.gameObject.SetActive(false);
            Debug.Log("WWW Ok!: " + www.text);
            StartCoroutine(gpsServices());

            // JsonUtility can't parse top-level arrays.
            // Need to have a wrapper class
            string json = "{\"parkings\": " + www.text + "}";
            Parkings Parkings = Parkings.CreateFromJSON(json);
            parkLat = Parkings.parkings[0].lat;
            parkLong = Parkings.parkings[0].lng;
            // The app is only for one node atm...
            // Therefore, only look at first node
            SensorInfo[] sensors = Parkings.parkings[0].sensors;

            Debug.Log(
                sensors[0] + "," +
                sensors[1] + "," +
                sensors[2] + "," +
                sensors[3]
            );

            int free;
            for (int i = 0; i < distVec.Length; i++)
            {
                Button button = buttonArr[i];
                button.transform.position = new Vector3(screenWidth / 2, (5 - i) * 
                (screenHeight - 250) / 6 + (screenHeight - 250) / 12, 0);
                free = 0;
                for (int p = 0; p < Parkings.parkings[i].sensors.Length; p++)
                {
                    if (!Parkings.parkings[i].sensors[p].occupied)
                        free++;
                }
                button.GetComponentInChildren<Text>().text = "Parking at " + 
                Parkings.parkings[i].name + ", Distance: " + (int)distVec[i] + 
                "m, Free: [" + free + "/" + Parkings.parkings[i].sensors.Length + "]";
                AddListener(button, Parkings.parkings[i].name, parkingIndex[i]);                
                button.gameObject.SetActive(true);
            }

            for(int i = distVec.Length; i < buttonArr.Length; i++)
            {
                Button button = buttonArr[i];
                button.GetComponentInChildren<Text>().text = "Test button nr " + ((i - distVec.Length) + 1);
                button.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log("WWW Error: " + www.error);
            thisThing.gameObject.SetActive(false);
            errorCanvas.gameObject.SetActive(true);
        }
    }

    public void AddListener(Button b, string a, int i)
    {
        b.onClick.AddListener(() => ToParking(a, i));
    }

    public void ToParking(string sceneName, int i)
    {
        // Stop service if there is no need to query location updates continuously
        Input.location.Stop();
        PlayerPrefs.SetInt("ParkingIndex", i);
        PlayerPrefs.SetString("ParkingName", sceneName);
        SceneManager.LoadScene(parkingScene);
    }

    private float getDistanceFromLatLonInm(float lat1, float lon1, float lat2, float lon2)
    {
        // Haversine formula by "Chuck" at Stack Overflow (27928)
        int R = 6371; // Radius of the earth in km
        float dLat = deg2rad(lat2 - lat1);  // deg2rad below
        float dLon = deg2rad(lon2 - lon1);
        Debug.Log("dLat: " + dLat);
        Debug.Log("dLon: " + dLon);
        float a =
          Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
          Mathf.Cos(deg2rad(lat1)) * Mathf.Cos(deg2rad(lat2)) *
          Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2)
          ;
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float d = R * c * 1000; // Distance in m
        return d;
    }

    private float deg2rad(float deg)
    {
        return deg * (Mathf.PI / 180);
    }

    Color HexToColor(string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), 
        System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), 
        System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), 
        System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 160);
    }
}