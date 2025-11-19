using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [Range(1f, 20f)]
    public float followSpeed = 5.0f;

    [Header("Mouse Offset")]
    public bool enableMouseLook = true;

    [Range(0f, 1f)]
    public float lookAheadFactor = 0.3f;

    public float maxLookAheadDistance = 10f;

    [Header("Screen Shake")]
    public float shakeDecay = 5.0f;

    private Transform _target;
    private float _currentShakeIntensity = 0f;
    private float _initialZ;
    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _initialZ = transform.position.z;

        FindTarget();
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            FindTarget();
            if (_target == null) return;
        }

        Vector3 targetPos = _target.position;

        if (enableMouseLook)
        {
            Vector3 mouseOffset = CalculateMouseOffset(targetPos);
            targetPos += mouseOffset;
        }

        targetPos.z = _initialZ;

        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        if (_currentShakeIntensity > 0)
        {
            Vector3 shakeOffset = Random.insideUnitCircle * _currentShakeIntensity;
            smoothedPos += shakeOffset;

            _currentShakeIntensity = Mathf.MoveTowards(_currentShakeIntensity, 0f, shakeDecay * Time.deltaTime);
        }

        transform.position = smoothedPos;
    }

    private void FindTarget()
    {
        if (BattleController.instance != null && BattleController.instance.MC != null)
        {
            _target = BattleController.instance.MC.transform;
        }
    }

    private Vector3 CalculateMouseOffset(Vector3 playerPos)
    {
        Vector3 mouseScreenPos = Input.mousePosition;

        mouseScreenPos.z = Mathf.Abs(_cam.transform.position.z - playerPos.z);
        Vector3 mouseWorldPos = _cam.ScreenToWorldPoint(mouseScreenPos);

        Vector3 dirToMouse = mouseWorldPos - playerPos;

        dirToMouse.z = 0;

        if (dirToMouse.magnitude > maxLookAheadDistance)
        {
            dirToMouse = dirToMouse.normalized * maxLookAheadDistance;
        }

        return dirToMouse * lookAheadFactor;
    }

    public void TriggerShake(float intensity)
    {
        _currentShakeIntensity = Mathf.Max(_currentShakeIntensity, intensity);
    }
}