using UnityEngine;
using System.Collections;
using AClockworkBerry.Splines;

public class Reparametrization : MonoBehaviour {
    public Spline spline;
    public GameObject capsule, cube;
    private GameObject pool;

    private GameObject[] objects;

    [HideInInspector, SerializeField]
    private float _epsilon = 0.01f;

    public float epsilon
    {
        set { 
            _epsilon = value;
            if (Application.isPlaying)
            {
                Refresh();
            }
        }
        get { return _epsilon; }
    }

    [SerializeField, HideInInspector]
    private int _integrationSteps = 12;
    public int integrationSteps
    {
        set
        {
            _integrationSteps = value;
            if (Application.isPlaying)
            {
                Refresh();
            }
        }
        get { return _integrationSteps; }
    }

    [SerializeField, HideInInspector]
    private float _samplesDistance = 10;
    public float samplesDistance
    {
        set
        {
            _samplesDistance = value;
            if (Application.isPlaying)
            {
                Refresh();
            }
        }
        get { return _samplesDistance; }
    }

    [SerializeField, HideInInspector]
    private int _nDecorators = 33;
    public int nDecorators
    {
        set
        {
            _nDecorators = value;
            if (Application.isPlaying)
            {
                Refresh();
            }
        }
        get { return _nDecorators; }
    }

    private void Refresh()
    {
        if (objects != null)
        {
            for (int o = 0; o < objects.Length; o++)
                if (objects[o] != null) Destroy(objects[o]);
        }

        Vector3 point;
        int steps = nDecorators;
        objects = new GameObject[steps * 2];

        spline.SetDirty();
        float lastU = 0;

        float[] distances = new float[steps-1];
        float[] errors = new float[steps];

        Bezier.integrationSteps = integrationSteps;
        spline.samplesDistance = samplesDistance;

        for (int i = 0; i < steps; i++)
        {
            float s = i * spline.length / (steps-1);

            float u = spline.GetArcLengthParameter(s, _epsilon);
            point = spline.GetPoint(u); objects[i * 2] = Instantiate(cube, point, Quaternion.identity) as GameObject;
            objects[i * 2].transform.parent = pool.transform;

            float w = spline.GetArcLengthParameterApproximate(s);
            point = spline.GetPoint(w); objects[i * 2 + 1] = Instantiate(capsule, point, Quaternion.identity) as GameObject;
            objects[i * 2 + 1].transform.parent = pool.transform;

            if (i != 0) distances[i-1] = spline.GetArcLength(lastU, u);
            errors[i] = Mathf.Abs(s - spline.GetArcLength(0, w));
            lastU = u;
        }

        Debug.Log("spline.length: " + spline.length);

        Debug.Log("cube steps");
        GetStats(distances);
        Debug.Log("approx errors");
        GetStatsErrors(errors, 0.05f);
    }

    void GetStats(float[] values)
    {
        float mean = 0;
        for (int t = 0; t < values.Length; t++)
        {
            //Debug.Log(t + ". " + distances[t]);
            mean += values[t];
        }
        mean /= values.Length;

        string debug = "Mean = " + mean + " ";

        for (int j = 0; j < values.Length; j++)
        {
            float var = Mathf.Abs(values[j] - mean) / mean * 100;
            if (var > 1) debug += "[" + (j + 1) + ". " + values[j] + " (" + var + "%)" + "] ";
        }

        Debug.Log(debug);
    }

    void GetStatsErrors(float[] values, float min = 0.1f)
    {
        string debug = "";

        for (int j = 0; j < values.Length; j++)
        {
            if(values[j] > min)
                debug += "[" + (j + 1) + ". " + values[j] + "] ";
        }

        Debug.Log(debug);
    }

	// Use this for initialization
	void Start () {
        pool = GameObject.FindGameObjectWithTag("Pool");
        Refresh();
	}
	
	// Update is called once per frame
	void Update () {
        
	}
}
