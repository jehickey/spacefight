using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;


[System.Serializable]
public struct VRMountEntry
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;

    public VRMountEntry(string setName, Vector3 pos, Quaternion rot)
    {
        name = setName;
        position = pos;
        rotation = rot;
    }
}

[System.Serializable]
public class VRMountEntryList
{
    public List<VRMountEntry> entries = new List<VRMountEntry>();
}

public class VRMountManager : MonoBehaviour
{
    public static VRMountManager I;
    public string customFile = "vrmounts.json";
    public string resourceFile = "Settings/VRMounts";

    private Dictionary<string, VRMountEntry> positions = new Dictionary<string, VRMountEntry>();
    private string customPath;

    private void Awake()
    {
        if (I != null)
        {
            Debug.LogError("Multiple VRMountManager instances detected! Destroying the new one.");
            Destroy(gameObject);
            return;
        }
        I = this;
        customPath = Path.Combine(Application.persistentDataPath, "vrmounts.json");
        //C:\Users\<User>\AppData\LocalLow\<CompanyName>\<ProductName>\vrmounts.json
        Load();
    }



    void Start()
    {
        Load();
    }

    void Update()
    {

    }


    public Vector3 GetPosition(string name, Transform t)
    {
        if (name == string.Empty)
        {
            Debug.Log("VRMountManager: Get called with empty name");
            return t.localPosition;
        }
        //Debug.Log($"Getting position for '{name}'");
        if (positions.TryGetValue(name, out VRMountEntry entry)) return entry.position; //return the position
        Set(name, t);                                                                   //create a new entry
        return t.localPosition;                                                         //return the default position
    }

    public Quaternion GetRotation(string name, Transform t)
    {
        if (name == string.Empty)
        {
            Debug.Log("VRMountManager: Get called with empty name");
            return t.localRotation;
        }
        //Debug.Log($"Getting position for '{name}'");
        if (positions.TryGetValue(name, out VRMountEntry entry)) return entry.rotation; //return the position
        Set(name, t);                                                                   //create a new entry
        return t.localRotation;                                                         //return the default position
    }


    public void Set(string name, Transform t)
    {
        if (name == string.Empty)
        {
            Debug.Log("VRMountManager: Set called with empty name");
            return;
        }
        positions[name] = new VRMountEntry(name, t.localPosition, t.localRotation);
        Debug.Log($"Setting VRMount '{name}'");
        Save();
    }


    private void Save()
    {
        var wrapper = new VRMountEntryList();
        foreach (var kvp in positions)
        {
            //wrapper.entries.Add(new VRMountEntry { name = kvp.Key, position = kvp.Value });
            wrapper.entries.Add(kvp.Value);
        }

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(customPath, json);
    }

    private void Load()
    {
        VRMountEntryList data = null;
        string json = null;
        positions.Clear();

        //try the custom file first
        if (File.Exists(customPath))
        {
            try
            {
                json = File.ReadAllText(customPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read custom VRMounts file: " + e.Message);
            }

            //custom file loaded, parse it
            if (json != null)
            {
                try
                {
                    data = JsonUtility.FromJson<VRMountEntryList>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse custom VRMounts JSON: " + e.Message);
                }
            }
        }

        if (data == null)       //nothing from custom file, revert to default
        {
            TextAsset defaultJson = Resources.Load<TextAsset>(resourceFile);
            if (defaultJson)
            {
                try
                {
                    //parse it
                    data = JsonUtility.FromJson<VRMountEntryList>(defaultJson.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse default VRMounts JSON: " + e.Message);
                    return;
                }
            }
        }
 
        if (data != null)
        {
            //build the list from data
            foreach (VRMountEntry entry in data.entries)
                positions[entry.name] = entry;
        }
    }
}
