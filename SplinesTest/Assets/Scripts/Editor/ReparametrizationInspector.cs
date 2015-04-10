using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Reparametrization))]
public class ReparametrizationInspector : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Reparametrization r = target as Reparametrization;

        EditorGUI.BeginChangeCheck();
        float epsilon = EditorGUILayout.Slider("Epsilon", r.epsilon, 0.0001f, 0.01f, null);
        if (EditorGUI.EndChangeCheck())
            r.epsilon = epsilon;

        EditorGUI.BeginChangeCheck();
        int integrationSteps = EditorGUILayout.IntSlider("Numerical Integration Steps", r.integrationSteps, 10, 100, null);
        if (EditorGUI.EndChangeCheck())
            r.integrationSteps = integrationSteps;

        EditorGUI.BeginChangeCheck();
        float sDistance = EditorGUILayout.Slider("Samples Distance", r.samplesDistance, 0.1f, 10f, null);
        if (EditorGUI.EndChangeCheck())
            r.samplesDistance = sDistance;

        EditorGUI.BeginChangeCheck();
        int nDecorators = EditorGUILayout.IntSlider("Number of decorators", r.nDecorators, 10, 100, null);
        if (EditorGUI.EndChangeCheck())
            r.nDecorators = nDecorators;
    }
       
}
