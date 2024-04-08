using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectionZone : MonoBehaviour
{
    [SerializeField] UnityEvent OnFirstEnter = default, OnLastExit = default;
    List<Collider> colliders = new List<Collider>();

    void Awake()
    {
        enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (colliders.Count == 0) {
            OnFirstEnter.Invoke();
            enabled = true;
        }
        colliders.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (colliders.Remove(other) && colliders.Count == 0) {
            OnLastExit.Invoke();
            enabled = false;
        }  
    }

    void FixedUpdate()
    {
        for (int i = 0; i < colliders.Count; i++) {
            Collider collider = colliders[i];
            if (collider == null || !collider.gameObject.activeInHierarchy) {
                colliders.RemoveAt(i--);
                if (colliders.Count == 0) {
                    OnLastExit.Invoke();
                    enabled = false;
                }   
            }
        }
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        if (enabled && gameObject.activeInHierarchy) 
            return;
#endif
        if (colliders.Count > 0) {
            colliders.Clear();
            OnLastExit.Invoke();
        }
    }
}
