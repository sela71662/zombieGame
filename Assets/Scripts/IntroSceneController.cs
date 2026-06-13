using UnityEngine;
using UnityEngine.Playables; // 타임라인 이벤트 감지를 위해 필수!
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!

public class IntroSceneController : MonoBehaviour
{
    // 이동할 게임 씬의 이름을 인스펙터에서 적을 수 있게 만듭니다.
    public string gameSceneName = "Main";

    public Material grassMaterial; // 바닥 잔디 머티리얼 (Grass.mat 연결 필수)
    
    // 고정 초록색 (138, 181, 73)
    private Color introColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);

    [Header("Timeline Auto Transition")]
    public bool autoTransitionOnTimelineEnd = false; // 타임라인이 끝나면 자동으로 다음 씬으로 전환할지 여부

    private void Start() {
        // 인트로 씬 시작 시 머티리얼을 무조건 초록색으로 초기화
        if (grassMaterial != null) {
            grassMaterial.SetColor("_BaseColor", introColor);
        }

        // 타임라인 자동 전환 설정이 켜진 경우, 씬 내 PlayableDirector 이벤트 바인딩
        if (autoTransitionOnTimelineEnd) {
            PlayableDirector director = FindObjectOfType<PlayableDirector>();
            if (director != null) {
                director.stopped += OnTimelineStopped;
            } else {
                Debug.LogWarning("autoTransitionOnTimelineEnd가 활성화되었으나 PlayableDirector를 찾지 못했습니다.");
            }
        }
    }

    private void OnTimelineStopped(PlayableDirector director) {
        Debug.Log("엔딩 타임라인 재생 종료 - 인트로 씬으로 이동합니다.");
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
}
