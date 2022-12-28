using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class ProcessSpinner : Image
    {
        private int _spinIndex;
        private Texture[] _spins = new Texture[12];
        private IVisualElementScheduledItem _updaterItem;

        public bool IsSpinning
        {
            get { return _updaterItem.isActive; }
            set
            {
                if (value && !_updaterItem.isActive)
                    _updaterItem.Resume();
                else if (!value && _updaterItem.isActive)
                    _updaterItem.Pause();
            }
        }
        
        public ProcessSpinner()
        {
            for (int i = 0; i < _spins.Length; i++)
            {
                string id = (i < 10) ? "WaitSpin0" + i : "WaitSpin" + i;
                _spins[i] = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? id : "d_" + id).image;
            }
            
            AddToClassList(LibraryConstants.IconUssClassName);
            _updaterItem = schedule.Execute(UpdateSpinner).Every(100);
        }
        
        private void UpdateSpinner()
        {
            _spinIndex = (_spinIndex + 1) % (_spins.Length - 1);
            image = _spins[_spinIndex];
        }
    }
}
