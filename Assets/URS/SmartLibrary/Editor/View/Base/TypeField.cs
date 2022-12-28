using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Bewildered.SmartLibrary.UI
{
    internal class TypeField : BaseField<string>
    {
        public new static readonly string ussClassName = "bewildered-type-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        private static readonly string _textUssClassName = ussClassName + "__text";

        private Label _text;

        public TypeField() : base(null, null)
        {
            var visualInput = this.Q(className: BaseField<string>.inputUssClassName);

            labelElement.focusable = false;

            AddToClassList(ussClassName);
            AddToClassList(BasePopupField<object, object>.ussClassName);
            labelElement.AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.AddToClassList(BasePopupField<object, object>.inputUssClassName);

            _text = new Label();
            _text.RemoveFromClassList(Label.ussClassName);
            _text.AddToClassList(_textUssClassName);
            _text.AddToClassList(BasePopupField<object, object>.textUssClassName);
            _text.text = "None";
            visualInput.Add(_text);

            var arrow = new VisualElement();
            arrow.AddToClassList(BasePopupField<object, object>.arrowUssClassName);
            arrow.pickingMode = PickingMode.Ignore;
            arrow.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("icon dropdown").image;
            visualInput.Add(arrow);
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
                return;

            if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var visualInput = this.Q(className: BaseField<string>.inputUssClassName);
                var mde = (MouseDownEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.mousePosition)))
                {
                    OnFieldSelected(visualInput.worldBound, OnTypeSelect);
                    evt.StopPropagation();
                }
               
            }
        }

        protected virtual void OnFieldSelected(Rect buttonRect, Action<Type> onTypeSelected)
        {
            UnityTypeDropdown.Open(GUIUtility.GUIToScreenRect(buttonRect), onTypeSelected, string.IsNullOrEmpty(value) ? null : Type.GetType(value));
        }

        private void OnTypeSelect(Type type)
        {
            value = type?.AssemblyQualifiedName;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            Type type = string.IsNullOrEmpty(newValue) ? null : Type.GetType(newValue);

            _text.text = type == null ? "None" : type.Name + $" ({type.Namespace})";
        }
    }

}