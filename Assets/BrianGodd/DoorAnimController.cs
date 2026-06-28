using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAnimController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetState()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetInteger("State", 0);
        }
    }
}
