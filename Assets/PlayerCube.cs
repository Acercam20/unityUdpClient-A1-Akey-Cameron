using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{
    public string ClientID;
    private GameObject networkManager;
    public float r, g, b;
    public Vector3 movementVector;

    // Start is called before the first frame update
    void Start()
    {
        networkManager = GameObject.Find("NetworkMan");
        InvokeRepeating("UpdatePosition", 1, 0.03f);
    }

    public void DestroyCube()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        //if (nw.GetComponent<NetworkMan>().myID == this.ClientID)
        {
            movementVector = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical")) * 2000 * Time.deltaTime;
            gameObject.GetComponent<Rigidbody>().velocity = movementVector;
        }
        gameObject.GetComponent<Renderer>().material.color = new Color(r, g, b); //C#
    }

    public void UpdatePosition()
    {
        networkManager.GetComponent<NetworkMan>().SendPlayerInfo(gameObject.transform.position);
    }
}

