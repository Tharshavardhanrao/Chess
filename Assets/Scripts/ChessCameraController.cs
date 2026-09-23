using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChessCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Point the camera orbits around. Defaults to the board's transform if left empty.")]
    public Transform target;

    [Header("Distance / Zoom")]
    public float distance = 8f;
    public float minDistance = 3f;
    public float maxDistance = 20f;
    public float mouseZoomSpeed = 1.2f;
    public float touchZoomSpeed = 0.015f;

    [Header("Rotation")]
    [Tooltip("Starting horizontal angle (degrees).")]
    public float yaw = 0f;
    [Tooltip("Starting vertical angle (degrees). 90 = top-down, 0 = flat.")]
    public float pitch = 50f;
    public float minPitch = 15f;
    public float maxPitch = 85f;
    public float mouseRotateSpeed = 0.25f;
    public float touchRotateSpeed = 0.25f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.08f;

    [Header("Turn-Based Auto-Flip (Pass and Play)")]
    [Tooltip("Automatically rotate the camera to face whichever player's turn it is.")]
    public bool autoFlipPerTurn = true;
    [Tooltip("Camera yaw angle representing White's side of the board.")]
    public float whiteViewYaw = 0f;
    [Tooltip("Camera yaw angle representing Black's side of the board.")]
    public float blackViewYaw = 180f;
    [Tooltip("How long the smooth flip animation takes, in seconds.")]
    public float flipDuration = 0.7f;
    [Tooltip("Block manual drag/rotate input while the flip animation is playing.")]
    public bool blockInputDuringFlip = true;

    private Camera cam;
    private Vector3 velocity;

    // Mouse drag tracking
    private bool mouseDragging = false;
    private Vector2 lastMousePos;

    // Touch drag tracking
    private bool touchDragging = false;
    private Vector2 lastTouchPos;

    // Pinch tracking
    private bool pinching = false;
    private float lastPinchDistance;

    // Flip animation state
    private Coroutine flipCoroutine;
    private bool isFlipping = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (target == null)
        {
            ChessBoardGenerator board = FindObjectOfType<ChessBoardGenerator>();
            if (board != null) target = board.transform;
        }

        ApplyImmediate();
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        bool inputAllowed = !(isFlipping && blockInputDuringFlip);
        if (inputAllowed)
        {
            HandleMouse();
            HandleTouch();
        }

        Vector3 desiredPos = ComputePosition();
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, positionSmoothTime);
        transform.LookAt(target.position);
    }

    // ------------------------------------------------------------------
    // Public API — call this whenever the active turn changes.
    // ------------------------------------------------------------------
    public void SetViewForTurn(ChessPieceColor color)
    {
        if (!autoFlipPerTurn) return;

        float targetYaw = color == ChessPieceColor.White ? whiteViewYaw : blackViewYaw;

        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        flipCoroutine = StartCoroutine(SmoothFlip(targetYaw, flipDuration));
    }

    // Snap instantly with no animation (e.g. when a new game starts).
    public void SnapViewForTurn(ChessPieceColor color)
    {
        if (!autoFlipPerTurn) return;
        yaw = color == ChessPieceColor.White ? whiteViewYaw : blackViewYaw;
    }

    private IEnumerator SmoothFlip(float targetYaw, float duration)
    {
        isFlipping = true;

        float startYaw = yaw;
        float delta = Mathf.DeltaAngle(startYaw, targetYaw); // shortest signed path
        float elapsed = 0f;

        if (duration <= 0f)
        {
            yaw = targetYaw;
            isFlipping = false;
            flipCoroutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t); // smoothstep easing
            yaw = startYaw + delta * smoothT;
            yield return null;
        }

        yaw = targetYaw;
        isFlipping = false;
        flipCoroutine = null;
    }

    private void HandleMouse()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f && !overUI)
        {
            distance -= scroll * mouseZoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (overUI) return;
            mouseDragging = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
        }

        if (mouseDragging && Input.GetMouseButton(0))
        {
            Vector2 current = Input.mousePosition;
            Vector2 delta = current - lastMousePos;
            lastMousePos = current;

            yaw += delta.x * mouseRotateSpeed;
            pitch -= delta.y * mouseRotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            pinching = false;
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                {
                    touchDragging = false;
                    return;
                }
                touchDragging = true;
                lastTouchPos = t.position;
            }
            else if (touchDragging && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
            {
                Vector2 delta = t.position - lastTouchPos;
                lastTouchPos = t.position;

                yaw += delta.x * touchRotateSpeed;
                pitch -= delta.y * touchRotateSpeed;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchDragging = false;
            }
        }
        else if (Input.touchCount >= 2)
        {
            touchDragging = false;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float currentPinchDistance = Vector2.Distance(t0.position, t1.position);

            if (!pinching)
            {
                pinching = true;
                lastPinchDistance = currentPinchDistance;
            }
            else
            {
                float delta = currentPinchDistance - lastPinchDistance;
                lastPinchDistance = currentPinchDistance;

                distance -= delta * touchZoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
        else
        {
            touchDragging = false;
            pinching = false;
        }
    }

    private Vector3 ComputePosition()
    {
        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        float horizontalRadius = distance * Mathf.Cos(pitchRad);
        float x = horizontalRadius * Mathf.Sin(yawRad);
        float z = horizontalRadius * Mathf.Cos(yawRad);
        float y = distance * Mathf.Sin(pitchRad);

        return target.position + new Vector3(x, y, z);
    }

    private void ApplyImmediate()
    {
        if (target == null) return;
        transform.position = ComputePosition();
        transform.LookAt(target.position);
    }
}