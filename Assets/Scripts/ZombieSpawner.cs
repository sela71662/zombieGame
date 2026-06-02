using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 좀비 게임 오브젝트를 주기적으로 생성 및 환경(눈) 제어
public class ZombieSpawner : MonoBehaviour {
    public Zombie zombiePrefab;
    public ZombieData[] zombieDatas;
    public Transform[] spawnPoints;

    [Header("Environment Settings")]
    public Material snowGrassMaterial; // 메인 게임용 머티리얼 (SnowGrass.mat 연결 필수)
    
    public Color startColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);
    public Color targetColor = new Color(220f / 255f, 239f / 255f, 253f / 255f, 1f);

    private List<Zombie> zombies = new List<Zombie>();
    private int wave;
    private bool isFinalSequenceStarted = false;

    private void Start() {
        // 게임 시작 시 메인용 머티리얼을 초록색으로 리셋
        if (snowGrassMaterial != null) {
            snowGrassMaterial.SetColor("_BaseColor", startColor);
        }
    }

    private void Update() {
        if (GameManager.instance != null && GameManager.instance.isGameover || isFinalSequenceStarted) return;

        if (zombies.Count <= 0 && wave < 7)
        {
            SpawnWave();
        }
        UpdateUI();
    }

    private void UpdateUI() {
        UIManager.instance.UpdateWaveText(wave, zombies.Count);
    }

    private void SpawnWave() {
        wave++;

        // 웨이브에 따라 SnowGrass의 색상만 변경
        if (snowGrassMaterial != null)
        {
            float t = (float)(wave - 1) / 6f;
            Color lerpedColor = Color.Lerp(startColor, targetColor, t);
            snowGrassMaterial.SetColor("_BaseColor", lerpedColor);
        }

        int spawnCount = Mathf.RoundToInt(wave * 1.5f);
        for (int i = 0; i < spawnCount; i++) CreateZombie();
    }

    private void CreateZombie() {
        ZombieData zombieData = zombieDatas[Random.Range(0, zombieDatas.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Zombie zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombie.Setup(zombieData);
        zombies.Add(zombie);

        zombie.onDeath += () => {
            zombies.Remove(zombie);
            // 마지막 7웨이브의 마지막 좀비가 죽는 '그 찰나'에 즉시 실행
            if (wave == 7 && zombies.Count == 0 && !isFinalSequenceStarted)
            {
                isFinalSequenceStarted = true;
                StartCoroutine(FinalKillSequence(zombie.transform.position));
            }
        };
        zombie.onDeath += () => Destroy(zombie.gameObject, 10f);
        zombie.onDeath += () => GameManager.instance.AddScore(100);
    }

    private IEnumerator FinalKillSequence(Vector3 targetPosition) {
        // 1. 마지막 일격 시점에 즉시 슬로우 모션 (0.15배속)
        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Camera mainCam = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (mainCam != null && player != null)
        {
            // 시네머신 브레인 해제
            MonoBehaviour brain = mainCam.GetComponent("CinemachineBrain") as MonoBehaviour;
            if (brain != null) brain.enabled = false;

            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;
            
            // 카메라 높이를 1m 더 올림 (기존 up * 2.0f -> 3.0f)
            Vector3 zoomPos = player.transform.position - player.transform.forward * 3.0f + Vector3.up * 3.0f;
            // 타겟 지점도 약간 위로 보정하여 전체 구도 안정화
            Vector3 lookTarget = Vector3.Lerp(player.transform.position, targetPosition, 0.5f) + Vector3.up * 1.2f;
            Quaternion zoomRot = Quaternion.LookRotation(lookTarget - zoomPos);

            float elapsed = 0f;
            float duration = 2.5f; // 줌인 2.5초

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                
                mainCam.transform.position = Vector3.Lerp(startPos, zoomPos, smoothT);
                mainCam.transform.rotation = Quaternion.Slerp(startRot, zoomRot, smoothT);
                yield return null;
            }
        }

        // 3. 줌인이 끝난 후 4초 동안 여운 유지 (요청 사항 반영)
        yield return new WaitForSecondsRealtime(4.0f);
        
        // 4. 시간 복구 및 엔딩 씬 전환
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene("Ending Scene");
    }
}
