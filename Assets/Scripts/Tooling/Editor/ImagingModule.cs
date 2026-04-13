using UnityEngine;

public class ImagingModule
{
    public string Name;


    //ui info
    public Rect rect;
    public bool isSelected;

    public ImagingModule(string name)
    {
        Name = name;
    }

    public ImagingModule(ImagingModule original)
    {
        Name = original.Name;
        rect = Rect.zero;
        isSelected = false;
    }

}
