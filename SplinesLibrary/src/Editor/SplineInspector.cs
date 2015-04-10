using UnityEditor;
using UnityEngine;
using AClockworkBerry.Splines;

namespace AClockworkBerry.SplinesEditor
{
    [CustomEditor(typeof(Spline)), CanEditMultipleObjects]
    public class SplineInspector : Editor
    {
        private const int STEPS_PER_CURVE = 4;
        private const float VELOCITIES_SCALE = 0.4f;
        private const float ACCELERATION_SCALE = 0.1f;
        private const float HANDLE_SIZE = 0.04f;
        private const float PICK_SIZE = 0.06f;

        private static Color[] _modeColors = {
	        Color.white,    // corner
	        Color.yellow,   // aligned
	        Color.green     // smooth
        };

        private Spline _spline;
        private Transform _handleTransform;
        private Quaternion _handleRotation;
        private int _selectedIndex = -1;

        private bool [] foldouts = new bool[1] { true };

        public override void OnInspectorGUI()
        {
            _spline = target as Spline;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showGizmo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showNumbers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showVelocities"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showAccelerations"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("color"));
            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            bool loop = EditorGUILayout.Toggle("Loop", _spline.loop);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(serializedObject.targetObjects, "Toggle Loop");

                foreach (Object o in serializedObject.targetObjects)
                {
                    Spline s = o as Spline;

                    s.loop = loop;
                    EditorUtility.SetDirty(s);
                }
            }

            if (_selectedIndex >= 0 && _selectedIndex <= _spline.curveCount * 3)
            {
                _DrawSelectedPointInspector();
            }

            if (GUILayout.Button("Add Point"))
            {
                Undo.RecordObject(_spline, "Add Point");
                _spline.AddPoint(_selectedIndex);
                EditorUtility.SetDirty(_spline);
            }

            if (_spline.curveCount >= 2 && (_selectedIndex == 0 || _selectedIndex % 3 == 0))
            {
                if (GUILayout.Button("Delete Point"))
                {
                    Undo.RecordObject(_spline, "Delete Point");
                    _spline.DeletePoint(_selectedIndex);
                    _selectedIndex = -1;
                    EditorUtility.SetDirty(_spline);
                }
            }

            if (SplinesGUI.Foldout(ref foldouts[0], "Spline Info"))
            {
                EditorGUILayout.LabelField("Total Length: " + _spline.length);
            }
        }

        private void _DrawSelectedPointInspector()
        {
            GUILayout.Label("Selected Point");
            EditorGUI.BeginChangeCheck();
            Vector3 point = EditorGUILayout.Vector3Field("Position", _spline.GetControlPoint(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Move Point");
                EditorUtility.SetDirty(_spline);
                _spline.SetControlPoint(_selectedIndex, point);
            }
            EditorGUI.BeginChangeCheck();
            BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", _spline.GetControlPointMode(_selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, "Change Point Mode");
                _spline.SetControlPointMode(_selectedIndex, mode);
                EditorUtility.SetDirty(_spline);
            }
        }

        void OnSceneGUI()
        {
            _spline = target as Spline;

            _handleTransform = _spline.transform;
            _handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            _handleTransform.rotation : Quaternion.identity;

            Vector3 p0 = _ShowPoint(0);

            for (int i = 1; i < _spline.curveCount * 3; i += 3)
            {
                Vector3 p1 = _ShowPoint(i);
                Vector3 p2 = _ShowPoint(i + 1);
                Vector3 p3 = _ShowPoint(i + 2);

                Handles.color = Color.gray;
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p2, p3);

                //Handles.DrawBezier(p0, p3, p1, p2, _spline.color, null, 2f);
                p0 = p3;
            }

            if (_spline.showVelocities) _ShowVelocities();
            if (_spline.showAccelerations) _ShowAccelerations();
        }

        private void _ShowVelocities()
        {
            Handles.color = Color.green;
            Vector3 point;

            for (int c = 0; c < _spline.curveCount; c++)
                for (int i = 0; i < STEPS_PER_CURVE; i++)
                {
                    point = _spline.GetPoint(c, c + 1, i / (float)STEPS_PER_CURVE);
                    Handles.DrawLine(point, point + _spline.GetVelocity(c, c + 1, i / (float)STEPS_PER_CURVE) * VELOCITIES_SCALE);
                }

            point = _spline.GetPoint(1f);
            Handles.DrawLine(point, point + _spline.GetVelocity(1f) * VELOCITIES_SCALE);
        }

        private void _ShowAccelerations()
        {
            Handles.color = Color.yellow;
            Vector3 point;

            for (int c = 0; c < _spline.curveCount; c++)
                for (int i = 0; i <= STEPS_PER_CURVE; i++)
                {
                    point = _spline.GetPoint(c, c + 1, i / (float)STEPS_PER_CURVE);
                    Handles.DrawLine(point, point + _spline.GetAcceleration(c, c + 1, i / (float)STEPS_PER_CURVE) * ACCELERATION_SCALE);
                }
        }

        private Vector3 _ShowPoint(int index)
        {
            Vector3 point = _handleTransform.TransformPoint(_spline.GetControlPoint(index));
            float size = HandleUtility.GetHandleSize(point);

            if (index % 3 == 0 && _spline.showNumbers)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = _spline.color;
                style.fontSize = 20;
                Handles.Label(point + Vector3.right * size * 0.1f, "" + (index / 3), style);
            }

            if (index == 0)
            {
                size *= 2f;
            }

            Handles.color = _modeColors[(int)_spline.GetControlPointMode(index)];

            if (index % 3 != 0)
                Handles.color = Color.grey;

            if (Handles.Button(point, _handleRotation, size * HANDLE_SIZE, size * PICK_SIZE, Handles.DotCap))
            {
                _selectedIndex = index;
                Repaint();
            }
            if (_selectedIndex == index)
            {
                EditorGUI.BeginChangeCheck();
                point = Handles.PositionHandle(point, _handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_spline, "Move Point");
                    EditorUtility.SetDirty(_spline);
                    _spline.SetControlPoint(index, _handleTransform.InverseTransformPoint(point));
                }
            }
            return point;
        }
    }
}