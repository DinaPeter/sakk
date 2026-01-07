using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    private int slotIndex;
    private Chessboard chessboard;

    public void Init(int index, Chessboard board)
    {
        slotIndex = index;
        chessboard = board;

        titleText.text = $"SLOT {index + 1}";

        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(() => chessboard.SaveSlotAndResume(slotIndex));

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => chessboard.LoadSlotAndResume(slotIndex));

        Refresh();
    }

    public void Refresh()
    {
        string key = $"ChessSave_{slotIndex}";

        if (PlayerPrefs.HasKey(key))
        {
            string time = PlayerPrefs.GetString(key + "_Time");
            int moves = PlayerPrefs.GetInt(key + "_Moves");

            infoText.text = $"Last save:\n{time}\nMoves: {moves}";
            loadButton.interactable = true;
        }
        else
        {
            infoText.text = "Empty slot";
            loadButton.interactable = false;
        }
    }
}
