using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenController : MonoBehaviour
{
    private void Start()
    {
        if (MusicManager.Instance != null) MusicManager.Instance.PlayMenuMusic();
    }

    public void StartDemo()
    {
        SceneManager.LoadScene("Demo");
    }
}
