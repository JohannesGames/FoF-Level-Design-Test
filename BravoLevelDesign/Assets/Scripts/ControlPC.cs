using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Huge help from https://github.com/GenaSG/UnityUnetMovement for server-authoritative movement
/// </summary>

public struct MovementMod
{
    public MovementMod(Vector3 direction, float startTime, float removeTime, bool fade, bool groundClear, bool gravReset)
    {
        modDirection = currentVector = direction;
        modStartTime = startTime;
        modRemoveTime = removeTime;
        modFadesOut = fade;
        resetGravityWhileActive = gravReset;
        removeWhenGrounded = groundClear;
    }

    public Vector3 modDirection;
    public Vector3 currentVector;
    public float modStartTime;
    public float modRemoveTime;
    public bool modFadesOut;
    public bool removeWhenGrounded;
    public bool resetGravityWhileActive;
}

public class ControlPC : MonoBehaviour
{

    # region Gameplay Variables

    // Animation
    public AnimationCurve exponentialCurveUp;

    // Basic Movement
    [HideInInspector]
    public CharacterController cc;
    private Vector3 moveDirection;
    private float speed;
    [Header("Basic Movement")]
    public float baseSpeed;
    public float sprintMultiplier = 1;
    public float strafeMultiplier = .8f;
    public float airBaseSpeed;
    public bool isGrounded;
    private bool isFalling;
    [HideInInspector]
    public List<MovementMod> movementModifiers = new List<MovementMod>();

    // Jumping
    [Header("Jumping")]
    public float jumpTimeLength = 1;
    public float jumpHeight = 2;
    private bool isJumping;
    private float jumpTimer = 0;


    // Rigidbody & Physics
    private bool wasStopped;
    public float gravity = 1;
    [HideInInspector]
    public float appliedGravity;

    // Camera
    [Header("Camera")]
    public Transform cameraContianer;
    [HideInInspector]
    public Camera cam;
    public float yRotationSpeed = 45;
    public float xRotationSpeed = 45;
    private float yRotation;
    private float xRotation;

    private enum AnimationStates
    {
        Idle,
        Walking,
        Running,
        Jumping,
    }
    private AnimationStates pcAnimationState;

    // Debugging
    [Header("Debugging")]
    public bool calculateDistanceTravelled;
    public bool calculateSpeed;
    Vector3 lastPosition;
    float timer = 0;
    float distanceInterval = 0;
    float cumulativeDistance = 0;

    #endregion

    #region Setup Function

