using System;
using UnityEngine;


namespace Qiskit {

    public class QiskitReadme : ScriptableObject {
        public Texture2D icon;
        public string title;
        public Section[] sections;
        public bool loadedLayout;

        [Serializable]
        public class Section {
            public string heading, text, linkText, url;
        }
    }
}
