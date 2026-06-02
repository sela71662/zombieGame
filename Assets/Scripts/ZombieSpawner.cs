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
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

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

        yield return new WaitForSecondsRealtime(2.5f);
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene("Ending Scene");
    }
}
