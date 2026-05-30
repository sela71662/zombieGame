using UnityEngine;

// 주어진 Gun 오브젝트를 쏘거나 재장전
// 알맞은 애니메이션을 재생하고 IK를 사용해 캐릭터 양손이 총에 위치하도록 조정
public class PlayerShooter : MonoBehaviour {
    public Gun gun; // 사용할 총
    public Transform gunPivot; // 총 배치의 기준점
    public Transform leftHandMount; // 총의 왼쪽 손잡이, 왼손이 위치할 지점
    public Transform rightHandMount; // 총의 오른쪽 손잡이, 오른손이 위치할 지점

    public int grenadeCount = 0; // 보유한 수류탄 개수
    public GameObject throwableGrenadePrefab; // 투척용 수류탄 프리팹
    public float throwForce = 5f; // 수류탄 투척 힘

    public int gramophoneCount = 0; // 보유한 축음기 개수
    public GameObject gramophoneLurePrefab; // 설치용 축음기 프리팹

    public GameObject snowManPrefab; // 소환할 눈사람 프리팹
    public GameObject snowManExplosionEffect; // 눈사람 전용 폭발 이펙트
    public GameObject snowManSecondaryEffect; // 눈사람 보조 폭발 이펙트 (Ice Hit 등)
    public GameObject snowManSlowAuraEffect; // 눈사람 슬로우 오라 이펙트 (IceMagicEF 등)
    public GameObject snowManGroundIndicator; // 눈사람 바닥 표시기 (Quad 등)
    private GameObject currentSnowMan; // 현재 소환된 눈사람
    public float summonCooldown = 30f; // 소환 쿨타임
    private float lastSummonTime = -30f; // 마지막 소환 시점 (처음엔 바로 소환 가능하도록)

    private PlayerInput playerInput; // 플레이어의 입력(r버튼 눌렀는지 확인위해.재장전해야하니까)
    private Animator playerAnimator; // 애니메이터 컴포넌트

    private void Start() {
        // 사용할 컴포넌트들을 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable() {
        // 슈터가 활성화될 때 총도 함께 활성화
        gun.gameObject.SetActive(true);
    }
    
    private void OnDisable() {
        // 슈터가 비활성화될 때 총도 함께 비활성화
        gun.gameObject.SetActive(false);
    }

    private void Update() {
        // 입력을 감지하고 총 발사하거나 재장전
        if (playerInput.fire)
        {
            gun.Fire();
        }else if (playerInput.reload)
        {
            if (gun.Reload())
            {
                playerAnimator.SetTrigger("Reload");
            }
        }

        // 수류탄 투척 입력 감지
        if (playerInput.grenade && grenadeCount > 0)
        {
            Throw();
        }

        // 축음기 설치 입력 감지 - 쿨타임 없이 즉시 설치
        if (playerInput.lure && gramophoneCount > 0)
        {
            PlaceGramophone();
        }

        // 눈사람 소환 입력 감지 (T 키) - 30초 쿨타임 체크
        if (playerInput.summon && Time.time >= lastSummonTime + summonCooldown)
        {
            SummonSnowMan();
        }

        UpdateUI();
    }

