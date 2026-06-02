using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!

public class IntroSceneController : MonoBehaviour
{
    // 이동할 게임 씬의 이름을 인스펙터에서 적을 수 있게 만듭니다.
    public string gameSceneName = "Main";

    public Material grassMaterial; // 바닥 잔디 머티리얼
    
    // 인트로 씬 전용 고정 색상 (138, 181, 73)
    public Color introColor = new Color(138f / 255f, 181f / 255f, 73f / 255f, 1f);

    private void Start() {
        // 인트로 씬이 시작될 때 머티리얼을 항상 초록색으로 초기화 (URP 속성 직접 제어)
        if (grassMaterial != null) {
            grassMaterial.SetColor("_BaseColor", introColor);
        }
    }

    // 버튼을 클릭했을 때 실행될 함수입니다.
    public void StartGame()
    {
        Debug.Log("click");

        // 지정된 씬을 로드합니다.
        SceneManager.LoadScene(gameSceneName);
    }
}
