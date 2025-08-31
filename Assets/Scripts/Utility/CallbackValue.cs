using System;

namespace Utility
{
    public class CallbackValue<T>
    {
        public event Action<T> Changed;

        private T _value;

        public T Value {
            get => _value;
            set
            {
                if (_value != null && _value.Equals(value))
                    return;

                _value = value;
                Changed?.Invoke(_value);
            }
        }

        public CallbackValue() {
            _value = default;
        }

        public CallbackValue(T value) {
            _value = value;
        }

        public void ForceSet(T value) {
            _value = value;
            Changed?.Invoke(_value);
        }

        public void SetNoCallback(T value) {
            _value = value;
        }
    }
}