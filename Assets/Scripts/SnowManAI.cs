using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// 눈사람 AI: NavMesh를 이용해 장애물을 피하며 플레이어를 따라다니고,
// 주변 좀비를 느리게 만들며, 20초 뒤 자폭함.
[RequireComponent(typeof(NavMeshAgent))]
public class SnowManAI : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // 따라다닐 대상 (플레이어)
    public Vector3 followOffset = new Vector3(0.5f, 0f, 0f); // 0.5m 밀착
    public float followSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Attack Settings (Slow Aura)")]
    public float slowRadius = 0.5f; // 슬로우 범위 반경 (지름 1m)
    public float slowCenterOffset = 1.5f; // 정면으로부터의 거리 (1.5m)
    public float slowAuraHeight = 1.0f; // 슬로우 오라 높이 (공중에 띄우기 위함)
    public float slowMultiplier = 0.5f; // 50% 감속 효과
    public GameObject slowAuraEffectPrefab; // 슬로우 오라 시각 이펙트 (공중)
    public GameObject slowAuraGroundIndicatorPrefab; // 슬로우 오라 바닥 표시기 (데칼/메쉬)
    private GameObject currentSlowAuraEffect; // 생성된 슬로우 오라 인스턴스
    private GameObject currentGroundIndicator; // 생성된 바닥 표시기 인스턴스
    private List<Zombie> slowedZombies = new List<Zombie>();

    [Header("Explosion Settings")]
    public float lifeTime = 20f; // 생존 시간
    public float explosionDamage = 300f; // 자폭 데미지
    public float explosionRadius = 3f; // 자폭 범위
    public GameObject explosionEffect; // 주 폭발 이펙트 (Icicle Bumb 등)
    public GameObject secondaryExplosionEffect; // 보조 폭발 이펙트 (Ice Hit 등)

    private NavMeshAgent navMeshAgent;
    private Transform bodyTransform;
    private Transform headTransform;
    private Vector3 lastPosition;

    private void Start()
    {
        // 1. 컴포넌트 및 자식 오브젝트 설정
        navMeshAgent = GetComponent<NavMeshAgent>();
        bodyTransform = transform.Find("Player Body");
        headTransform = transform.Find("Head");

        // 2. NavMeshAgent 세부 설정 (플레이어에게 바짝 붙기 위함)
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = followSpeed;
            navMeshAgent.acceleration = 60f; // 더 빠른 반응성
            navMeshAgent.angularSpeed = 0f; // 수동 회전 사용하므로 AI 회전은 끔
            navMeshAgent.updateRotation = false; // 수동 회전 사용
            navMeshAgent.stoppingDistance = 0.1f; // 매우 가깝게 정지
            navMeshAgent.radius = 0.1f; // 에이전트의 충돌 반경을 최소화하여 플레이어 방해 안 함
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // 플레이어와 겹쳐도 되도록 회피 끔
            
            // 정지 상태 해제 확인
            navMeshAgent.isStopped = false;

            // 만약 시작 위치가 NavMesh 위가 아니라면 가장 가까운 곳으로 워프
            if (!navMeshAgent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    navMeshAgent.Warp(hit.position);
                }
            }
        }

        // 3. 물리 엔진 간섭 차단
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 4. 기존 조작 스크립트 비활성화
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this && !(script is NavMeshAgent)) script.enabled = false;
        }

        // 5. 슬로우 오라 시각 효과 생성 (추가됨)
        if (slowAuraEffectPrefab != null)
        {
            currentSlowAuraEffect = Instantiate(slowAuraEffectPrefab, transform);
            // 정면 1.5m 위치 및 공중 높이 설정
            currentSlowAuraEffect.transform.localPosition = new Vector3(0, slowAuraHeight, slowCenterOffset);
            currentSlowAuraEffect.transform.localRotation = Quaternion.identity;
        }

        // 6. 바닥 표시기 생성 (추가됨)
        if (slowAuraGroundIndicatorPrefab != null)
        {
            currentGroundIndicator = Instantiate(slowAuraGroundIndicatorPrefab);
            
            // 물리적 충돌 방지: 바닥 표시기에 콜라이더가 있다면 비활성화
            Collider c = currentGroundIndicator.GetComponent<Collider>();
            if (c != null) c.enabled = false;
        }

        // 7. 자폭 타이머 시작
        StartCoroutine(SelfDestructRoutine());

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (target == null) return;

        // [이동 로직] NavMesh를 사용하여 목표 지점으로 이동
        Vector3 targetPosition = target.position + (target.right * followOffset.x) + (target.up * followOffset.y) + (target.forward * followOffset.z);
        
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(targetPosition);
        }

        // [비주얼 로직] 머리/몸통 고정 및 구르기 효과
        UpdateVisuals();

        // [슬로우 오라 로직]
        UpdateSlowAura();

        // [바닥 표시기 위치 갱신]
        if (currentGroundIndicator != null)
        {
            Vector3 auraCenter = transform.position + transform.forward * slowCenterOffset;
            float groundY = transform.position.y; // 기본값은 눈사람 높이

            // 슬로우 존 중심에서 위쪽 2m 지점에서 아래로 레이를 쏘아 실제 지면 높이 찾기
            RaycastHit hit;
            if (Physics.Raycast(auraCenter + Vector3.up * 2f, Vector3.down, out hit, 5f))
            {
                // 부딪힌 물체가 좀비나 플레이어가 아닐 때만 높이 갱신 (좀비 위로 올라타기 방지)
                if (hit.collider.GetComponentInParent<Zombie>() == null && hit.collider.GetComponentInParent<PlayerMovement>() == null)
                {
                    groundY = hit.point.y;
                }
            }

            // 바닥에 밀착시키되 Z-Fighting 방지를 위해 살짝 띄움 (0.05m)
            currentGroundIndicator.transform.position = new Vector3(auraCenter.x, groundY + 0.05f, auraCenter.z);
            // 바닥 평면에 맞게 회전 (Quad 기준 90도 눕힘)
            currentGroundIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            // 영역 크기를 slowRadius에 맞게 실시간 동기화 (지름 1m = 반지름 0.5m * 2)
            float diameter = slowRadius * 2f;
            currentGroundIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    private void UpdateVisuals()
    {
        // 머리와 몸통 위치 강제 고정
        if (bodyTransform != null) bodyTransform.localPosition = Vector3.zero;
        if (headTransform != null) headTransform.localPosition = new Vector3(0, 1.0f, 0);

        // 이동 거리에 따른 몸통 구르기
        float moveDistance = Vector3.Distance(transform.position, lastPosition);
        if (moveDistance > 0.001f)
        {
            Vector3 moveDir = (transform.position - lastPosition).normalized;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, moveDir).normalized;
            float rollAngle = (moveDistance / 0.3f) * Mathf.Rad2Deg;
            if (bodyTransform != null) bodyTransform.Rotate(rotationAxis, rollAngle, Space.World);
        }

        // 시선 방향 동기화
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * rotationSpeed);
        lastPosition = transform.position;
    }

    private void UpdateSlowAura()
    {
        Vector3 auraCenter = transform.position + transform.forward * slowCenterOffset;
        
        // 시각적 표시기의 위치와 크기가 이 auraCenter 및 slowRadius와 100% 일치해야 함
        
        // 1. 범위 내의 모든 콜라이더 탐색
        Collider[] colliders = Physics.OverlapSphere(auraCenter, slowRadius);
        List<Zombie> currentZombiesInRange = new List<Zombie>();

        foreach (var col in colliders)
        {
            Zombie zombie = col.GetComponent<Zombie>();
            // 좀비가 존재하고, 죽지 않았으며, 좀비의 위치가 정확히 원 안에 있는지 다시 한 번 체크
            if (zombie != null && !zombie.dead)
            {
                // 실제 거리 체크를 통해 시각적 원과 판정 범위를 엄격하게 일치시킴
                float distance = Vector3.Distance(new Vector3(auraCenter.x, 0, auraCenter.z), 
                                                 new Vector3(zombie.transform.position.x, 0, zombie.transform.position.z));
                
                if (distance <= slowRadius)
                {
                    currentZombiesInRange.Add(zombie);
                    if (!slowedZombies.Contains(zombie))
                    {
                        zombie.ApplySlow(slowMultiplier);
                        slowedZombies.Add(zombie);
                    }
                }
            }
        }

        // 2. 범위를 벗어난 좀비들 속도 복구
        for (int i = slowedZombies.Count - 1; i >= 0; i--)
        {
            Zombie z = slowedZombies[i];
            if (z == null || z.dead || !currentZombiesInRange.Contains(z))
            {
                if (z != null && !z.dead) z.RemoveSlow();
                slowedZombies.RemoveAt(i);
            }
        }
    }

    // 에디터 뷰에서 슬로우 범위를 선으로 표시 (디버깅용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 auraCenter = transform.position + transform.forward * slowCenterOffset;
        Gizmos.DrawWireSphere(auraCenter, slowRadius);
    }

    private IEnumerator SelfDestructRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Explode();
    }

    private void Explode()
    {
        // 주 폭발 이펙트 생성
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        // 보조 폭발 이펙트 생성 (추가됨)
        if (secondaryExplosionEffect != null)
        {
            GameObject effect = Instantiate(secondaryExplosionEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            Zombie zombie = col.GetComponent<Zombie>();
            if (zombie != null && !zombie.dead)
            {
                zombie.OnDamage(explosionDamage, col.transform.position, (col.transform.position - transform.position).normalized);
            }
        }

        foreach (var z in slowedZombies)
        {
            if (z != null && !z.dead) z.RemoveSlow();
        }
        
        // 바닥 표시기 제거 (추가됨)
        if (currentGroundIndicator != null)
        {
            Destroy(currentGroundIndicator);
        }

        Destroy(gameObject);
    }
}
