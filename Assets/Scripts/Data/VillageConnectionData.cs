using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageConnectionData : MonoBehaviour {

    public int NumberOfConnections = 0;
    public List<GameObject> Connections = new List<GameObject>();

    void Awake() {
        NumberOfConnections = 0;
    }

    public void AddConnection(GameObject connectionPoint) {
        Connections.Add(connectionPoint);
        NumberOfConnections++;
    }
}
