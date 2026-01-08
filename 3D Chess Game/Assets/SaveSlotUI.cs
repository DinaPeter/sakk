using System;
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
    [SerializeField] private Button deleteButton;
    [SerializeField] private Image previewImage;

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

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(OnDeleteClicked);

        Refresh();
    }

    public void Refresh()
    {
        string key = $"ChessSave_{slotIndex}";

        bool hasSave = PlayerPrefs.HasKey(key);

        if (hasSave)
        {
            string time = PlayerPrefs.GetString(key + "_Time");
            int moves = PlayerPrefs.GetInt(key + "_Moves");

            infoText.text = $"Last save:\n{time}\nMoves: {moves}";
            loadButton.interactable = true;
            deleteButton.interactable = true;
        }
        else
        {
            infoText.text = "Empty slot";
            loadButton.interactable = false;
            deleteButton.interactable = false;
        }
        LoadPreviewImage();
    }

    private void OnDeleteClicked()
    {
        chessboard.DeleteSlot(slotIndex);
        Refresh();
    }
    private void LoadPreviewImage()
    {
        string key = $"ChessSave_{slotIndex}_Image";

        if (!PlayerPrefs.HasKey(key))
        {
            previewImage.enabled = false;
            return;
        }

        byte[] png = Convert.FromBase64String(PlayerPrefs.GetString(key));
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(png);

        previewImage.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        previewImage.enabled = true;
    }
}
