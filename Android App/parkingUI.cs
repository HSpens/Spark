using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class parkingUI : MonoBehaviour {

    public Text stuff;
	// Use this for initialization
	void Start () {
        stuff.text = "Parking at " + PlayerPrefs.GetString("ParkingName");
	}
	
	// Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void ToMain() {
        SceneManager.LoadScene("MainMenu");
    }
}