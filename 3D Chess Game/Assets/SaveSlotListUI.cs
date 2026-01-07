using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotListUI : MonoBehaviour
{
    [SerializeField] private SaveSlotUI slotPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Chessboard chessboard;
    [SerializeField] private int slotCount = 3;

    private SaveSlotUI[] slots;

    private void OnEnable()
    {
        GenerateSlots();
    }

    private void GenerateSlots()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        slots = new SaveSlotUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            SaveSlotUI slot = Instantiate(slotPrefab, contentParent);
            slot.Init(i, chessboard);
            slots[i] = slot;
        }
    }
}
