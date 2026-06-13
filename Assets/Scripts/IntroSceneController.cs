using UnityEngine;
using UnityEngine.Playables; // 타임라인 이벤트 감지를 위해 필수!
using UnityEngine.Timeline; // 타임라인 프레임 속도 참조용
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!
using System.Collections; // 코루틴 사용을 위해 필수!

public class IntroSceneController : MonoBehaviour
{
    // 이동할 게임 씬의 이름을 인스펙터에서 적을 수 있게 만듭니다.
    public string gameSceneName = "Main";

    public Material grassMaterial; // 바닥 잔디 머티리얼 (Grass.mat 연결 필수)
    
    // 고정 초록색 (138, 181, 73)
    private Color introColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);

    [Header("Timeline Auto Transition")]
    public bool autoTransitionOnTimelineEnd = false; // 타임라인이 끝나면 자동으로 다음 씬으로 전환할지 여부

    // 엔딩 씬이 끝나서 인트로로 돌아갈 때 타임라인을 1200 프레임 지점부터 시작하게 만드는 플래그
    private static bool startFrom1200 = false;

    private void Start() {
        // 인트로 씬 시작 시 머티리얼을 무조건 초록색으로 초기화
        if (grassMaterial != null) {
            grassMaterial.SetColor("_BaseColor", introColor);
        }

        PlayableDirector director = FindObjectOfType<PlayableDirector>();
        bool jumped = false;

        if (director != null)
        {
            if (startFrom1200)
            {
                startFrom1200 = false; // 플래그 리셋
                jumped = true;

                double fps = 60.0;
                if (director.playableAsset is TimelineAsset timelineAsset)
                {
                    fps = timelineAsset.editorSettings.fps;
                }

                double targetTime = 1200.0 / fps;
                director.time = targetTime;
                director.Evaluate();
                director.Play();
            }
        }

        // 인트로 씬에서만 FadeImg 조절 및 버튼 활성화 처리 (엔딩 씬은 타임라인 연출을 훼손하지 않음)
        if (SceneManager.GetActiveScene().name == "Intro Scene")
        {
            GameObject fadeObj = GameObject.Find("FadeImg");
            if (fadeObj != null) {
                UnityEngine.UI.Image fadeImage = fadeObj.GetComponent<UnityEngine.UI.Image>();
                if (fadeImage != null) {
                    fadeImage.raycastTarget = false; // 즉시 버튼 클릭 차단 해제

                    if (jumped) {
                        // 엔딩에서 넘어와 1200 지점으로 점프한 경우: 블랙아웃 상태에서 서서히 밝아지는 페이드인
                        StartCoroutine(FadeInIntro(fadeImage, 1.0f));
                    } else {
                        // 일반적인 인트로 진입 시: 기존대로 2초 뒤 비활성화
                        StartCoroutine(DisableFadeObjectAfterDelay(fadeObj, 2f));
                    }
                }
            }
        }

        // 타임라인 자동 전환 설정이 켜진 경우, 씬 내 PlayableDirector 이벤트 바인딩
        if (autoTransitionOnTimelineEnd) {
            if (director != null) {
                director.stopped += OnTimelineStopped;
            } else {
                Debug.LogWarning("autoTransitionOnTimelineEnd가 활성화되었으나 PlayableDirector를 찾지 못했습니다.");
            }
        }
    }

    private void OnTimelineStopped(PlayableDirector director) {
        Debug.Log("엔딩 타임라인 재생 종료 - 인트로 씬으로 즉시 전환합니다.");
        startFrom1200 = true;
        GoTOIntro();
    }

    private void OnDestroy() {
        if (autoTransitionOnTimelineEnd) {
            PlayableDirector director = FindObjectOfType<PlayableDirector>();
            if (director != null) {
                director.stopped -= OnTimelineStopped;
            }
        }
    }

    private void GoTOIntro()
    {
        SceneManager.LoadScene("Intro Scene");
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 게임 종료 함수
    public void ExitGame()
    {
        Debug.Log("Game Exit Clicked");
        
        #if UNITY_EDITOR
            // 유니티 에디터에서 실행 중일 때
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 실제 빌드된 게임에서 실행 중일 때
            Application.Quit();
        #endif
    }

    private IEnumerator DisableFadeObjectAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    private IEnumerator FadeInIntro(UnityEngine.UI.Image fadeImage, float duration) {
        float elapsed = 0f;
        Color col = fadeImage.color;
        col.a = 1f;
        fadeImage.color = col;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            col.a = Mathf.Clamp01(1f - (elapsed / duration));
            fadeImage.color = col;
            yield return null;
        }
        
        col.a = 0f;
        fadeImage.color = col;
        fadeImage.gameObject.SetActive(false); // 완전히 투명해지면 비활성화
    }
}