    // 수류탄 투척 로직
    private void Throw() {
        grenadeCount--; // 개수 차감

        // 수류탄 생성 (머리 높이쯤에서 생성)
        GameObject grenade = Instantiate(throwableGrenadePrefab, transform.position + Vector3.up * 1.5f + transform.forward * 0.5f, transform.rotation);
        
        // 물리적인 힘 가하기
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 전방 대각선 위 방향으로 힘을 가함
            Vector3 forceDirection = (transform.forward + Vector3.up * 0.5f).normalized;
            rb.AddForce(forceDirection * throwForce, ForceMode.Impulse);
        }
    }

    // 축음기 설치 로직
    private void PlaceGramophone() {
        gramophoneCount--; // 개수 차감

        // 플레이어 앞 지면 근처에 축음기 생성
        Vector3 spawnPos = transform.position + transform.forward * 1.0f;
        Instantiate(gramophoneLurePrefab, spawnPos, transform.rotation);
    }

    // 눈사람 소환 로직
    private void SummonSnowMan() {
        lastSummonTime = Time.time; // 소환 시점 기록

        // 이미 소환된 눈사람이 있다면 제거
        if (currentSnowMan != null)
        {
            Destroy(currentSnowMan);
        }

        // 플레이어의 오른쪽 위치 계산 (약 0.5m 옆 - AI의 followOffset과 일치시킴)
        Vector3 spawnPos = transform.position + transform.right * 0.5f;
        
        // NavMesh 위의 가장 가까운 위치로 보정
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        
        // 눈사람 생성
        currentSnowMan = Instantiate(snowManPrefab, spawnPos, transform.rotation);
        
        // 눈사람에게 AI 스크립트 설정 (이미 프리팹에 붙어있으므로 GetComponent 사용)
        SnowManAI aiScript = currentSnowMan.GetComponent<SnowManAI>();
        if (aiScript == null)
        {
            aiScript = currentSnowMan.AddComponent<SnowManAI>();
        }
        
        aiScript.target = transform; // 따라갈 대상은 플레이어
        
        // 폭발 이펙트 연결 (눈사람 전용 이펙트가 있으면 그것을 사용, 없으면 수류탄 이펙트 사용)
        if (snowManExplosionEffect != null)
        {
            aiScript.explosionEffect = snowManExplosionEffect;
        }
        else if (throwableGrenadePrefab != null)
        {
            ThrowableGrenade grenade = throwableGrenadePrefab.GetComponent<ThrowableGrenade>();
            if (grenade != null)
            {
                aiScript.explosionEffect = grenade.explosionEffect;
            }
        }

        // 보조 폭발 이펙트 연결 (Ice Hit 등)
        if (snowManSecondaryEffect != null)
        {
            aiScript.secondaryExplosionEffect = snowManSecondaryEffect;
        }

        // 슬로우 오라 이펙트 연결 (IceMagicEF 등)
        if (snowManSlowAuraEffect != null)
        {
            aiScript.slowAuraEffectPrefab = snowManSlowAuraEffect;
        }

        // 바닥 표시기 연결 (Quad 등)
        if (snowManGroundIndicator != null)
        {
            aiScript.slowAuraGroundIndicatorPrefab = snowManGroundIndicator;
        }
    }

    
    // 탄약 및 수류탄 UI 갱신
    private void UpdateUI() {
        if (UIManager.instance != null)
        {
            // UI 매니저의 탄약 텍스트에 탄창의 탄약과 남은 전체 탄약을 표시
            if (gun != null)
            {
                UIManager.instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
            }
            
            // 수류탄 개수 UI 갱신
            UIManager.instance.UpdateGrenadeCount(grenadeCount);

            // 축음기 개수 UI 갱신
            UIManager.instance.UpdateGramophoneCount(gramophoneCount);

            // 눈사람 쿨타임 UI 갱신
            float timePassed = Time.time - lastSummonTime;
            float cooldownRatio = Mathf.Clamp01(1.0f - (timePassed / summonCooldown));
            bool isCooldown = timePassed < summonCooldown;
            UIManager.instance.UpdateSnowManCooldownUI(cooldownRatio, isCooldown);
        }
    }

    // 애니메이터의 IK 갱신
    private void OnAnimatorIK(int layerIndex) {

        gunPivot.position = playerAnimator.GetIKHintPosition(AvatarIKHint.RightElbow);  //총의 기준점을 오른

        //가중치를 100%로 설정하여 IK가 양손을 총의 손잡이에 위치하도록 조정
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandMount.position);//왼손 위치를 총의 왼쪽
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandMount.rotation);//왼손 회전을 총의 왼쪽

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandMount.rotation);

    }
}
