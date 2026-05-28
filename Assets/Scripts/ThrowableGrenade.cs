using UnityEngine;

// 투척된 수류탄의 동작을 제어하는 스크립트
public class ThrowableGrenade : MonoBehaviour
{
    public float damage = 100f; // 폭발 데미지
    public float explosionRadius = 5f; // 폭발 반경
    public float fuseTime = 2f; // 기폭 시간
    public GameObject explosionEffect; // 폭발 이펙트 프리팹

    private void Start()
    {
        // fuseTime 이후에 Explode 메서드 실행
        Invoke("Explode", fuseTime);
    }

    private void Explode()
    {
        // 폭발 이펙트 재생
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        // 폭발 반경 내의 모든 콜라이더를 찾음
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            // IDamageable 인터페이스를 가진 컴포넌트인지 확인
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                // 데미지 입히기
                target.OnDamage(damage, hit.transform.position, (hit.transform.position - transform.position).normalized);
            }
        }

        // 수류탄 오브젝트 삭제
        Destroy(gameObject);
    }
}
