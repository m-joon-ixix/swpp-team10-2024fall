using UnityEngine;
using System.Collections;

public class BossStagePlayer : PlayerBase
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float rotationSpeed = 5f; // 회전 보간 속도
    private bool isSpinning = false;
    private float playerSpinDuration = 3f; // 한 바퀴 도는 데 걸리는 시간

    [Header("Movement Constraints")]
    public Vector3 areaMin = new Vector3(-10f, 0.8f, -10f);
    public Vector3 areaMax = new Vector3(10f, 0.8f, 10f);
    public float minDistanceFromBoss = 2f; // 원점으로부터 최소 거리 설정

    [Header("Boss Settings")]
    public Transform bossTransform;
    private Rigidbody rb;
    private const float initialPitch = 20f;
    private Quaternion lastRotation;

    [Header("Projectile Settings")]
    public float projectileSpeed = 10.0f;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    private Camera mainCamera;

    [Header("Audio Settings")]
    public AudioClip obstacleCollisionSound;
    [Range(0f, 1f)] public float collisionVolume = 0.5f;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        mainCamera = Camera.main;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 초기 회전 저장
        lastRotation = Quaternion.Euler(initialPitch, 0f, 0f);
        rb.rotation = lastRotation;
    }

    private void FixedUpdate()
    {
        if (!isSpinning && enableKeys)
        {
            MovePlayer();
            RotatePlayer();
        }
    }

    private void Update()
    {
        // Fire Laser
        if (Input.GetKeyDown(KeyCode.Mouse0) && enableKeys)
        {
            FireLaser();
        }
    }

    private void MovePlayer()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // 좌우 이동 입력
        float moveVertical = Input.GetAxis("Vertical");     // 전후 이동 입력

        // 보스의 위치 (보스가 없으면 원점 사용)
        Vector3 bossPosition = bossTransform != null ? bossTransform.position : Vector3.zero;

        // 방향 벡터 계산
        Vector3 directionToBoss = bossPosition - rb.position;
        directionToBoss.y = 0f; // 수직 방향 무시하여 수평 방향만 고려

        Vector3 forwardDirection;
        Vector3 rightDirection;

        if (directionToBoss.sqrMagnitude > 0.0001f)
        {
            forwardDirection = directionToBoss.normalized;
            rightDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized; // 오른쪽 방향 계산
        }
        else
        {
            // 플레이어가 보스와 같은 위치에 있을 경우, 임의의 방향 설정
            forwardDirection = Vector3.forward;
            rightDirection = Vector3.right;
        }

        // 이동 벡터 계산
        Vector3 movement = (forwardDirection * moveVertical + rightDirection * moveHorizontal) * speed * Time.fixedDeltaTime;

        // 새로운 위치 계산
        Vector3 newPosition = rb.position + movement;

        // 이동 영역 제한
        newPosition.x = Mathf.Clamp(newPosition.x, areaMin.x, areaMax.x);
        newPosition.z = Mathf.Clamp(newPosition.z, areaMin.z, areaMax.z);
        newPosition.y = rb.position.y; // Y좌표 고정

        // 보스로부터 최소 거리 유지
        Vector3 offsetFromBoss = newPosition - bossPosition;
        float distanceFromBoss = offsetFromBoss.magnitude;

        if (distanceFromBoss < minDistanceFromBoss)
        {
            // 플레이어가 최소 거리 내로 들어가려는 경우
            // 움직임이 보스 방향인지 확인
            Vector3 attemptedMovement = newPosition - rb.position;
            Vector3 toBoss = bossPosition - rb.position;
            toBoss.y = 0f;

            float dot = Vector3.Dot(attemptedMovement.normalized, toBoss.normalized);

            if (dot > 0f && moveVertical > 0f)
            {
                // 움직임이 보스 방향으로 향하고 있고, 전진 입력인 경우
                // 움직임을 취소 (이동하지 않음)
                movement = Vector3.zero;
                newPosition = rb.position;
            }
            else
            {
                // 움직임이 보스에서 멀어지는 방향이거나, 전진 입력이 아님
                // 최소 거리를 유지하도록 위치 조정
                offsetFromBoss = offsetFromBoss.normalized * minDistanceFromBoss;
                newPosition = bossPosition + offsetFromBoss;
            }
        }

        rb.MovePosition(newPosition);
    }

    private void RotatePlayer()
    {
        // 보스의 위치 (보스가 없으면 원점 사용)
        Vector3 bossPosition = bossTransform != null ? bossTransform.position : Vector3.zero;

        // 플레이어가 항상 보스를 바라보도록 회전 계산
        Vector3 directionToBoss = bossPosition - rb.position;
        directionToBoss.y = 0f; // 수직 방향 무시하여 수평 회전만 고려

        if (directionToBoss.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToBoss);

            // 초기 X축 기울기(20도)를 적용
            Quaternion pitchRotation = Quaternion.Euler(initialPitch, 0f, 0f);

            Quaternion targetRotation = lookRotation * pitchRotation;

            // 회전을 부드럽게 보간하여 급격한 회전 변화 방지
            Quaternion newRotation = Quaternion.Slerp(lastRotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(newRotation);

            // 마지막 회전 업데이트
            lastRotation = newRotation;
        }
        else
        {
            // 플레이어가 보스와 같은 위치에 있을 경우, 회전 변화 없음
            // 이전 회전을 유지
            rb.MoveRotation(lastRotation);
        }
    }

    // Player Attack
    public override void FireLaser()
    {
        if (laserFireSound != null)
        {
            AudioSource.PlayClipAtPoint(laserFireSound, transform.position, laserVolume);
        }
        // Center shot always fires
        SpawnProjectile(projectileSpawnPoint.position);
    }

    void SpawnProjectile(Vector3 spawnPosition)
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 direction;

        // Convert to a ray from the camera
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // Perform the raycast
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Get the hit point on the mesh
            Vector3 targetPoint = hit.point;

            // Calculate the direction from the spawn point to the target point
            direction = (targetPoint - spawnPosition).normalized;
        }
        else
        {
            mousePosition.z = 16f; ; // Distance from camera to plane
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            direction = (worldPoint - spawnPosition).normalized;
        }
            
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition + Vector3.forward, Quaternion.identity);
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.velocity = direction * projectileSpeed;
        }
    }

    protected override void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            Destroy(other.gameObject);
            if (isBlinking) return;
            if (obstacleCollisionSound != null)
            {
                AudioSource.PlayClipAtPoint(obstacleCollisionSound, gameObject.transform.position, collisionVolume);
            }
            GameManager.inst.RemoveLife();
            if (GameManager.inst.GetLife() > 0)
                StartCoroutine(Blink());
        }
    }

    // 코루틴을 통해 플레이어가 제자리에서 한 바퀴 돌도록 구현
    public IEnumerator SpinInPlace()
    {
        if (isSpinning) yield break; // 이미 회전 중이라면 중복 실행 방지
        isSpinning = true;

        // 카메라를 플레이어에게 고정하도록 BossStageCamera에 요청
        BossStageCamera cameraScript = FindObjectOfType<BossStageCamera>();
        if (cameraScript != null)
        {
            StartCoroutine(cameraScript.FixCameraOnPlayerDuringSpin());
        }

        // 카메라 이동 완료 후 잠시 대기
        yield return new WaitForSeconds(1.5f); // 추가 대기 시간

        float elapsed = 0f;

        Quaternion initialRotation = transform.rotation;

        while (elapsed < playerSpinDuration)
        {
            float angle = (elapsed / playerSpinDuration) * 360f; // 360도 회전
            Quaternion targetRotation = Quaternion.Euler(0, angle, 0) * initialRotation;

            rb.MoveRotation(targetRotation);
            elapsed += Time.deltaTime;

            yield return null;
        }

        rb.MoveRotation(initialRotation);
        isSpinning = false;
    }

    // 플레이어가 회전 중인지 여부를 반환
    public bool IsSpinning()
    {
        return isSpinning;
    }
}
