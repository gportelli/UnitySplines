using AClockworkBerry.Splines;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Subdivision))]
public class SubdivisionInspector : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Subdivision s = target as Subdivision;

        if (GUILayout.Button("Create Subdivision"))
        {
            Spline subdivSpline = new GameObject("SplineSubdivision", typeof(Spline)).GetComponent<Spline>();

            Vector3 [] controlPoints = s.spline.GetSubdivision(s.s0, s.s1);

            subdivSpline.SetControlPointRaw(0, controlPoints[0]);
            subdivSpline.SetControlPointRaw(1, controlPoints[1]);
            subdivSpline.SetControlPointRaw(2, controlPoints[2]);
            subdivSpline.SetControlPointRaw(3, controlPoints[3]);

            subdivSpline.color = Color.green;

            Undo.RegisterCreatedObjectUndo(subdivSpline.gameObject, "Create Subdivision");
        }
    }

}
