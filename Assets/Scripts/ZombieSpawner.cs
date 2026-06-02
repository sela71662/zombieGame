using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가

// 좀비 게임 오브젝트를 주기적으로 생성
public class ZombieSpawner : MonoBehaviour {
    public Zombie zombiePrefab; // 생성할 좀비 원본 프리팹

    public ZombieData[] zombieDatas; // 사용할 좀비 셋업 데이터들
    public Transform[] spawnPoints; // 좀비 AI를 소환할 위치들

    public Material grassMaterial; // 바닥 잔디 머티리얼
    
    // 시작 색상 (138, 181, 73)
    public Color startColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);
    // 목표 색상 (220, 239, 253)
    public Color targetColor = new Color(220f / 255f, 239f / 255f, 253f / 255f, 1f);

    private List<Zombie> zombies = new List<Zombie>(); // 생성된 좀비들을 담는 리스트
    private int wave; // 현재 웨이브
    private bool isFinalSequenceStarted = false; // 최종 연출 시작 여부

    private void Start() {
        // 시작 시 머티리얼 색상 초기화 (URP 속성 직접 제어)
        if (grassMaterial != null) {
            grassMaterial.SetColor("_BaseColor", startColor);
        }
    }

    private void Update() {
        // 게임 오버 상태이거나 최종 연출 중일 때는 생성하지 않음
        if (GameManager.instance != null && GameManager.instance.isGameover || isFinalSequenceStarted)
        {
            return;
        }

        // 좀비를 모두 물리친 경우 다음 스폰 실행
        // 7웨이브까지만 진행
        if (zombies.Count <= 0 && wave < 7)
        {
            SpawnWave();
        }

        // UI 갱신
        UpdateUI();
    }

    // 웨이브 정보를 UI로 표시
    private void UpdateUI() {
        // 현재 웨이브와 남은 적 수 표시
        UIManager.instance.UpdateWaveText(wave, zombies.Count);
    }

    // 현재 웨이브에 맞춰 좀비들을 생성
    private void SpawnWave() {
        wave++;

        // 머티리얼 색상 변경 (1웨이브는 startColor, 7웨이브에서 targetColor 도달)
        if (grassMaterial != null)
        {
            float t = (float)(wave - 1) / 6f; // 1웨이브: 0, 7웨이브: 1
            Color lerpedColor = Color.Lerp(startColor, targetColor, t);
            grassMaterial.SetColor("_BaseColor", lerpedColor);
        }

        int spawnCount = Mathf.RoundToInt(wave * 1.5f);

        for (int i = 0; i < spawnCount; i++)
        {
            CreateZombie();
        }
    }

    // 좀비를 생성하고 생성한 좀비에게 추적할 대상을 할당
    private void CreateZombie() {
        ZombieData zombieData = zombieDatas[Random.Range(0, zombieDatas.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Zombie zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

        zombie.Setup(zombieData);
        zombies.Add(zombie);

        zombie.onDeath += () => {
            zombies.Remove(zombie);
            // 7웨이브이고 모든 좀비를 처치했을 때 최종 연출 시작
            if (wave == 7 && zombies.Count == 0 && !isFinalSequenceStarted)
            {
                isFinalSequenceStarted = true;
                StartCoroutine(FinalKillSequence(zombie.transform.position));
            }
        };
        zombie.onDeath += () => Destroy(zombie.gameObject, 10f);
        zombie.onDeath += () => GameManager.instance.AddScore(100);
    }

    // 최종 킬 연출 코루틴
    private IEnumerator FinalKillSequence(Vector3 targetPosition) {
        // 1. 슬로우 모션 시작
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. 카메라 줌인 연출
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;
            
            Vector3 zoomPos = targetPosition + Vector3.up * 2.0f - mainCam.transform.forward * 3.0f;
            Quaternion zoomRot = Quaternion.LookRotation((targetPosition + Vector3.up * 0.5f) - zoomPos);

            float elapsed = 0f;
            float duration = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                mainCam.transform.position = Vector3.Lerp(startPos, zoomPos, t);
                mainCam.transform.rotation = Quaternion.Slerp(startRot, zoomRot, t);
                yield return null;
            }
        }

        // 3. 잠시 대기 후 엔딩 씬으로 전환
        yield return new WaitForSecondsRealtime(2.5f);
        
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene("Ending Scene");
    }
}
