using UnityEngine;

namespace Bewildered.SmartLibrary.UI
{
    internal class ListViewDragger : ListViewDragManipulator
    {
        protected override void OnDrop(ListDragArgs args, Vector3 mousePosition)
        {
            int targetIndex = targetListView.itemsSource.IndexOf(args.target);
            if (targetIndex == args.insertIndex)
                return;

            if (args.insertIndex > targetIndex)
                args.insertIndex--;

            targetListView.itemsSource.Remove(args.target);

            if (args.insertIndex > targetListView.itemsSource.Count)
                args.insertIndex--;
            targetListView.itemsSource.Insert(args.insertIndex, args.target);

#if UNITY_2021_2_OR_NEWER
            targetListView.Rebuild();
#else
            targetListView.Refresh();
#endif
        }
    } 
}
