using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 좀비 게임 오브젝트를 주기적으로 생성 및 환경(눈) 제어
public class ZombieSpawner : MonoBehaviour {
    // 싱글톤 접근용 프로퍼티
    public static ZombieSpawner instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<ZombieSpawner>();
            }
            return m_instance;
        }
    }
    private static ZombieSpawner m_instance;

    public Zombie zombiePrefab;
    public ZombieData[] zombieDatas;
    public Transform[] spawnPoints;

    [Header("Environment Settings")]
    public Material snowGrassMaterial; // 메인 게임용 머티리얼 (SnowGrass.mat 연결 필수)
    public Light directionalLight; // 씬의 Directional Light
    public float darkLightIntensity = 0.2f; // 어두운 웨이브일 때의 빛 밝기
    
    public Color startColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);
    public Color targetColor = new Color(220f / 255f, 239f / 255f, 253f / 255f, 1f);

    private List<Zombie> zombies = new List<Zombie>(); // 웨이브 진행을 체크하는 좀비 리스트
    private int wave; // 현재 웨이브
    private bool isFinalSequenceStarted = false; // 최종 연출 시작 여부

    private float originalLightIntensity; // 원래 빛 밝기
    private List<int> darkWaves = new List<int>(); // 어두워질 웨이브 목록

    private void Start() {
        if (m_instance == null) m_instance = this;

        // 게임 시작 시 메인용 머티리얼을 초록색으로 리셋
        if (snowGrassMaterial != null) {
            snowGrassMaterial.SetColor("_BaseColor", startColor);
        }

        // 빛 초기 밝기 저장
        if (directionalLight != null) {
            originalLightIntensity = directionalLight.intensity;
        }

        // 2~7 웨이브 중 2개의 어두운 웨이브 랜덤 선택
        while (darkWaves.Count < 2) {
            int randomWave = Random.Range(2, 8); // 2~7
            if (!darkWaves.Contains(randomWave)) {
                darkWaves.Add(randomWave);
            }
        }
    }

    private void Update() {
        if (GameManager.instance != null && GameManager.instance.isGameover || isFinalSequenceStarted) return;

        // 리스트에 있는 좀비들이 모두 죽으면 다음 웨이브
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

        // 머티리얼 색상 변경
        if (snowGrassMaterial != null)
        {
            float t = (float)(wave - 1) / 6f;
            Color lerpedColor = Color.Lerp(startColor, targetColor, t);
            snowGrassMaterial.SetColor("_BaseColor", lerpedColor);
        }

        // 어두운 웨이브 연출 처리
        if (directionalLight != null) {
            float targetIntensity = darkWaves.Contains(wave) ? darkLightIntensity : originalLightIntensity;
            StartCoroutine(ChangeLightIntensity(targetIntensity, 2f));
        }

        int spawnCount = Mathf.RoundToInt(wave * 1.5f);
        for (int i = 0; i < spawnCount; i++) CreateZombie();
    }

    // 빛 밝기를 서서히 바꾸는 코루틴
    private IEnumerator ChangeLightIntensity(float targetIntensity, float duration) {
        float startIntensity = directionalLight.intensity;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            directionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
        directionalLight.intensity = targetIntensity;
    }

    private void CreateZombie() {
        ZombieData zombieData = zombieDatas[Random.Range(0, zombieDatas.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Zombie zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombie.Setup(zombieData);
        
        // 기본 좀비는 항상 웨이브 트래킹에 포함
        RegisterZombie(zombie, true);
    }

    // 좀비 등록 로직
    public void RegisterZombie(Zombie zombie, bool trackForWave) {
        // 마지막 웨이브(7)라면 분열된 좀비(trackForWave=false인 녀석들)도 강제로 트래킹 목록에 넣음
        bool shouldTrack = trackForWave || (wave == 7);

        if (shouldTrack)
        {
            zombies.Add(zombie);
            zombie.onDeath += () => {
                zombies.Remove(zombie);
                // 모든 좀비(7웨이브에서는 분열 좀비 포함)가 죽었을 때만 엔딩 시작
                if (wave == 7 && zombies.Count == 0 && !isFinalSequenceStarted)
                {
                    isFinalSequenceStarted = true;
                    StartCoroutine(FinalKillSequence(zombie.transform.position));
                }
            };
        }

        // 공통 처리
        zombie.onDeath += () => Destroy(zombie.gameObject, 10f);
        zombie.onDeath += () => GameManager.instance.AddScore(100);
    }

    private IEnumerator FinalKillSequence(Vector3 targetPosition) {
        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Camera mainCam = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (mainCam != null && player != null)
        {
            MonoBehaviour brain = mainCam.GetComponent("CinemachineBrain") as MonoBehaviour;
            if (brain != null) brain.enabled = false;

            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;
            
            Vector3 zoomPos = player.transform.position - player.transform.forward * 4.0f + Vector3.up * 3.5f;
            Vector3 lookTarget = Vector3.Lerp(player.transform.position, targetPosition, 0.5f) + Vector3.up * 1.2f;
            Quaternion zoomRot = Quaternion.LookRotation(lookTarget - zoomPos);

            float elapsed = 0f;
            float duration = 2.5f;

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

        yield return new WaitForSecondsRealtime(4.0f);
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene("Ending Scene");
    }
}
