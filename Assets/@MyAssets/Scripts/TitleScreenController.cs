using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenController : MonoBehaviour
{
    public void StartDemo()
    {
        SceneManager.LoadScene("Demo");
    }
}
