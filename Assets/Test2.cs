using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test2 : MonoBehaviour {

    public Inject<float> inj1;
    public Inject<float> inj2;
    public Inject<float> inj3;

    void Start() {
        Variable<float> a = 0.0f;
        Variable<float> b = 1.0f;
        Variable<float> c = a + a + a + a;
        Parser.Parse(c.symbol);
    }
}
