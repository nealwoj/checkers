using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    public bool AIenabled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //buttons
    public void LocalButton(string scene)
    {
        AIenabled = false;
        SceneManager.LoadScene(scene);
    }
    public void AIButton(string scene)
    {
        AIenabled = true;
        SceneManager.LoadScene(scene);
    }
}
