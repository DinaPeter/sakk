using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private bool gameIsPaused = false;

    private void Awake()
    {
        Instance = this;
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
            gameIsPaused = true;
            Time.timeScale = 1f;
            menuAnimator.SetTrigger("OptionMenu");
            escMenu.SetActive(false);
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
            menuAnimator.SetTrigger("EscMenu");
            escMenu.SetActive(true);
            mainMenu.SetActive(false);
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
}
