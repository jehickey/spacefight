using System;
using UnityEngine;

[Serializable]
public class ModuleDefinition
{
    public int id;
    public string name;
    public ParameterDefinition[] parameters;
    public bool breakpoint;

    public ModuleDefinition(ImagingModule module)
    {
        id = module.id;
        name = module.name;
        breakpoint = module.breakpoint;
        parameters = module.parameters.ToArray();
    }

}
