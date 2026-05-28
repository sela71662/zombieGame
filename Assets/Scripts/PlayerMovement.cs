using UnityEngine;

// 플레이어 캐릭터를 사용자 입력에 따라 움직이는 스크립트
public class PlayerMovement : MonoBehaviour {
    public float moveSpeed = 5f; // 앞뒤 움직임의 속도
    public float rotateSpeed = 180f; // 좌우 회전 속도


    private PlayerInput playerInput; // 플레이어 입력을 알려주는 컴포넌트
    private Rigidbody playerRigidbody; // 플레이어 캐릭터의 리지드바디
    private Animator playerAnimator; // 플레이어 캐릭터의 애니메이터

    private void Start() {
        // 사용할 컴포넌트들의 참조를 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
    }

    // FixedUpdate는 물리 갱신 주기에 맞춰 실행됨
    private void FixedUpdate() {
        // 물리 갱신 주기마다 움직임, 회전, 애니메이션 처리 실행
        Rotate();   //회전 실행
        Move(); //움직임 실행
        //입력값에 따라 애니메이터의 Move 파라미터값 변경
        playerAnimator.SetFloat("Move", playerInput.move);
    }

    // 입력값에 따라 캐릭터를 앞뒤로 움직임
    private void Move() {
        //상대적으로 이동할 거리 계산
        Vector3 moveDistance = playerInput.move * transform.forward * moveSpeed * Time.deltaTime;
        //리지드바디를 이용해 게임 오브젝트 위치 변경
        playerRigidbody.MovePosition(playerRigidbody.position + moveDistance);
    }

    // 입력값에 따라 캐릭터를 좌우로 회전 (마우스 방향 바라보기)
    private void Rotate() {
        // 메인 카메라에서 마우스 위치로 레이 생성
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 레이가 부딪힐 평면 설정 (바닥, y=0)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        float rayDistance;

        // 레이가 평면과 교차하는지 검사
        if (groundPlane.Raycast(ray, out rayDistance)) {
            // 교차한 지점의 월드 좌표 획득
            Vector3 point = ray.GetPoint(rayDistance);

            // 캐릭터에서 마우스 포인터 위치를 향하는 방향 벡터 계산
            Vector3 lookDirection = point - transform.position;
            lookDirection.y = 0f; // 수직 방향 회전 방지 (위아래로 고개 젖힘 방지)

            if (lookDirection != Vector3.zero) {
                // 회전값 계산
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // 리지드바디를 이용해 캐릭터 회전
                playerRigidbody.rotation = targetRotation;
            }
        }
    }
}