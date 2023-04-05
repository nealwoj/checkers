using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum CustomColors
{
    INVALID = -1,
    MOVE_TILE,
    OTHER_TILE,
    PLAYER1,
    PLAYER2
}

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public GameObject winUI, playAgainButton, colorList, colorPicker, mainMenuButton;
    public CustomColors currentCustomColor;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        playAgainButton.SetActive(false);
        mainMenuButton.SetActive(false);
        winUI.SetActive(false);
        colorList.SetActive(false);
        currentCustomColor = CustomColors.INVALID;

        colorPicker.GetComponent<FlexibleColorPicker>().SetColor(Color.white);
        colorPicker.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateColorPicker(currentCustomColor);
    }

    private void UpdateColorPicker(CustomColors color)
    {
        if (colorPicker.activeSelf)
        {
            switch (color)
            {
                case CustomColors.MOVE_TILE:
                    World.Instance.mainGridColor = colorPicker.GetComponent<FlexibleColorPicker>().GetColor();
                    break;
                case CustomColors.OTHER_TILE:
                    World.Instance.otherGridColor = colorPicker.GetComponent<FlexibleColorPicker>().GetColor();
                    break;
                case CustomColors.PLAYER1:
                    World.Instance.teamOneColor = colorPicker.GetComponent<FlexibleColorPicker>().GetColor();
                    break;
                case CustomColors.PLAYER2:
                    World.Instance.teamTwoColor = colorPicker.GetComponent<FlexibleColorPicker>().GetColor();
                    break;
                default:
                    Debug.Log("Custom Color defaulted!");
                    break;
            }

            World.Instance.Draw();
        }
    }

    //buttons
    public void PlayAgain()
    {
        playAgainButton.SetActive(false);
        mainMenuButton.SetActive(false);
        winUI.SetActive(false);

        World.Instance.Restart();
    }
    public void ColorListButton()
    {
        colorList.SetActive(!colorList.activeSelf);
        colorPicker.SetActive(false);
    }
    public void MoveTile()
    {
        colorPicker.SetActive(true);
        currentCustomColor = CustomColors.MOVE_TILE;
        //colorPicker.GetComponent<FlexibleColorPicker>().SetColor(World.Instance.mainGridColor);
    }
    public void OtherTile()
    {
        colorPicker.SetActive(true);
        currentCustomColor = CustomColors.OTHER_TILE;
        //colorPicker.GetComponent<FlexibleColorPicker>().SetColor(World.Instance.otherGridColor);
    }
    public void Player1()
    {
        colorPicker.SetActive(true);
        currentCustomColor = CustomColors.PLAYER1;
        //colorPicker.GetComponent<FlexibleColorPicker>().SetColor(World.Instance.teamOneColor);
    }
    public void Player2()
    {
        colorPicker.SetActive(true);
        currentCustomColor = CustomColors.PLAYER2;
        //colorPicker.GetComponent<FlexibleColorPicker>().SetColor(World.Instance.teamTwoColor);
    }
    public void ResetButton()
    {
        colorPicker.SetActive(false);

        World.Instance.mainGridColor = Color.black;
        World.Instance.otherGridColor = Color.red;
        World.Instance.teamOneColor = Color.white;
        World.Instance.teamTwoColor = Color.red;

        World.Instance.ResetGrid();
        World.Instance.ResetPieces();
    }
    public void MainMenu()
    {
        if (GameController.Instance)
            GameController.Instance.AIenabled = false;

        SceneManager.LoadScene("StartScene");
    }
}