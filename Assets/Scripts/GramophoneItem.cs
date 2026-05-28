using UnityEngine;

// 축음기 아이템 클래스
public class GramophoneItem : MonoBehaviour, IItem
{
    public int score = 150; // 습득 시 점수

    public void Use(GameObject target)
    {
        // 플레이어에게 축음기 수량 추가
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();
        if (playerShooter != null)
        {
            playerShooter.gramophoneCount++;
        }

        // 아이템 오브젝트 삭제
        Destroy(gameObject);
    }
}
