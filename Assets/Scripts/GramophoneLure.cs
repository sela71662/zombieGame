using UnityEngine;

// 설치된 축음기가 좀비를 유인하는 로직
public class GramophoneLure : LivingEntity
{
    public float lureRadius = 15f; // 유인 반경
    public float duration = 10f; // 유지 시간
    public GameObject musicEffect; // 음악 재생 시 파티클 효과 (선택사항)

    private void Start()
    {
        // 10초 뒤 파괴
        Destroy(gameObject, duration);
        
        // 음악 효과 재생 (있을 경우)
        if (musicEffect != null)
        {
            musicEffect.SetActive(true);
        }
    }

    // 축음기는 죽지 않지만 인터페이스 호환을 위해 유지
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 축음기는 데미지를 입지 않거나 필요시 파괴되도록 설정 가능
    }
}
