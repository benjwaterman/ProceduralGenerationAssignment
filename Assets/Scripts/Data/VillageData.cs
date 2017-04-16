using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageData {

    public List<VillageHouseData> VillageHouses { get; private set; }
    public int VillageSize { get; private set; }
    public GameObject VillageCenter;
    public Vector3 CenterPosition;
    public Vector2 LocalChunkCenterPosition;

    public VillageData() {
        VillageSize = 0;
        VillageHouses = new List<VillageHouseData>();
    }

    public void AddHouse(VillageHouseData house) {
        house.AssignVillage(this);
        VillageHouses.Add(house);
        VillageSize++;
    }

    public void RemoveHouse(VillageHouseData house) {
        VillageSize--;
        VillageHouses.Remove(house);
        MonoBehaviour.Destroy(house.gameObject);
    }

    public void DestroyVillage() {
        for (int i = 0; i < VillageSize; i++) {
            MonoBehaviour.Destroy(VillageHouses[i].gameObject);
        }

        VillageSize = 0;
        VillageHouses.Clear();
    }
}
