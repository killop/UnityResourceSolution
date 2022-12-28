using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class IconButton : VisualElement
    {
        /// <summary>
        /// Instantiates a Button using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<IconButton, IconButton.UxmlTraits>
        {
        }

        /// <summary>
        /// Defines UxmlTraits for the Button.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
        }
        
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "bewildered-icon-button";

        private Image _image;
        private Clickable _clickable;
        
        public Texture2D Icon
        {
            get { return _image.image as Texture2D; }
            set { _image.image = value; }
        }
        
        public Clickable Clickable
        {
            get { return _clickable; }
            set
            {
                if (_clickable != null && _clickable.target == this)
                    this.RemoveManipulator(_clickable);
                _clickable = value;
                if (_clickable == null)
                    return;
                this.AddManipulator(_clickable);
            }
        }
        
        public event Action OnClicked
        {
            add
            {
                if (_clickable == null)
                    Clickable = new Clickable(value);
                else
                    _clickable.clicked += value;
            }
            remove
            {
                if (_clickable == null)
                    return;
                _clickable.clicked -= value;
            }
        }

        public IconButton()
        {
            AddToClassList(ussClassName);

            _image = new Image();
            _image.AddToClassList("bewildered-library-icon");
            Add(_image);
        }
    }
}
