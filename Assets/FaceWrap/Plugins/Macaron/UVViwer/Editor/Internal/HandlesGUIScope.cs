using UnityEngine;
using UnityEditor;

namespace Macaron.UVViewer.Editor.Internal
{
    public class HandlesGUIScope : GUI.Scope
    {
        public HandlesGUIScope()
        {
            Handles.BeginGUI();
        }

        #region Overrides
        protected override void CloseScope()
        {
            Handles.EndGUI();
        }
        #endregion
    }
}
