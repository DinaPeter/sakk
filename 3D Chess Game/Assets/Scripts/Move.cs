using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move
{
    public ChessPieceType piece;
    public Vector2Int from;
    public Vector2Int to;

    public bool isCapture;
    public bool isCheck;
    public bool isCheckmate;

    public bool isKingSideCastle;
    public bool isQueenSideCastle;

    public ChessPieceType promotion = ChessPieceType.None;
}
