using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Portal : MonoBehaviour
{
    [field: SerializeField]
    public Portal OtherPortal { get; private set; }

    [SerializeField]
    private bool isPrePlaced = true;

    private List<PortalableObject> portalObjects = new List<PortalableObject>();
    public bool IsPlaced { get; private set; } = false;

    // Components.
    public Renderer Renderer { get; private set; }

    private void Awake()
    {
        Renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (isPrePlaced)
        {
            IsPlaced = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (OtherPortal == null || !OtherPortal.IsPlaced) return;

        for (int i = 0; i < portalObjects.Count; ++i)
        {
            if (portalObjects[i] == null) continue;

            Vector3 objPos = transform.InverseTransformPoint(portalObjects[i].transform.position);

            if (objPos.z > 0.0f)
            {
                portalObjects[i].Warp();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var obj = other.GetComponent<PortalableObject>();
        if (obj != null)
        {
            portalObjects.Add(obj);
            obj.SetIsInPortal(this, OtherPortal);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var obj = other.GetComponent<PortalableObject>();
        if (obj != null && portalObjects.Contains(obj))
        {
            portalObjects.Remove(obj);
            obj.ExitPortal();
        }
    }
}
