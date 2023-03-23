using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public GameObject whiteScoreUI, redScoreUI, whiteWinUI, redWinUI;

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
        
    }

    // Update is called once per frame
    void Update()
    {
        whiteScoreUI.GetComponent<TextMeshProUGUI>().text = World.Instance.whiteScore.ToString();
        redScoreUI.GetComponent<TextMeshProUGUI>().text = World.Instance.redScore.ToString();
    }
}