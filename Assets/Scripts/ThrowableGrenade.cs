using UnityEngine;

// 투척된 수류탄의 동작을 제어하는 스크립트
public class ThrowableGrenade : MonoBehaviour
{
    public float damage = 100f; // 폭발 데미지
    public float explosionRadius = 5f; // 폭발 반경
    public float fuseTime = 2f; // 기폭 시간
    public GameObject explosionEffect; // 폭발 이펙트 프리팹
    public AudioClip explosionSound; // 폭발 사운드 (Explosion3)

    private void Start()
    {
        // fuseTime 이후에 Explode 메서드 실행
        Invoke("Explode", fuseTime);
    }

    private void Explode()
    {
        // 폭발 사운드 재생 (2D 강제 재생 테스트)
        if (explosionSound != null)
        {
            Debug.Log("폭발 사운드 2D 재생 시도: " + explosionSound.name);
            
            // 임시 오디오 오브젝트 생성
            GameObject tempAudio = new GameObject("TempExplosionAudio");
            tempAudio.transform.position = transform.position;
            AudioSource aSource = tempAudio.AddComponent<AudioSource>();
            aSource.clip = explosionSound;
            aSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D (2D로 설정하여 거리 무시)
            aSource.volume = 1.0f;
            aSource.playOnAwake = false;
            aSource.Play();
            
            // 재생이 끝나면 오브젝트 파괴
            Destroy(tempAudio, explosionSound.length);
        }
        else
        {
            Debug.LogWarning("폭발 사운드가 ThrowableGrenade 스크립트에 할당되지 않았습니다!");
        }

        // 폭발 이펙트 재생 및 자동 삭제 설정
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, transform.rotation);
            Destroy(effect, 2f); // 2초 뒤 이펙트 삭제 (파티클 길이에 맞춰 조절)
        }

        // 폭발 반경 내의 모든 콜라이더를 찾음
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            // 플레이어인 경우 데미지 무시
            if (hit.CompareTag("Player"))
            {
                continue;
            }

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
