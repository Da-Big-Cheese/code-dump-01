using System.Collections;
using UnityEngine; 

[RequireComponent(typeof(CharacterController))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float rotationSpeed = 360f;

    [SerializeField] private float accelerationFactor = 5f;
    [SerializeField] private float decelerationFactor = 10f;

    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private float jumpSpeed = 5f;
    

    [Header("Dash")]
    [SerializeField] private float dashingSpeed = 30f;
    [SerializeField] private float dashingCooldown = 1.5f;
    [SerializeField] private float dashingTime = 0.3f;

    private bool _jumpInput;

    private bool _canDash;
    private bool _isDashing;

    private bool _dashInput;

    private Vector3 _velocity;
    private float _currentSpeed;
    private InputSystem_Actions playerInputActions;
    private Vector3 _input; 
    private CharacterController _characterController;
    private Vector3 _moveDirection;


    private void Awake()
    {
        playerInputActions = new InputSystem_Actions();
        _characterController = GetComponent<CharacterController>();

        _canDash = true;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }

    private void Update()
    {
        bool isGrounded = _characterController.isGrounded;

        if (isGrounded)
        {
            if (_velocity.y < 0f)
            _velocity.y = -2f; // keeps controller grounded

            if (_jumpInput)
            {
                _velocity.y = jumpSpeed; // applies jump velocity when the player is grounded and jump input is pressed
            }
        }

        else
        {
            _velocity.y += gravity * Time.deltaTime; // applies gravity when the player is not grounded
            
            if (!_jumpInput && _velocity.y > 0f)
            {
                _velocity.y += gravity * 2f * Time.deltaTime; // increases gravity when the player releases the jump input while ascending
            }

        }

        

       GatherInput(); // collects player input each frame

       Look(); // rotates the player to face the movement direction
       CalculateSpeed(); 


       Move();

       if (_dashInput && _canDash)
        {
            StartCoroutine(Dash()); // starts the dash coroutine if the player pressed the dash input and is allowed to dash
        }

        

        
       

    }

    private IEnumerator Dash()
    {
        _canDash = false; // disables dashing right after it is used (prevents spamming)
        _isDashing = true;
        yield return new WaitForSeconds(dashingTime); // waits until the dash duration is over 
        _isDashing = false;
        yield return new WaitForSeconds(dashingCooldown); // waits until dash cooldown is over then re-enables dashing

        _canDash = true;
    }

    private void CalculateSpeed()
    {
        float targetSpeed = (_moveDirection == Vector3.zero) ? 0f : maxSpeed; 
        // sets target speed based on whether the player is moving or not

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, (_moveDirection == Vector3.zero ? decelerationFactor : accelerationFactor) * Time.deltaTime); 
        // smoothly interpolates current speed towards target speed based on whether the player is moving or not
    }

    private void Look()
    {
        if (_moveDirection == Vector3.zero) return;

         Quaternion targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up); // calculates the target rotation based on movement direction
         transform.rotation = Quaternion.RotateTowards(transform.rotation,targetRotation,rotationSpeed * Time.deltaTime); // smoothly rotates the player towards the target rotation
    }

    private void Move()
    {
        float airControl = _characterController.isGrounded ? 1f : 0.6f; // reduces control in the air

        Vector3 movement = _moveDirection * _currentSpeed * airControl * Time.deltaTime;

        if (_isDashing)
        {
            movement = _moveDirection * dashingSpeed * Time.deltaTime;
        }

        
        movement += _velocity * Time.deltaTime;
        _characterController.Move(movement);
    }

    private void GatherInput()
    {
       Vector2 input = playerInputActions.Player.Move.ReadValue<Vector2>(); 
        _input = new Vector3(input.x, 0, input.y).normalized;

        _dashInput = playerInputActions.Player.Sprint.WasPressedThisFrame();

        if (_input.magnitude > 0.01f) // Converts raw player input into a normalized world-space movement direction
        {
            Matrix4x4 isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45f, 0)); // using an isometric (45° rotated) coordinate system.
            _moveDirection = isoMatrix.MultiplyPoint3x4(_input).normalized; // Input magnitude is used only as a deadzone check to prevent tiny inputs
        }
        else
        {
            _moveDirection = Vector3.zero; // from causing unintended movement or rotation.
        }

        _jumpInput = playerInputActions.Player.Jump.WasPressedThisFrame();
    }
}
