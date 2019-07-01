using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

namespace Qiskit {

    [CustomEditor(typeof(QiskitReadme))]
    [InitializeOnLoad]
    public class QiskitReadmeEditor : Editor {

        static string kShowedReadmeSessionStateName = "QiskitReadmeEditor.showedReadme";

        static float kSpace = 16f;

        static QiskitReadmeEditor() {
            EditorApplication.delayCall += SelectReadmeAutomatically;
        }

        static void SelectReadmeAutomatically() {
            if (!SessionState.GetBool(kShowedReadmeSessionStateName, false)) {
                QiskitReadme readme = SelectReadme();
                SessionState.SetBool(kShowedReadmeSessionStateName, true);

                if (readme && !readme.loadedLayout) {
                    LoadLayout();
                    readme.loadedLayout = true;
                }
            }
        }

        static void LoadLayout() {
            Assembly assembly = typeof(EditorApplication).Assembly;
            Type windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
            MethodInfo method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { Path.Combine(Application.dataPath, "Qiskit/TutorialInfo/Layout.wlt"), false });
        }

        [MenuItem("Qiskit/Readme")]
        static QiskitReadme SelectReadme() {
            string[] ids = AssetDatabase.FindAssets("t:QiskitReadme");

            if (ids.Length >= 1) {
                UnityEngine.Object readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                Selection.objects = new UnityEngine.Object[] { readmeObject };

                return (QiskitReadme)readmeObject;
            } else {
                Debug.Log("Couldn't find a readme");
                return null;
            }
        }

        protected override void OnHeaderGUI() {
            QiskitReadme readme = (QiskitReadme)target;
            Init();

            float iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

            GUILayout.BeginHorizontal("In BigTitle");
            {
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
                GUILayout.Label(readme.title, TitleStyle);
            }
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI() {
            QiskitReadme readme = (QiskitReadme)target;
            Init();

            foreach (QiskitReadme.Section section in readme.sections) {
                if (!string.IsNullOrEmpty(section.heading)) {
                    GUILayout.Label(section.heading, HeadingStyle);
                }
                if (!string.IsNullOrEmpty(section.text)) {
                    GUILayout.Label(section.text, BodyStyle);
                }
                if (!string.IsNullOrEmpty(section.linkText)) {
                    if (LinkLabel(new GUIContent(section.linkText))) {
                        Application.OpenURL(section.url);
                    }
                }
                GUILayout.Space(kSpace);
            }
        }


        bool m_Initialized;

        GUIStyle LinkStyle { get { return m_LinkStyle; } }
        [SerializeField] GUIStyle m_LinkStyle;

        GUIStyle TitleStyle { get { return m_TitleStyle; } }
        [SerializeField] GUIStyle m_TitleStyle;

        GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
        [SerializeField] GUIStyle m_HeadingStyle;

        GUIStyle BodyStyle { get { return m_BodyStyle; } }
        [SerializeField] GUIStyle m_BodyStyle;

        void Init() {
            if (m_Initialized)
                return;
            m_BodyStyle = new GUIStyle(EditorStyles.label) {
                wordWrap = true,
                fontSize = 14
            };

            m_TitleStyle = new GUIStyle(m_BodyStyle) {
                fontSize = 26
            };

            m_HeadingStyle = new GUIStyle(m_BodyStyle) {
                fontSize = 18
            };

            m_LinkStyle = new GUIStyle(m_BodyStyle) {
                wordWrap = false,
                stretchWidth = false
            };
            // Match selection color which works nicely for both light and dark skins
            m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);

            m_Initialized = true;
        }

        bool LinkLabel(GUIContent label, params GUILayoutOption[] options) {
            Rect position = GUILayoutUtility.GetRect(label, LinkStyle, options);

            Handles.BeginGUI();
            Handles.color = LinkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, LinkStyle);
        }
    }

}