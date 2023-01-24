using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class CharacterStance
{
    public float CameraHeight;
    public CapsuleCollider StanceCollider;
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Enable Features")]
    [SerializeField] private bool enableSprint = true;
    [SerializeField] private bool enableFootstepSound = true;
    [SerializeField] private bool enableCrouch = true;
    [SerializeField] private bool enableJump = true;
    [SerializeField] private bool enableHeadbob = true;
    [SerializeField] private bool enableIdleBob = true;

    [Header("Basic Parameters")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float walkSpeed = 12f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float groundDistance = 0.4f;
    public bool sprinting = false;
    private Vector3 scale;
    Vector3 velocity;
    public Vector3 move;
    public bool canMove = true;
    Vector3 jumpMove;
    bool _isGrounded = false;
    public bool isGrounded
    {
        get { return _isGrounded; }
        set
        {
            if(value == true)
            {
                if (_isGrounded != value)
                {
                    if (enableFootstepSound)
                    {
                        FootstepSoundPlay();
                        FootstepTimerReset();
                    }

                    velocity.y = -4f;
                    jumpMove = Vector3.zero;
                }
            }

            _isGrounded = value;
        }
    }


    [Header("Footstep Sound Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private string[] defaultStepSounds;
    [SerializeField] private string[] stoneStepSounds;
    [SerializeField] private string[] metalStepSounds;
    private float footstepTimer = 0;

    public enum PlayerStance
    {
        Stand,
        Crouch
    }

    [Header("Crouching Parameters")]
    public PlayerStance playerStance;
    [SerializeField] private float playerStanceSmoothing;
    [SerializeField] private CharacterStance playerStandStance;
    [SerializeField] private CharacterStance playerCrouchStance;
    [SerializeField] private CharacterStance currentStance;

    private float cameraHeightVelocity;
    private float crouchMove;

    private Vector3 stanceCapsuleCentre;
    private Vector3 stanceCapsuleCentreVelocity;

    private float stanceCapsuleHeight;
    private float stanceCapsuleHeightVelocity;

    [Header("Head Bob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.5f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.25f;
    private float headbobTimer;
    private float headbobMove;

    [Header("Idle Bob Parameters")]
    [SerializeField] private float idleBobSpeed = 8f;
    [SerializeField] private float idleBobAmount = 0.25f;
    private float idleBobTimer;
    private float idleBobMove;

    [Header("References")]
    public CharacterController cController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private AudioManager audioManager;


    // Start is called before the first frame update
    void Awake()
    {
        currentStance = playerStandStance;
    }

    void Sprint()
    {
        if (Input.GetButton("Sprint") && currentStance != playerCrouchStance)
        {
            sprinting = true;
        }
        else
        {
            sprinting = false;
        }
    }
    
    void FootstepSound()
    {
        if (move != Vector3.zero)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                FootstepSoundPlay();
                FootstepTimerReset();
            }
        }
    }

    void FootstepSoundPlay()
    {
        if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, 3))
        {
            switch (hit.collider.tag)
            {
                case "Stone":
                    audioManager.Play(stoneStepSounds[Random.Range(0, stoneStepSounds.Length)]);
                    break;
                case "Metal":
                    audioManager.Play(metalStepSounds[Random.Range(0, metalStepSounds.Length)]);
                    break;
                default:
                    audioManager.Play(defaultStepSounds[Random.Range(0, defaultStepSounds.Length)]);
                    break;
            }
        }
    }

    void FootstepTimerReset()
    {
        if (playerStance == PlayerStance.Crouch)
            footstepTimer = baseStepSpeed * crouchStepMultiplier;
        else if (sprinting)
            footstepTimer = baseStepSpeed * sprintStepMultiplier;
        else
            footstepTimer = baseStepSpeed;
    }

    void Headbob()
    {
        if(move != Vector3.zero)
        {
            headbobTimer += Time.deltaTime * (playerStance == PlayerStance.Crouch ? crouchBobSpeed : sprinting ? sprintBobSpeed : walkBobSpeed);
            headbobMove = Mathf.Sin(headbobTimer) * (playerStance == PlayerStance.Crouch ? crouchBobAmount : sprinting ? sprintBobAmount : walkBobAmount);
        }
        else
        {
            if(headbobMove != 0f)
                headbobMove = 0f;
        }
    }

    void IdleBob()
    {
        idleBobTimer += Time.deltaTime * idleBobSpeed;
        idleBobMove = Mathf.Sin(idleBobTimer) * idleBobAmount;
    }

    bool CanStand()
    {
        if (Physics.CheckCapsule(transform.position + Vector3.up * (playerStandStance.StanceCollider.radius + Physics.defaultContactOffset),
                                 transform.position + Vector3.up * (playerStandStance.StanceCollider.height - playerStandStance.StanceCollider.radius),
                                 playerStandStance.StanceCollider.radius - Physics.defaultContactOffset, -1, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    void Crouch()
    {
        // crouching inputs
        if (Input.GetButton("Crouch"))
        {
            playerStance = PlayerStance.Crouch;
            currentStance = playerCrouchStance;
        }
        else
        {
            if(CanStand())
            {
                playerStance = PlayerStance.Stand;
                currentStance = playerStandStance;
            }
        }

        cController.height = Mathf.SmoothDamp(cController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        cController.center = Vector3.SmoothDamp(cController.center, currentStance.StanceCollider.center, ref stanceCapsuleCentreVelocity, playerStanceSmoothing);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpMove = move;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove == false) return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        move = transform.right * x + transform.forward * z;     // calculate movement vector
        move += jumpMove;
        
        if (isGrounded)
        {
            if(enableSprint)
                Sprint();

            if(enableFootstepSound)
                FootstepSound();

            if(enableHeadbob)
                Headbob();

            if (enableIdleBob)
                IdleBob();
        }

        if (enableCrouch)
            Crouch();

        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, 
            Mathf.SmoothDamp(cameraTransform.localPosition.y, currentStance.CameraHeight + headbobMove + idleBobMove, ref cameraHeightVelocity, playerStanceSmoothing), 
            cameraTransform.localPosition.z);

        if (sprinting)
            speed = walkSpeed * 2;
        else if(currentStance == playerCrouchStance)
            speed = walkSpeed / 2;
        else
            speed = walkSpeed;

        cController.Move(move * speed * (Time.deltaTime));    // apply movement

        if (enableJump)
            Jump();

        // apply gravity to velocity
        if(!isGrounded)
            velocity.y += gravity * Time.deltaTime;

        // move with the velocity
        cController.Move(velocity * Time.deltaTime);
    }
}