    void Start()
    {
        cameraContianer.GetChild(0).gameObject.SetActive(true);
        cam = cameraContianer.GetComponentInChildren<Camera>();
        yRotation = transform.localEulerAngles.y;
        xRotation = cam.transform.localEulerAngles.x;
        wasStopped = true;
        appliedGravity = gravity / 2;
        Application.runInBackground = true;
        cc = GetComponent<CharacterController>();
        lastPosition = transform.position;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    #endregion

    #region Updates and Inputs

    void Update()
    {
        GetPlayerInput();
        MovePC();
        if (Input.GetKeyDown(KeyCode.Escape))   //show cursor in editor
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        CheckForGround();
    }

    private void LateUpdate()
    {
        if (calculateDistanceTravelled || calculateSpeed)
        {
            timer += Time.deltaTime;

            if (timer >= .2f)
            {
                timer = 0;
                distanceInterval = Vector3.Distance(lastPosition, transform.position);
                cumulativeDistance += distanceInterval;
                if (calculateDistanceTravelled) print(cumulativeDistance);
                if (calculateSpeed) print(distanceInterval / .2f);
                lastPosition = transform.position;
            }

        }
    }


    void GetPlayerInput()
    {
        // Keyboard input
        moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveDirection.Normalize();
        moveDirection = transform.TransformDirection(moveDirection);
        speed = 0;
        if (Mathf.Abs(moveDirection.x) != 0 || Mathf.Abs(moveDirection.z) != 0)
        {
            if (Mathf.Abs(moveDirection.x) == 1 || Mathf.Abs(moveDirection.z) == 1)
            {
                wasStopped = false;
            }

            pcAnimationState = AnimationStates.Running;
            speed = 1;

            //if (sprintMultiplier != 0 && Input.GetButton("Sprint"))  // if PC is sprinting
            //{
            //    speed *= sprintMultiplier;
            //    pcAnimationState = AnimationStates.Running;
            //}
        }

        // Aerial
        if (isGrounded && !isJumping && Input.GetButtonDown("Jump"))
        {
            isJumping = true;
        }


        // Mouse input
        yRotation += Input.GetAxis("Mouse X") * yRotationSpeed * Time.deltaTime;
        xRotation -= Input.GetAxis("Mouse Y") * xRotationSpeed * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (xRotation != cam.transform.eulerAngles.x || yRotation != transform.eulerAngles.y)
        {
            cameraContianer.transform.localEulerAngles = new Vector3(xRotation, 0, 0);
            transform.localEulerAngles = new Vector3(0, yRotation, 0);
        }
    }

    sbyte RoundToLargest(float inp)
    {
        if (inp > 0)
        {
            return 1;
        }
        else if (inp < 0)
        {
            return -1;
        }
        return 0;
    }

    #endregion

    void ResetGravity()
    {
        appliedGravity = 0;
    }

    void MovePC()
    {
        if (isGrounded)
        {
            if (Mathf.Abs(moveDirection.x) != 0 || Mathf.Abs(moveDirection.z) != 0) // if there's some input
            {
                moveDirection *= baseSpeed * speed;
            }
            else
            {
                pcAnimationState = AnimationStates.Idle;
            }
        }
        else
        {
            if (Mathf.Abs(moveDirection.x) != 0 || Mathf.Abs(moveDirection.z) != 0) // if there's some input
            {
                moveDirection *= airBaseSpeed * speed;
            }
        }

        ApplyJump();
        ApplyGravity();
        ApplyMovementModifiers();

        cc.Move(moveDirection * Time.deltaTime);
        moveDirection = Vector3.zero;
        if (cc.velocity == Vector3.zero) wasStopped = true;

        ResetGravityFromModifier();
    }

    void ApplyJump()
    {
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            moveDirection += Vector3.up * jumpHeight * (1 - (jumpTimer / jumpTimeLength));

            if (jumpTimer >= jumpTimeLength)
            {
                isJumping = false;
                appliedGravity = jumpTimer = 0;
            }
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            if (!isFalling)
            {
                isFalling = true;
                movementModifiers.Add(new MovementMod(cc.velocity / 2, Time.time, Time.time + 1, true, true, false));
            }
        }
        if (!isJumping)
        {
            moveDirection += Vector3.down * appliedGravity;
            appliedGravity += gravity * Time.deltaTime;
        }

    }

    bool CheckForGround()
    {
        int layermask = 1 << 9;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, .5f, Vector3.down, out hit, .6f, layermask))
        {
            appliedGravity = gravity / 3;
            isFalling = false;
            GroundClearMoveMods();
            return isGrounded = true;
        }
        else
        {
            return isGrounded = false;
        }
    }

    #region Movement Mods

    void ApplyMovementModifiers()   // applies movement modifiers (e.g. motion retained when walking over an edge, or from an explosion)
    {
        for (int i = movementModifiers.Count - 1; i > -1; i--)
        {
            if (Time.time >= movementModifiers[i].modStartTime) // if mod effect is to start
            {
                if (Time.time >= movementModifiers[i].modRemoveTime)    // if the movement modifier has timed out
                {
                    movementModifiers.RemoveAt(i);
                }
                else
                {
                    if (movementModifiers[i].modFadesOut)   // if the mod force fades out over time reduce it's force
                    {
                        moveDirection += movementModifiers[i].modDirection *
                            (1 - (Time.time - movementModifiers[i].modStartTime) / (movementModifiers[i].modRemoveTime - movementModifiers[i].modStartTime));
                    }
                    else
                    {
                        moveDirection += movementModifiers[i].currentVector;
                    }

                }
            }
        }
    }

    void ResetGravityFromModifier()
    {
        for (int i = movementModifiers.Count - 1; i > -1; i--)
        {
            if (movementModifiers[i].resetGravityWhileActive)
            {
                appliedGravity = 0;
                return;
            }
        }
    }

    void GroundClearMoveMods()
    {
        for (int i = movementModifiers.Count - 1; i > -1; i--)
        {
            if (movementModifiers[i].removeWhenGrounded)
            {
                movementModifiers.RemoveAt(i);
            }
        }
    }

    #endregion
}
