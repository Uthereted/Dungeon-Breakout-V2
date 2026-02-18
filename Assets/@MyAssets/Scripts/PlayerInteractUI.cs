using UnityEngine;
using TMPro;

public class PlayerInteractUI : MonoBehaviour
{
    public GameObject root;      // el GameObject del texto (GrabPrompt)
    public TMP_Text label;       // el componente TMP del texto

    void Awake()
    {
        Hide();
    }

    public void Show(string text)
    {
        if (label) label.text = text;
        if (root) root.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}
