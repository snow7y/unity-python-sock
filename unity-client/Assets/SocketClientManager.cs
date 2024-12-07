using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketClientManager : MonoBehaviour
{
    private SimpleSocketClient client;
    void Start()
    {
        client = new SimpleSocketClient();
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
