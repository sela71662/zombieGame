using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!

public class IntroSceneController : MonoBehaviour
{
    // 이동할 게임 씬의 이름을 인스펙터에서 적을 수 있게 만듭니다.
    public string gameSceneName = "Main";

    public Material grassMaterial; // 바닥 잔디 머티리얼 (Grass.mat 연결 필수)
    
    // 고정 초록색 (138, 181, 73)
    private Color introColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);

    private void Start() {
        // 인트로 씬 시작 시 머티리얼을 무조건 초록색으로 초기화
        if (grassMaterial != null) {
            grassMaterial.SetColor("_BaseColor", introColor);
        }
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
