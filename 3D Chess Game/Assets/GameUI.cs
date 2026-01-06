using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private GameObject gameBoard;
    [SerializeField] private GameObject escMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private AudioSource musicPlayer;
    [SerializeField] private GameObject whiteTimeTitle;
    [SerializeField] private GameObject blackTimeTitle;
    [SerializeField] private GameObject whiteTime;
    [SerializeField] private GameObject blackTime;
    [SerializeField] private GameObject plusTimeTitle;
    [SerializeField] private GameObject plusTime;
    [SerializeField] private Dropdown difficultyDropdown;
    [SerializeField] private Dropdown aiColorDropdown;
    [SerializeField] private Chessboard chessboard;

    private bool gameIsPaused = false;

    private void Awake()
    {
        Instance = this;

        aiColorDropdown.value = (int)chessboard.aiColor;
        aiColorDropdown.onValueChanged.AddListener(SetAIColor);

        if (difficultyDropdown != null && chessboard != null)
        {
            // Kezdeti érték beállítása
            chessboard.aiDifficulty = AIDifficulty.Easy;
            difficultyDropdown.value = (int)chessboard.aiDifficulty;

            // Listener hozzáadása a Dropdownhoz
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }

    // Gombok
    public void OnPlayButton()
    {
        menuAnimator.SetTrigger("InGameMenu");
        gameBoard.SetActive(true);
    }
    public void OnOptionButton()
    {
        if (Time.timeScale == 0f)
        {
            menuAnimator.SetTrigger("OptionMenu");
            chessboard.SetMenuState(MenuState.Settings);
        }
        else 
        {
            menuAnimator.SetTrigger("OptionMenu");
        }
    }
    public void OnBackButton()
    {
        if (gameIsPaused == true)
        {
            menuAnimator.SetTrigger("InGameMenu");
            chessboard.SetMenuState(MenuState.Pause);
        }
        else 
        {
            menuAnimator.SetTrigger("StartMenu");
        }
    }
    public void OnMusic()
    {
        if (musicPlayer.mute == false)
        {
            musicPlayer.mute = true;
        }
        else 
        {
            musicPlayer.mute = false;
        }
    }
    public void OnClockOnOffClick()
    {
        if (whiteTime.activeSelf == true && blackTime.activeSelf == true && whiteTimeTitle.activeSelf == true && blackTimeTitle.activeSelf == true && plusTimeTitle.activeSelf == true && plusTime.activeSelf == true)
        {
            plusTimeTitle.SetActive(false);
            blackTimeTitle.SetActive(false);
            whiteTimeTitle.SetActive(false);
            plusTime.SetActive(false);
            blackTime.SetActive(false);
            whiteTime.SetActive(false);
        }
        else
        {
            plusTimeTitle.SetActive(true);
            blackTimeTitle.SetActive(true);
            whiteTimeTitle.SetActive(true);
            plusTime.SetActive(true);
            blackTime.SetActive(true);
            whiteTime.SetActive(true);
        }
    }
    public void OnDifficultyChanged(int index)
    {
        if (chessboard != null)
        {
            chessboard.aiDifficulty = (AIDifficulty)index;
            Debug.Log("AI Difficulty set to: " + index);
        }
    }
    public void SetAIColor(int value)
    {
        chessboard.aiColor = (AIColor)value;
        Debug.Log("AI color set to: " + chessboard.aiColor);
    }
}
