using System;
using System.Collections.Generic;
using UnityEngine;

public class PropertyInjector {
    public List<Action<ComputeShader, Dictionary<string, Texture>>> injected;

    public PropertyInjector() {
        this.injected = new List<Action<ComputeShader, Dictionary<string, Texture>>>();
    }


    public void UpdateInjected(ComputeShader shader, Dictionary<string, Texture> textures) {
        foreach (var item in injected) {
            item.Invoke(shader, textures);
        }
    }
}