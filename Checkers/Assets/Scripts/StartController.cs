using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartController : MonoBehaviour
{
    public GameObject grid, easyButton, hardButton, localButton, aiButton, backButton;

    // Start is called before the first frame update
    void Start()
    {
        float verticalOffset = (int)Camera.main.orthographicSize;
        float horizontalOffset = verticalOffset * (Screen.width / Screen.height);
        for (int x = 0; x < 20; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                GameObject go = Instantiate(grid);
                go.transform.SetParent(GameObject.Find("Grid").transform);
                go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);

                if ((x + y) % 2 == 0)
                    go.GetComponent<SpriteRenderer>().color = Color.red;
                else
                    go.GetComponent<SpriteRenderer>().color = Color.black;
            }
        }

        easyButton.SetActive(false);
        hardButton.SetActive(false);
        backButton.SetActive(false);
        localButton.SetActive(true);
        aiButton.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //buttons
    public void LocalButton(string scene)
    {
        GameController.Instance.AIenabled = false;
        SceneManager.LoadScene(scene);
    }
    public void AIButton()
    {
        GameController.Instance.AIenabled = true;
        easyButton.SetActive(true);
        hardButton.SetActive(true);
        backButton.SetActive(true);
        localButton.SetActive(false);
        aiButton.SetActive(false);
    }
    public void EasyButton(string scene)
    {
        GameController.Instance.difficulty = Difficulty.EASY;
        SceneManager.LoadScene(scene);
    }
    public void HardButton(string scene)
    {
        GameController.Instance.difficulty = Difficulty.HARD;
        SceneManager.LoadScene(scene);
    }
    public void BackButton()
    {
        easyButton.SetActive(false);
        hardButton.SetActive(false);
        backButton.SetActive(false);
        localButton.SetActive(true);
        aiButton.SetActive(true);
    }
}
