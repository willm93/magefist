using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float duration = 1f;
    [System.Serializable] public class OnValueChangedEvent : UnityEvent<float> {}
    [SerializeField] OnValueChangedEvent OnValueChanged = default;
    [SerializeField] bool autoReverse, smoothStep;
    float value;
    float SmoothedValue => 3f * Mathf.Pow(value, 2) - 2f * Mathf.Pow(value, 3);
    bool reversed;
    

    void FixedUpdate() 
    {
        if (reversed && autoReverse) {
            value -= Time.deltaTime / duration;
        }
        else {
            value += Time.deltaTime / duration;
        }
            
        if (value >= 1f) {
            value = 1f;
            reversed = true;
            enabled = autoReverse;
        }

        if (value <= 0f) {
            value = 0f;
            reversed = false;
        }

        OnValueChanged.Invoke(smoothStep ? SmoothedValue : value);
    }
}
