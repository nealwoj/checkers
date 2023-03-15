using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public GameObject whiteScoreUI, redScoreUI, whiteWinUI, redWinUI;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        whiteScoreUI.GetComponent<TextMeshProUGUI>().text = World.Instance.whiteScore.ToString();
        redScoreUI.GetComponent<TextMeshProUGUI>().text = World.Instance.redScore.ToString();

        if (World.Instance.redWin)
            redWinUI.SetActive(true);
        if (World.Instance.whiteWin)
            whiteWinUI.SetActive(true);
    }
}