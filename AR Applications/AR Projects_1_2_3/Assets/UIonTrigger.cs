using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIonTrigger : MonoBehaviour
{
   public Canvas canvas;

    private void Start()
    {
        // Hide the canvas at the start
        canvas.gameObject.SetActive(false);
    }

    private void OnMouseDown()
    {
        // Display the canvas when the object is clicked
        canvas.gameObject.SetActive(true);
        Debug.Log("Clicked");
    }
}
