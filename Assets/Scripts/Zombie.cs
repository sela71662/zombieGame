using System.Collections;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI; // AI, 내비게이션 시스템 관련 코드 가져오기

// 좀비 AI 구현
public class Zombie : LivingEntity
{
    public LayerMask whatIsTarget; // 추적 대상 레이어

    private LivingEntity targetEntity; // 추적 대상
    private NavMeshAgent navMeshAgent; // 경로 계산 AI 에이전트

    public ParticleSystem hitEffect; // 피격 시 재생할 파티클 효과
    public ParticleSystem bloodSprayEffect; // 혈흔 효과
    public AudioClip deathSound; // 사망 시 재생할 소리
    public AudioClip hitSound; // 피격 시 재생할 소리

    private Animator zombieAnimator; // 애니메이터 컴포넌트
    private AudioSource zombieAudioPlayer; // 오디오 소스 컴포넌트
    private Renderer zombieRenderer; // 렌더러 컴포넌트

    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점

    private float originalSpeed; // 원래 이동 속도 기억용

    // 추적할 대상이 존재하는지 알려주는 프로퍼티
    private bool hasTarget {
        get
        {
            // 추적할 대상이 존재하고, 대상이 사망하지 않았다면 true
            if (targetEntity != null && !targetEntity.dead)
            {
                return true;
            }

            // 그렇지 않다면 false
            return false;
        }
    }

    private void Awake() {
        // 게임 오브젝트에서 사용할 컴포넌트 가져오기
        navMeshAgent = GetComponent<NavMeshAgent>();
        zombieAnimator = GetComponent<Animator>();
        zombieAudioPlayer = GetComponent<AudioSource>();

        //렌더러 컴포넌트는 자식 게임 오브젝트에게 있으므로
        //GetComponentChildren() 메서드 사용
        zombieRenderer = GetComponentInChildren<Renderer>();
        
        // 초기 속도 저장
        if (navMeshAgent != null)
        {
            originalSpeed = navMeshAgent.speed;
        }
    }

    // 좀비 AI의 초기 스펙을 결정하는 셋업 메서드
    public void Setup(ZombieData zombieData) {

        //체력 설정
        startingHealth = zombieData.health;
        health = zombieData.health;
        //공격력 설정
        damage = zombieData.damage;
        //네비메시 에이전트의 이동 속도 설정
        navMeshAgent.speed = zombieData.speed;
        originalSpeed = zombieData.speed; // 다시 갱신
        //렌더러가 사용 중인 머테리얼의 컬러를 변경, 외형 색이 변함
        zombieRenderer.material.color = zombieData.skinColor;
        
    }

    // 느려짐 효과 적용 (비율, 예: 0.5면 50% 속도)
    public void ApplySlow(float speedMultiplier)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.speed = originalSpeed * speedMultiplier;
        }
    }

    // 느려짐 효과 제거 (원래 속도로 복구)
    public void RemoveSlow()
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.speed = originalSpeed;
        }
    }

    private void Start() {
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        StartCoroutine(UpdatePath());
    }

    private void Update() {
        // 추적 대상의 존재 여부에 따라 다른 애니메이션 재생
        zombieAnimator.SetBool("HasTarget", hasTarget);
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로 갱신
    private IEnumerator UpdatePath() {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            // 근처에 유인용 축음기가 있는지 먼저 확인 (플레이어를 쫓고 있더라도 축음기가 우선)
            bool lureFound = false;
            Collider[] lures = Physics.OverlapSphere(transform.position, 15f); // 축음기 유인 반경
            foreach (var lureCollider in lures)
            {
                GramophoneLure lure = lureCollider.GetComponent<GramophoneLure>();
                if (lure != null && !lure.dead)
                {
                    targetEntity = lure;
                    lureFound = true;
                    break;
                }
            }

            if (hasTarget)
            {
                //추적 대상 존재 : 경로를 갱신하고 AI 이동을 계속 진행
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetEntity.transform.position);
            }
            else if (!lureFound)
            {
                //추적 대상 없음 & 축음기도 없음 : AI이동 중지
                navMeshAgent.isStopped = true;

                // 20유닛 반경 내의 플레이어 탐색
                Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);

                for (int i = 0; i < colliders.Length; i++)
                {
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();

                    if (livingEntity != null && !livingEntity.dead)
                    {
                        targetEntity = livingEntity;
                        break;
                    }
                }
            }
            // 0.25초 주기로 처리 반복
                yield return new WaitForSeconds(0.25f);
        }
    }

    // 데미지를 입었을 때 실행할 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) {

        //아직 사망하지 않은 경우에만 피격 효과 재생
        if (!dead)
        {
            //공격 받은 지점과 방향으로 파티클 효과를 재생
            hitEffect.transform.position = hitPoint;
            hitEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            hitEffect.Play();

            // 혈흔 효과 재생
            if (bloodSprayEffect != null)
            {
                bloodSprayEffect.transform.position = hitPoint;
                bloodSprayEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
                bloodSprayEffect.Play();
            }

            //피격 효과음 재생
            zombieAudioPlayer.PlayOneShot(hitSound);
        }
        // LivingEntity의 OnDamage()를 실행하여 데미지 적용
        base.OnDamage(damage, hitPoint, hitNormal);
    }

    // 사망 처리
    public override void Die() {
        // LivingEntity의 Die()를 실행하여 기본 사망 처리 실행
        base.Die();

        Collider[] zombieColliders = GetComponents<Collider>();
        for (int i = 0; i < zombieColliders.Length; i++)
        {
            zombieColliders[i].enabled = false;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.enabled = false;

        zombieAnimator.SetTrigger("Die");
        zombieAudioPlayer.PlayOneShot(deathSound);
    }

    private void OnTriggerStay(Collider other) {

        if(!dead && Time.time >= lastAttackTime + timeBetAttack)
        {
            LivingEntity attackTarget = other.GetComponent<LivingEntity>();

            if (attackTarget != null && attackTarget == targetEntity){
                lastAttackTime = Time.time;

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;

                attackTarget.OnDamage(damage, hitPoint, hitNormal);
            }
        }
    }
}