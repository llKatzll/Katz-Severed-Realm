using System;
using UnityEngine;

public class NoteHitLine : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Hit()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A 눌림");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("S 눌림");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tap"))
            Debug.LogWarning("탭 태그됨");

        if (other.CompareTag("Upper"))
            Debug.LogWarning("어퍼 태그됨");

        if (other.CompareTag("Splash"))
            Debug.LogWarning("스플래시 태그됨");
    }
}
