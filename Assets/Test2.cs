using Unity.Mathematics;
using UnityEngine;

public class Test2 : MonoBehaviour {

    public Inject<float> inj1;
    public Inject<float> inj2;
    public Inject<float> inj3;

    void Start() {
        //Variable<float> a = 0.0f;
        //Variable<float> b = 1.0f;
        //Variable<float> c = a + a + a + a;
        Variable<float2> d = float2.zero;
        Simplex simplex = new Simplex();
        Warper warper = new Warper();
        warper.noisinator = simplex;


        //Voronoi voronoi = new Voronoi();
        //var output = simplex.Evaluate(d) + voronoi.Evaluate(d);
        //var out2 = output + b;
        var out2 = warper.Warpinate(d);
        TreeContext.Parse(out2);

        Debug.Log(JsonUtility.ToJson(out2));

        // 
        // snoise (p1, c2, c3)
    }
}
