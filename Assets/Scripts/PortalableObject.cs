using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalableObject : MonoBehaviour
{
    protected Portal inPortal;
    protected Portal outPortal;

    protected new Collider collider;

    private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

    protected virtual void Awake()
    {
        collider = GetComponent<Collider>();
    }

    public virtual void SetIsInPortal(Portal inPortal, Portal outPortal)
    {
        this.inPortal = inPortal;
        this.outPortal = outPortal;
    }

    public virtual void ExitPortal(Portal portal)
    {
        if (inPortal == portal)
        {
            this.inPortal = null;
            this.outPortal = null;
        }
    }

    public virtual void Warp()
    {
        if (inPortal == null || outPortal == null) return;

        var inTransform = inPortal.transform;
        var outTransform = outPortal.transform;
        
        // Update position of object.
        Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
        relativePos = halfTurn * relativePos;
        transform.position = outTransform.TransformPoint(relativePos);

        // Update rotation of object.
        Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
        relativeRot = halfTurn * relativeRot;
        transform.rotation = outTransform.rotation * relativeRot;

        // Swap portal references.
        var tmp = inPortal;
        inPortal = outPortal;
        outPortal = tmp;
    }
}
