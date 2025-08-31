using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Utility
{
    public class MultiSelectDropdown : VisualElement
    {
        private VisualElement _container;
        private VisualElement _dropdownTrigger;
        private VisualElement _options;
        
        private Label _label;
        
        private ScrollView _dropdown;
        private TextField _othersTextField;
        
        private string[] _allOptions;
        private string _text;
        
        private const string OTHER_SPECIFY = "Other (specify)";
        
        public List<string> SelectedOptions = new();

        public MultiSelectDropdown(string[] options, string label = "Select Options", TextField textField = null)
        {
            _allOptions = options;
            _othersTextField = textField;
            _text = label;
            
            _container = new VisualElement();
            _container.AddToClassList("unity-base-field");
            
            _label = new Label(label);

            _dropdownTrigger = new VisualElement { name = "DropdownTrigger" };
            _dropdownTrigger.AddToClassList("unity-base-field__input");
            // _dropdownTrigger.style.flexGrow = 0;
            _dropdownTrigger.RegisterCallback<PointerDownEvent>(evt => ToggleDropdown());

            var selectedTextLabel = new Label("") { name = "SelectedText" };
            selectedTextLabel.style.color = new Color(0, 0, 0, 1);
            selectedTextLabel.style.marginLeft = 20;
            _dropdownTrigger.Add(selectedTextLabel);

            _container.Add(_label);
            _container.Add(_dropdownTrigger);

            // _options = new VisualElement();
            // _options.AddToClassList("multi-select-dropdown");

            _dropdown = new ScrollView
            {
                style = { display = DisplayStyle.None }
            };
            _dropdown.horizontalScroller.SetEnabled(false);
            _dropdown.AddToClassList("multi-select-dropdown");

            foreach (var option in _allOptions)
            {
                var toggle = new Toggle(option);
                toggle.RegisterValueChangedCallback(evt => UpdateSelection(option, evt.newValue, selectedTextLabel));
                _dropdown.Add(toggle);
            }
            
            Add(_container);
            Add(_dropdown);
        }

        public void ClearSelection()
        {
            SelectedOptions.Clear();
            _dropdownTrigger.Q<Label>("SelectedText").text = "";
        }

        private void ToggleDropdown()
        {
            _dropdown.style.display = _dropdown.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
            _othersTextField.style.display = SelectedOptions.Contains(OTHER_SPECIFY) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateSelection(string option, bool isSelected, Label selectedTextLabel)
        {
            if (isSelected) SelectedOptions.Add(option);
            else SelectedOptions.Remove(option);

            selectedTextLabel.text = SelectedOptions.Count > 0 ? string.Join(", ", SelectedOptions) : "";
        }
    }

}