using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageData {

    public List<VillageHouseData> VillageHouses { get; private set; }
    public int VillageSize { get; private set; }

    public VillageData() {
        VillageSize = 0;
    }

    public void AddHouse(VillageHouseData house) {
        house.AssignVillage(this);
        VillageHouses.Add(house);
        VillageSize++;
    }
}
