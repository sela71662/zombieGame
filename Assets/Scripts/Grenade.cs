using UnityEngine;

// 수류탄 아이템 클래스
public class Grenade : MonoBehaviour, IItem
{
    public int damage = 50; // 수류탄 데미지 (나중에 사용될 수 있음)
    public int score = 100; // 아이템 습득 시 추가될 점수

    public void Use(GameObject target)
    {
        // 전달받은 target 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        // PlayerShooter 컴포넌트가 존재한다면
        if (playerShooter != null)
        {
            // 플레이어의 수류탄 수량을 1 증가
            playerShooter.grenadeCount++;
        }

        // 사용되었으므로 자신을 파괴
        Destroy(gameObject);
    }
}
