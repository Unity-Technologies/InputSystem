using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var newGameButtonObject = GameObject.Find("NewGameButton");
        if (newGameButtonObject != null)
        {
            var button = newGameButtonObject.GetComponent<Button>();
            if (button != null)
            {
                button.Select();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewGame()
    {
        gameObject.SetActive(false);
    }

    public void Options()
    {
        // Not implemented
    }

    public void Quit()
    {
        Application.Quit(0);
    }
}
