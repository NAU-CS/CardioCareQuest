using System;
using UnityEngine.UIElements;
using Utility;

namespace Gameplay.UI
{
    public class Popup : Singleton<Popup>
    {
        private VisualElement _container;
        private Label _title;
        private Label _message;
        private Button _button;

        private ScrollView _scrollView;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            _container = root.Q<VisualElement>("container");
            _title = root.Q<Label>("title");
            _message = root.Q<Label>("message");
            _button = root.Q<Button>("button");
            
            _scrollView = root.Q<ScrollView>("terms-view");

            _message.style.display = DisplayStyle.None;
            _scrollView.style.display = DisplayStyle.None;
            
            _button.clicked += Hide;
        }
        
        private void OnDisable()
        {
            _button.clicked -= Hide;
        }
        
        public void Show(string message, string title = "Error", string buttonText = "OK", Action onClick = null)
        {
            _title.text = title;
            _message.style.display = DisplayStyle.Flex;
            _scrollView.style.display = DisplayStyle.None;
            _message.text = message;
            _button.text = buttonText;

            _button.clicked += () =>
            {
                onClick?.Invoke();
            };

            _container.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _container.style.display = DisplayStyle.None;
        }
    }
}