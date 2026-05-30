using UnityEngine;

// 소환된 눈사람이 플레이어를 완벽하게 따라다니게 만드는 최종 스크립트
public class SnowManFollow : MonoBehaviour
{
    public Transform target; // 따라다닐 대상 (플레이어)
    public Vector3 offset = new Vector3(0.5f, 0f, 0f); // 플레이어 기준 상대 위치 (오른쪽 0.5m)
    public float followSpeed = 5f; // 따라가는 속도
    public float rotationSpeed = 10f; // 회전 속도

    private Transform bodyTransform;
    private Transform headTransform;
    private Vector3 lastPosition;

    private void Start()
    {
        // 1. 자식 오브젝트(몸통과 머리)를 정확하게 찾아내어 레퍼런스 확보
        // 프리팹 구조상 이름이 "Player Body"와 "Head"인 것을 찾습니다.
        bodyTransform = transform.Find("Player Body");
        headTransform = transform.Find("Head");

        // 2. 물리 엔진 간섭을 완벽히 차단 (부들거림, 튕김 현상 방지)
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 3. 기존의 다른 조작 스크립트(RollControl 등)가 켜져 있으면 로직이 꼬이므로 모두 끕니다.
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (target == null) return;

        // [이동 로직]
        // 플레이어의 현재 위치와 방향을 기준으로 목표 지점(오른쪽 1.5m)을 계산합니다.
        Vector3 targetPosition = target.position + (target.right * offset.x) + (target.up * offset.y) + (target.forward * offset.z);
        
        // 눈사람의 전체(루트) 위치를 부드럽게 목표 지점으로 이동시킵니다.
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // [밀착 고정 로직]
        // 머리와 몸통이 따로 노는 것을 방지하기 위해 매 프레임 위치를 강제로 동기화합니다.
        if (bodyTransform != null)
        {
            // 몸통을 부모(루트) 위치에 딱 붙입니다.
            bodyTransform.localPosition = Vector3.zero;
        }
        
        if (headTransform != null)
        {
            // 머리를 몸통 위(약 1m 위)에 고정합니다. (프리팹 기본 높이에 맞춤)
            headTransform.localPosition = new Vector3(0, 1.0f, 0);
        }

        // [구르기 로직]
        // 실제로 이동한 거리만큼 몸통 메쉬만 데굴데굴 굴려줍니다.
        float moveDistance = Vector3.Distance(transform.position, lastPosition);
        if (moveDistance > 0.001f)
        {
            Vector3 moveDir = (transform.position - lastPosition).normalized;
            // 이동 방향에 수직인 회전축(바퀴축 같은 역할)을 계산합니다.
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, moveDir).normalized;
            
            // 이동 거리에 따른 회전 각도 계산 (0.3m를 구르면 한 바퀴 도는 식)
            float rollAngle = (moveDistance / 0.3f) * Mathf.Rad2Deg;
            
            if (bodyTransform != null)
            {
                // 몸통만 회전축을 기준으로 굴립니다.
                bodyTransform.Rotate(rotationAxis, rollAngle, Space.World);
            }
        }

        // [회전 로직]
        // 전체적인 눈사람의 시선 방향은 플레이어가 보는 방향을 따라갑니다.
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * rotationSpeed);

        lastPosition = transform.position;
    }
}
