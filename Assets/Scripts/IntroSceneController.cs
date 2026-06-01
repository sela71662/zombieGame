using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수!
public class IntroSceneController : MonoBehaviour
{
      // 이동할 게임 씬의 이름을 인스펙터에서 적을 수 있게 만듭니다.
      public string gameSceneName = "Main";

    // 버튼을 클릭했을 때 실행될 함수입니다.
    public void StartGame()
    {
        Debug.Log("click");

        // 지정된 씬을 로드합니다.
        SceneManager.LoadScene(gameSceneName);
    }
}
