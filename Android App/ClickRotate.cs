using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ClickRotate : MonoBehaviour
{
    private bool pressed = false;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (pressed)
            rotateScene();
    } 

    public void rotateScene()
    {
        transform.Rotate(new Vector3(0f, -80f, 0f) * Time.deltaTime);
    }

    public void pressedDown()
    {
        pressed = true;
    }

    public void releasedUp()
    {
        pressed = false;
    }
}
