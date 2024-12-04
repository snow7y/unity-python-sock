using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketClientManager : MonoBehaviour
{
    private SocketClient client;
    void Start()
    {
        client = new SocketClient();
        client.StartClient();
    }

    void Update()
    {

    }

    // Update is called once per frame
    void OnApplicationQuit()
    {
        client.CloseClient();
    }
}
