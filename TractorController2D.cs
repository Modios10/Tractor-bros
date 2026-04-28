using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class TractorController2D : MonoBehaviour
{
    private const int AxisNeutral = 127;
    private const int AxisDeadzone = 20;

    [Header("Movimiento")]
    public float drive1Speed = 4f;
    public float drive2Speed = 7f;
    public float acceleration = 20f;
    public float deceleration = 25f;

    [Header("Rotacion visual")]
    public float rotationOffset = -90f;

    [Header("UI")]
    [SerializeField] private TMP_Text gearText;

    [Header("Input")]
    [SerializeField] private bool forceKeyboardInput = false;

    private Rigidbody2D rb;
    private Vector2 input;
    private Vector2 lastMoveDir = Vector2.up;
    private int currentGear = 0;
    private bool keyboardGearInitialized = false;

    private const int GEAR_NEUTRAL = 0;
    private const int GEAR_DRIVE_1 = 1;
    private const int GEAR_DRIVE_2 = 2;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        bool usingHardware = SerialController.HasHardwareData && !forceKeyboardInput;

        if (usingHardware)
        {
            currentGear = SerialController.Gear;
            float x = -AxisToDirection(SerialController.AxisX);  // Invertir solo X
            float y = AxisToDirection(SerialController.AxisY);   // Y sin invertir
            input = new Vector2(x, y).normalized;
        }
        else
        {
            // Fallback teclado para pruebas sin DE10 Lite.
            if (!keyboardGearInitialized)
            {
                currentGear = GEAR_DRIVE_1;
                keyboardGearInitialized = true;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) currentGear = GEAR_NEUTRAL;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentGear = GEAR_DRIVE_1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentGear = GEAR_DRIVE_2;

            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y = -1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y = 1f;

            input = new Vector2(x, y).normalized;
        }

        if (currentGear == GEAR_NEUTRAL)
        {
            input = Vector2.zero;
        }

        if (input.sqrMagnitude > 0.0001f)
        {
            lastMoveDir = input;
        }

        float angle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (gearText != null)
        {
            gearText.text = "Gear: " + GearToString(currentGear);
        }
    }

    private void FixedUpdate()
    {
        float maxSpeed = GetSpeedForCurrentGear();
        Vector2 targetVelocity = input * maxSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? acceleration : deceleration;

        rb.velocity = Vector2.MoveTowards(rb.velocity, targetVelocity, rate * Time.fixedDeltaTime);
    }

    private float AxisToDirection(int axisValue)
    {
        if (axisValue < AxisNeutral - AxisDeadzone)
        {
            return -1f;
        }

        if (axisValue > AxisNeutral + AxisDeadzone)
        {
            return 1f;
        }

        return 0f;
    }

    private float GetSpeedForCurrentGear()
    {
        if (currentGear == GEAR_DRIVE_1) return drive1Speed;
        if (currentGear == GEAR_DRIVE_2) return drive2Speed;
        return 0f;
    }

    private string GearToString(int gear)
    {
        if (gear == GEAR_DRIVE_1) return "D1";
        if (gear == GEAR_DRIVE_2) return "D2";
        return "N";
    }
}
