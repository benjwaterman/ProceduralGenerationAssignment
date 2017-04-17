using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class HouseData : ObjectData {

    [Header("Village Options")]
    public PrefabData VillageCenterPrefab;
    public float MaxDistanceBetweenConnectionPoints = 300;
    public int MaxNumberOfConnectionsPerVillage = 2;
    public int ClearAreaRadiusAroundBuildings = 20;
    public float MaxDistanceOfVillage = 150;
    public int MinHousesPerVillage = 5;
    public int MaxHousesPerVillage = 10;
}
