using UnityEngine;

[System.Serializable]
public class JumpLocation
{
    public bool Available;          //is this jump available for use right now?
    public Body Target;             //The body that it goes to
    public string Name;             //The name of the jump location (usually a body name)
    public float Distance;          //how far is it?  Mostly used for UI and timing
    public Vector3 Coordinate;      //coordinates where it will go to (relative to body)
    public bool Selected;           //is the player selecting it?
    public RectTransform reticle;   //visual reticle used to show it in HUD

    public JumpLocation(string name, float distance, bool available)
    {
        Name = name;
        Distance = distance;
        Available = available;
    }

    public static bool operator true(JumpLocation j)
    {
        if (j == null) return false;
        return j.Available;
    }

    public static bool operator false(JumpLocation j)
    {
        if (j == null) return true;
        return !j.Available;
    }


    public static bool operator !(JumpLocation j)
    {
        if (j == null) return true;
        return !j.Available;
    }


}
