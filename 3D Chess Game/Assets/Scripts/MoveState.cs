using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveState
{
    public int fromX, fromY;
    public int toX, toY;

    public ChessPiece movedPiece;
    public ChessPiece capturedPiece;

    public bool wasWhiteTurn;

    // promócióhoz
    public bool wasPromotion;
    public ChessPieceType originalType;
}
