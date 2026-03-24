using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetData : BodyData
{
    //includes all fields for BodyData, adding a moon list
    //this is to avoid recursion


    public List<BodyData> moons = new List<BodyData>();

}
