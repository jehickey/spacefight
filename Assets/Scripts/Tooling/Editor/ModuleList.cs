using System;
using System.Collections.Generic;

[Serializable]
public class ModuleList
{
    public ModuleDefinition[] modules;

    public ModuleList(List<ImagingModule> list)
    {
        modules = new ModuleDefinition[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            modules[i] = new ModuleDefinition(list[i]);
        }
    }
}