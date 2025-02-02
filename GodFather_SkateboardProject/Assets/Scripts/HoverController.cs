using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
public class HoverController : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    [Header("Suspensions")]
    [Range(0.0f, 10f)]
    [SerializeField] private float multiplier = 2.5f;
    [SerializeField] private float maxHeight = 5f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private Transform[] anchors = new Transform[4];
    RaycastHit[] hits = new RaycastHit[4];

    [Header("----------------------------------------")]
    [Header("Forward")]
    [SerializeField] private float moveForce = 1000f;
    [SerializeField] private float strengthImpulse = 50;

    [Space(10)]
    [Header("Turn")]
    [SerializeField] private float turnTorque = 100f;
    [Range(0.4f, 10f)]
    [SerializeField] private float _sharpTurn = 1f;
    [Range(1f, 15f)]
    [SerializeField] private float _wideBend = 8f;
    private float magnitude;
    float currentTorque;

    [Header("Speed")]
    [Tooltip("magnitude => [4 - 40]")]
    [SerializeField] private float playerMaxSpeed = 20;

    [SerializeField] private bool grounded;
    public int PlayerNumber;

    private Vector2 movementInput;

    private Quaternion _startRotation;

    private bool canImpulse;

    private bool canReset;
    private bool resetDeccel;

    private Vector2 Axis;
    private Vector3 downDir;
    [HideInInspector] public Vector3 DirectionCOl;

    [HideInInspector] public HoverController player2;
    [HideInInspector] public BoxCollider[] player2Collisions;

    private void Awake()
    {
        //MultiplayerManager.instance.AddPlayer(this);
    }
    private void OnEnable()
    {
        try
        {
            if (PlayerNumber == 0)
                MultiplayerManager.instance.GetPlayer(1).AddRefToPlayer();
            else
                MultiplayerManager.instance.GetPlayer(0).AddRefToPlayer();
        }
        catch { }
    }
    public void AddRefToPlayer()
    {
        if (PlayerNumber == 0)
            player2 = MultiplayerManager.instance.GetPlayer(1);
        else
            player2 = MultiplayerManager.instance.GetPlayer(0);

    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        canImpulse = true;
        _startRotation = new Quaternion(0, 0, 0, 1);
        currentTorque = turnTorque;
        downDir = -transform.parent.transform.up;
        Debug.Log(downDir);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        LimitRotation();
    }
    float timeLeft;
    void FixedUpdate()
    {
        if (grounded)
        {
            Axis = movementInput;
            timeLeft = 3;
            rb.AddForce(Axis.y * moveForce * Time.fixedDeltaTime * transform.forward, ForceMode.Impulse);
        }
        else
        {
            if(timeLeft > 0)
            timeLeft -= Time.fixedDeltaTime;
            else timeLeft = 0;
            rb.AddForce(timeLeft * moveForce * Time.fixedDeltaTime * transform.forward, ForceMode.Impulse);

        }

        for (int i = 0; i < 4; i++)
        {
            ApplyForce(anchors[i], hits[i]);
        }

        TorqueSetting();
        
        rb.AddTorque(Axis.x * turnTorque * Time.fixedDeltaTime * transform.up, ForceMode.Impulse);

        LimitMaxSpeed();
    }
    float calculspeed;
    private void TorqueSetting()
    {
        magnitude = Mathf.Clamp(rb.velocity.magnitude, _sharpTurn, _wideBend);
        turnTorque = Mathf.Lerp(turnTorque, currentTorque / magnitude, 2);
    }

    public void OnImpulse()
    {
        if (canImpulse)
            StartCoroutine(Impulse());
    }

    private void LimitMaxSpeed()
    {
        if (rb.velocity.magnitude > playerMaxSpeed)
            rb.velocity = rb.velocity.normalized * playerMaxSpeed;
    }
    private void LimitRotation()
    {
        //Si le skate se retourne
        if (Mathf.Abs(transform.position.z) >= 90f || Mathf.Abs(transform.position.x) >= 110f) { WaitToReset(); }
        if (Input.GetKeyDown(KeyCode.R)) ResetRotation();
    }
    private void WaitToReset()
    {
        canReset = true;
        //display "Press R to Reset" after 2Secs
    }

    private void ResetRotation()
    {
        canReset = false;
        transform.rotation = _startRotation;
        //ou appeler le spawner
    }

    private void ApplyForce(Transform anchor, RaycastHit hit)
    {
        if (Physics.Raycast(anchor.position, -anchor.up, out hit, minHeight))
        {
            Debug.DrawRay(anchor.position, -anchor.up * maxHeight, Color.green);
            grounded = true;
        }
        if (Physics.Raycast(anchor.position, -anchor.up, out hit, maxHeight))
        {
            float force = 0;
            force = Mathf.Abs(1 / (hit.point.y - anchor.position.y));
            rb.AddForceAtPosition(transform.up * force * multiplier, anchor.position, ForceMode.Acceleration);
        }
        else grounded = false;
    }

    IEnumerator Impulse()
    {
        canImpulse = false;
        rb.AddForce(moveForce * Time.fixedDeltaTime * transform.forward * strengthImpulse, ForceMode.Impulse);
        yield return new WaitForSeconds(1);
        canImpulse = true;
    }
}
