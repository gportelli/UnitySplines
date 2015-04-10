using System;
using UnityEditor;
using UnityEngine;

namespace AClockworkBerry.SplinesEditor
{
    public class SplinesGUI
    {
        public static GUIStyle stFoldout
        {
            get
            {
                if (mstFoldout == null)
                {
                    mstFoldout = new GUIStyle(EditorStyles.foldout);
                    mstFoldout.fontStyle = FontStyle.Bold;
                }
                return mstFoldout;
            }
        }
        static GUIStyle mstFoldout;

        public static bool Foldout(ref bool state, string text) { return Foldout(ref state, new GUIContent(text)); }

        public static bool Foldout(ref bool state, GUIContent content)
        {
            Rect r = GUILayoutUtility.GetRect(content, stFoldout);
            int lvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = Mathf.Max(0, EditorGUI.indentLevel - 1);
            r = EditorGUI.IndentedRect(r);
            r.x += 3;
            state = GUI.Toggle(r, state, content, stFoldout);

            EditorGUI.indentLevel = lvl;

            return state;
        }
    }
}
