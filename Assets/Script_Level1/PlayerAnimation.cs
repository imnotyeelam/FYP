
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Movement Animation")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float smoothTime = 0.1f;

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (animator == null)
            return;

        // Read actual horizontal movement speed
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        float animationSpeed;

        if (speed <= walkSpeed)
        {
            animationSpeed = Mathf.InverseLerp(
                0f, walkSpeed, speed
            ) * 0.5f;
        }
        else
        {
            animationSpeed = Mathf.Lerp(
                0.5f, 1f,
                Mathf.InverseLerp(walkSpeed, runSpeed, speed)
            );
        }

        animator.SetFloat(
            "Speed",
            animationSpeed,
            smoothTime,
            Time.deltaTime
        );
    }
}
