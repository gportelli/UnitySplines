using UnityEngine;
using UnityEditor;
using System.Collections;
using AClockworkBerry.Splines;

namespace AClockworkBerry.SplinesEditor
{

    public class SplinesMenu
    {
        [MenuItem("GameObject/Create Other/Spline", false, 0)]
        static void CreateCurvySpline()
        {
           Spline spl = Spline.Create();
           Selection.activeObject = spl;
           Undo.RegisterCreatedObjectUndo(spl.gameObject, "Create Spline");
        }
    }
}