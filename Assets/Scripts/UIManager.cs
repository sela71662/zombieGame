using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드
using UnityEngine.UI; // UI 관련 코드

// 필요한 UI에 즉시 접근하고 변경할 수 있도록 허용하는 UI 매니저
public class UIManager : MonoBehaviour {
    // 싱글톤 접근용 프로퍼티
    public static UIManager instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<UIManager>();
            }

            return m_instance;
        }
    }

    private static UIManager m_instance; // 싱글톤이 할당될 변수

    public Text ammoText; // 탄약 표시용 텍스트
    public Text scoreText; // 점수 표시용 텍스트
    public Text waveText; // 적 웨이브 표시용 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화할 UI 
    public Text grenadeText;
    public Text gramophoneText;

    [Header("SnowMan Cooldown UI")]
    public Image snowManCooldownImage; // 쿨타임 표시용 원형 이미지 (Radial Fill)
    public CanvasGroup snowManDisplayGroup; // 아이콘의 흐림 처리를 위한 그룹

    public void UpdateSnowManCooldownUI(float fillAmount, bool isCooldown)
    {
        if (snowManCooldownImage != null)
        {
            snowManCooldownImage.fillAmount = fillAmount;
        }

        if (snowManDisplayGroup != null)
        {
            // 쿨타임 중이면 흐리게(0.3), 아니면 원래대로(1.0)
            snowManDisplayGroup.alpha = isCooldown ? 0.3f : 1.0f;
        }
    }

    public void UpdateGrenadeCount(int count)
    {
        grenadeText.text = count.ToString();
    }

    public void UpdateGramophoneCount(int count)
    {
        gramophoneText.text = count.ToString();
    }

    // 탄약 텍스트 갱신
    public void UpdateAmmoText(int magAmmo, int remainAmmo) {
        ammoText.text = magAmmo + "/" + remainAmmo;
    }

    // 점수 텍스트 갱신
    public void UpdateScoreText(int newScore) {
        scoreText.text = "Score : " + newScore;
    }

    // 적 웨이브 텍스트 갱신
    public void UpdateWaveText(int waves, int count) {
        waveText.text = "Wave : " + waves + "\nEnemy Left : " + count;
    }

    // 게임 오버 UI 활성화
    public void SetActiveGameoverUI(bool active) {
        gameoverUI.SetActive(active);
    }

    // 게임 재시작
    public void GameRestart() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}