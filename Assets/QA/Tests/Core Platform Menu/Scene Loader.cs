using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private string sceneName;
    public TMP_Text buttonText;
    public void Start()
    {  
        sceneName = buttonText.name.ToString();
    }
    public void LoadSceneOnButtonPress()
    {
        SceneManager.LoadScene(sceneName);
        SceneManager.LoadScene("Esc Menu Additive", LoadSceneMode.Additive);
    }
}
