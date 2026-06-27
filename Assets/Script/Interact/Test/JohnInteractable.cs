using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class JohnInteractable : MonoBehaviour
{
    [SerializeField] bool autoSetInteractable = true;
    public void Interact()
    {
        Debug.Log($"[{gameObject.name}|{name}]: Interact called on " + gameObject.name);
    }

    public void OnFocus()
    {
        Debug.Log($"[{gameObject.name}|{name}]: OnFocus called on " + gameObject.name);
    }

    public void OnLostFocus()
    {
        Debug.Log($"[{gameObject.name}|{name}]: OnLostFocus called on " + gameObject.name);
    }

    private void Start()
    {
        if (!autoSetInteractable) return;
        
        if (!TryGetComponent<Interactable>(out var interactable))
        {
            Debug.LogError($"[{gameObject.name}|{name}]: Interactable component not found on " + gameObject.name);
            return;
        }

        interactable.OnInteract.AddListener(Interact);
        interactable.OnRaycastEnter.AddListener(OnFocus);
        interactable.OnRaycastExit.AddListener(OnLostFocus);
    }
}
