using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerFootsteps : MonoBehaviour
{
    private const float MoveThreshold = 0.01f;

    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    private AudioSource audioSource;
    private CharacterController characterController;
    private FirstPersonController player;
    private StarterAssetsInputs input;
    private bool wasGrounded;
    private bool wasJumpPressed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        player = GetComponent<FirstPersonController>();
        input = GetComponent<StarterAssetsInputs>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        oneShotSource.playOnAwake = false;
        wasGrounded = player.Grounded;
    }

    private void Update()
    {
        bool grounded = player.Grounded;
        bool jumpPressed = input.jump && !wasJumpPressed;

        if (grounded && jumpPressed)
        {
            oneShotSource.PlayOneShot(jumpClip);
        }

        if (!wasGrounded && grounded)
        {
            oneShotSource.PlayOneShot(landClip);
        }

        audioSource.pitch = input.sprint && !input.crouch ? 2f : 1f;

        Vector3 velocity = characterController.velocity;
        bool moving = new Vector2(velocity.x, velocity.z).sqrMagnitude > MoveThreshold;
        bool shouldPlay = grounded && input.move.sqrMagnitude > MoveThreshold && moving;

        if (shouldPlay && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!shouldPlay && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        wasGrounded = grounded;
        wasJumpPressed = input.jump;
    }
}
