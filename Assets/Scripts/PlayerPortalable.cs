using UnityEngine;
using StarterAssets;

public class PlayerPortalable : PortalableObject
{
    private FirstPersonController playerController;
    private CharacterController characterController;

    protected override void Awake()
    {
        base.Awake();
        
        playerController = GetComponent<FirstPersonController>();
        characterController = GetComponent<CharacterController>();
    }

    public override void Warp()
    {
        if (inPortal == null || outPortal == null) return;
        
        var inTransform = inPortal.transform;
        var outTransform = outPortal.transform;
        Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        Vector3 relativeVel = inTransform.InverseTransformDirection(playerController.Velocity);
        relativeVel = halfTurn * relativeVel;
        playerController.Velocity = outTransform.TransformDirection(relativeVel);
        
        characterController.enabled = false;
        
        base.Warp();
        
        Vector3 newForward = transform.forward;
        newForward.y = 0;
        if (newForward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(newForward.normalized, Vector3.up);
        }

        characterController.enabled = true;
    }
}
