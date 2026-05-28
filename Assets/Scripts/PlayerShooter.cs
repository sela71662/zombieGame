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