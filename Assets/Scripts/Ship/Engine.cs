using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    public float Level = 1.0f;

    public List<EnginePlume> enginePlumes = new List<EnginePlume>();

    private void OnEnable()
    {
        enginePlumes.Clear();
        enginePlumes.AddRange(GetComponentsInChildren<EnginePlume>());
    }

    void Update()
    {
        UpdatePlumes();
    }

    void UpdatePlumes()
    {
        foreach (EnginePlume plume in enginePlumes)
        {
            if (plume)
            {
                plume.Level = Level;
            }
        }
    }
}
