using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageHouseData : MonoBehaviour {

    public GameObject HouseObject { get; private set; }
    public VillageData Village { get; private set; }

    void Awake() {
        //Assign this house object to the object the script is attached to
        HouseObject = this.gameObject;
    }

    public void AssignHouseObject(GameObject house) {
        HouseObject = house;
    }

    public void AssignVillage(VillageData village) {
        Village = village;
    }

    public void DestroySelf() {
        Destroy(this.gameObject);
    }

}
