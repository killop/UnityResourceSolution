using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Wrapper around the <see cref="Selection"/> API. Allows setting selection to an asset Object without focusing it in a Project Browser.
    /// </summary>
    internal static class AssetSelection
    {
        private static Type _projectBrowserType;
        private static PropertyInfo _isLockedProperty;
        private static bool[] _cachedProjectLockStates = new bool[0];

        /// <summary>
        /// Returns the actual object selection.
        /// </summary>
        public static Object activeObject
        {
            get { return Selection.activeObject; }
            set
            {
                Initialize();

                if (value != Selection.activeObject)
                    LockProjectWindows();

                Selection.activeObject = value;
            }
        }

        /// <summary>
        /// The actual unfiltered selection.
        /// </summary>
        public static Object[] objects
        {
            get { return Selection.objects; }
            set
            {
                Initialize();
                if (DoLockWindows(value))
                    LockProjectWindows();

                Selection.objects = value;
            }
        }

        private static void Initialize()
        {
            // Has the values already been initialized.
            if (_projectBrowserType != null)
                return;

            _projectBrowserType = Assembly.Load(typeof(Editor).Assembly.FullName).GetType("UnityEditor.ProjectBrowser");
            _isLockedProperty = _projectBrowserType.GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            // The selectionChanged event does not get invoked right after setting the selection,
            // so we need to wait for it to be invoked and revert the lock states there instad of right after setting the selection. Otherwise the project windows would still update.
            Selection.selectionChanged += RevertProjectWindowsLock;
        }

        private static bool DoLockWindows(Object[] objs)
        {
            if (objs == null)
                return false;

            if (objs.Length == 0)
                return false;

            if (objs == Selection.objects)
                return false;

            if (objs.OrderBy(o => o != null ? o.name : "").SequenceEqual(Selection.objects.OrderBy(o => o != null ? o.name : "")))
                return false;

            return true;
        }

        private static void LockProjectWindows()
        {
            // Don't update the cached locked states if they have not been reverted since the last call.
            // This can happen if 'objects' or 'activeObject' is set to the same value it already is since that will not invoke the 'selectionChanged' event.
            if (_cachedProjectLockStates.Length > 0)
                return;
            
            // Find all the project windows.
            Object[] projectWindows = Resources.FindObjectsOfTypeAll(_projectBrowserType);
            _cachedProjectLockStates = new bool[projectWindows.Length];
            for (int i = 0; i < projectWindows.Length; i++)
            {
                // Cache the window's current lock state so that it can be reset properly after selection changed.
                _cachedProjectLockStates[i] = (bool)_isLockedProperty.GetValue(projectWindows[i]);
                // Lock the window so that when the selection is changed the selected asset is not focused in the project window.
                _isLockedProperty.SetValue(projectWindows[i], true);
            }
        }

        private static void RevertProjectWindowsLock()
        {
            // There is nothing to revert back to se we just stop here.
            if (_cachedProjectLockStates.Length == 0)
                return;

            // Find all the project windows.
            Object[] projectWindows = Resources.FindObjectsOfTypeAll(_projectBrowserType);
            // Apply the cached lock state for each window.
            for (int i = 0; i < projectWindows.Length; i++)
            {
                _isLockedProperty.SetValue(projectWindows[i], _cachedProjectLockStates[i]);
            }

            // Clear the cached lock states so we know that the states have been reverted.
            _cachedProjectLockStates = new bool[0];
        }
    } 
}
