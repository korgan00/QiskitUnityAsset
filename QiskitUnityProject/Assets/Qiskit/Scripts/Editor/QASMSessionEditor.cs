using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qiskit.Example {

    [CustomEditor(typeof(QASMSession))]
    public class QASMSessionEditor : Editor {
        GUIStyle _bodyStyle;


        bool m_Initialized;


        void Init() {
            //if (m_Initialized)
            //    return;
            _bodyStyle = new GUIStyle(EditorStyles.largeLabel) {
                wordWrap = true,
                fontSize = 15
            };

            m_Initialized = true;
        }

        public override void OnInspectorGUI() {
            QASMSession session = (QASMSession)target;
            Init();
            GUILayout.Label("IBMQ Configuration", _bodyStyle);

            session.apiTokenString = EditorGUILayout.TextField("Api Token", session.apiTokenString);
            //EditorGUILayout.Popup
            /*
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
            */
        }

    }
}
