using UnityEngine;
using System.Collections;
using AClockworkBerry.Splines;

public class Test : MonoBehaviour {
    public Transform walker;
    public Spline path;
    public float duration = 5.0f;

    private float desiredV = 3.6f;

	// Use this for initialization
	void Start () {
        //StartCoroutine(path.WalkDuration(3, 1, 5, walker, SplineWalkerMode.Once, true, delegate() { Debug.Log("Complete!"); }));
        //StartCoroutine(path.WalkDuration(1, 3, 5, walker, SplineWalkerMode.Loop));
        StartCoroutine(path.WalkDuration(0, path.curveCount, 5, walker, SplineWalkerMode.Loop));
        //StartCoroutine(path.WalkDuration(path.curveCount, 0, 5, walker, SplineWalkerMode.Loop));

        lastPos = path.GetPoint(0);
	}

	// Update is called once per frame
	void Update () {
        //CheckVelocity(walker.position);
	}

    private Vector3 lastPos;
    void CheckVelocity(Vector3 pos)
    {
        float v = Vector3.Distance(pos, lastPos) / Time.deltaTime;

        if(Mathf.Abs(v-desiredV) > .2)
            Debug.Log(v + " " + pos);

        lastPos = pos;
    }
}
