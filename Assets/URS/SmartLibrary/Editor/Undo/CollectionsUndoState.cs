using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    internal class CollectionsUndoState : ScriptableSingleton<CollectionsUndoState>
    {
        [SerializeField] private int _state;

        public int State
        {
            get { return _state; }
            set { _state = value; }
        }
    }
}
