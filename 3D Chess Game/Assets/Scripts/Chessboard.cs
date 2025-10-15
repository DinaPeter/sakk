using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion,
}

public class Chessboard : MonoBehaviour
{
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject vistoryScreen;
    [SerializeField] private GameObject escMenu;
    [SerializeField] private GameObject moves;
    [SerializeField] private TMP_Text write;
    [SerializeField] private GameObject pieceMenu;
    [SerializeField] private GameObject timeHelper;
    [SerializeField] private TMP_InputField whiteTime;
    [SerializeField] private TMP_InputField blackTime;
    [SerializeField] private TMP_InputField plusTime;
    [SerializeField] private GameObject blackTimeTitle;
    [SerializeField] private GameObject whiteTimeTitle;
    [SerializeField] private TMP_Text blackTimeWrite;
    [SerializeField] private TMP_Text whiteTimeWrite;
    [SerializeField] private GameObject blackTimeWrite2;
    [SerializeField] private GameObject whiteTimeWrite2;

    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private bool gameIsPaused = false;
    private Dictionary<Vector2Int, string> movesToWrite = new Dictionary<Vector2Int, string>();
    private float whiteTimeValue;
    private float blackTimeValue;
    private float plusTimeValue;
    private string currentWhiteTime;
    private string currentBlackTime;
    private string currentPlusTime;
    private float whiteMinutes;
    private float whiteSeconds;
    private float blackMinutes;
    private float blackSeconds;

    private void Awake()
    {
        GetTimeValue(whiteTime, blackTime, plusTime);
        isWhiteTurn = true;
        moves.SetActive(true);
        FillDictionary(TILE_COUNT_X, TILE_COUNT_Y);

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (timeHelper.activeSelf == false) 
        {
            blackTimeTitle.SetActive(false);
            whiteTimeTitle.SetActive(false);
            blackTimeWrite2.SetActive(false);
            whiteTimeWrite2.SetActive(false);
        }
        if (timeHelper.activeSelf == true && whiteTimeValue > 0 && isWhiteTurn)
        {
            whiteTimeValue -= Time.deltaTime;
            UpdateWhiteTimerDisplay(whiteTimeValue, plusTimeValue);
        }
        else if (timeHelper.activeSelf == true && whiteTimeValue <= 0)
        {
            DisplayVictory(1);
        }

        if (timeHelper.activeSelf == true && blackTimeValue > 0 && !isWhiteTurn)
        {
            blackTimeValue -= Time.deltaTime;
            UpdateBlackTimerDisplay(blackTimeValue, plusTimeValue);
        }
        else if (timeHelper.activeSelf == true && blackTimeValue <= 0)
        {
            DisplayVictory(0);
        }

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // Egér gomblenyomás
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        // Lehetséges lépés és highlight-olás
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                        // Speciális lépés
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();
                        HighlightTiles();
                    }
                }
            }

            // Egér gomb felengedés
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }

                string from = "";
                string to = "";
                string type = "";

                foreach (var item in movesToWrite)
                {
                    if (previousPosition == item.Key)
                    {
                        from = item.Value;
                    }
                    if (hitPosition == item.Key)
                    {
                        to = item.Value;
                    }
                }

                type = TypeCheck(currentlyDragging);
                write.text += type + ": " + from + " -> " + to + "\n";

                RemoveHighlightTiles();
                currentlyDragging = null;
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    // Tábla generálás
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int i = 0; i < tileCountX; i++)
        {
            for (int j = 0; j < tileCountY; j++)
            {
                tiles[i, j] = GenerateSingleTile(tileSize, i, j);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Figura generálás
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        // Világos
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        // Sötét
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        Material[] materialsArray = new Material[(this.GetComponent<Renderer>().materials.Length - 1)];
        cp.GetComponent<Renderer>().materials.CopyTo(materialsArray, 0);
        materialsArray[1] = teamMaterials[team];
        cp.GetComponent<Renderer>().materials = materialsArray;

        return cp;
    }

    // Figura Pozícionálás
    private void PositionAllPieces()
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                {
                    PositionSinglePiece(i, j, true);
                }
            }
        }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    // Matt
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        vistoryScreen.SetActive(true);
        vistoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnResetButton()
    {
        // UI
        vistoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        vistoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        vistoryScreen.SetActive(false);

        // Mezõ visszaállítás
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        // Bábu visszaállítás
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                {
                    Destroy(chessPieces[i, j].gameObject);
                }
                chessPieces[i, j] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
        {
            Destroy(deadWhites[i].gameObject);
        }

        for (int i = 0; i < deadBlacks.Count; i++)
        {
            Destroy(deadBlacks[i].gameObject);
        }

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }

    // Speciális Lépések
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 3, 0, tileSize / 3) + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize / 1.5f, 0, tileSize / 1.5f) + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if (specialMove == SpecialMove.Promotion)
        {
            pieceMenu.SetActive(true);
        }

        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            // Bal Bástya
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // Világos oldal
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) // Sötét oldal
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            // Jobb bástya
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // Világos oldal
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // Sötét oldal
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                {
                    if (chessPieces[i, j].type == ChessPieceType.King)
                    {
                        if (chessPieces[i, j].team == currentlyDragging.team)
                        {
                            targetKing = chessPieces[i, j];
                        }
                    }
                }

            }
        }

        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Értékek Meghívás után visszaállításra
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Lépés szimulálás és sakk ellenõrzés
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            if (cp.type == ChessPieceType.King)
            {
                kingPositionThisSim = new Vector2Int(simX, simY);
            }

            // Tábla szimulálás
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for (int j = 0; j < TILE_COUNT_X; j++)
            {
                for (int k = 0; k < TILE_COUNT_Y; k++)
                {
                    if (chessPieces[j, k] != null)
                    {
                        simulation[j, k] = chessPieces[j, k];
                        if (simulation[j, k].team != cp.team)
                        {
                            simAttackingPieces.Add(simulation[j, k]);
                        }
                    }
                }
            }

            // Lépés szimulálás
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            var deadPiece = simAttackingPieces.Find(x => x.currentX == simX && x.currentY == simY);
            if (deadPiece != null)
            {
                simAttackingPieces.Remove(deadPiece);
            }

            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int j = 0; j < simAttackingPieces.Count; j++)
            {
                var pieceMoves = simAttackingPieces[j].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int k = 0; k < pieceMoves.Count; k++)
                {
                    simMoves.Add(pieceMoves[k]);
                }
            }

            // Ha a király veszélyben van töröljük a lépést
            if (ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            // CP visszaállítás
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        // Törlés az elérhetõ lépéslistából
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                {
                    if (chessPieces[i, j].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[i, j]);
                        if (chessPieces[i, j].type == ChessPieceType.King)
                        {
                            targetKing = chessPieces[i, j];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[i, j]);
                    }
                }
            }
        }

        // Támadva van-e a király?
        List<Vector2Int> currentAvailabeMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int k = 0; k < pieceMoves.Count; k++)
            {
                currentAvailabeMoves.Add(pieceMoves[k]);
            }
        }

        // Sakkban vagyunk-e?
        if (ContainsValidMove(ref currentAvailabeMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            // meg tudjuk-e védeni a sakkban lévõ királyt?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count > 0)
                {
                    return false;
                }
            }
            return true; // Matt
        }

        return false;
    }

    // Sakk Óra
    private void GetTimeValue(TMP_InputField whiteTime, TMP_InputField blackTime, TMP_InputField plusTime)
    {
        whiteTimeValue = float.Parse(whiteTime.text) * 60f;
        blackTimeValue = float.Parse(blackTime.text) * 60f;
        plusTimeValue = float.Parse(plusTime.text) * 60f;
    }
    private void UpdateWhiteTimerDisplay(float whiteTimeValue, float plusTimeValue)
    {
        whiteMinutes = Mathf.FloorToInt(whiteTimeValue / 60);
        whiteSeconds = Mathf.FloorToInt(whiteTimeValue % 60);

        currentWhiteTime = string.Format("{00:00}{1:00}", whiteMinutes, whiteSeconds);

        whiteTimeWrite.text = currentWhiteTime[0].ToString() + currentWhiteTime[1].ToString() + ":" + currentWhiteTime[2].ToString() + currentWhiteTime[3].ToString();
    }
    private void UpdateBlackTimerDisplay(float blackTimeValue, float plusTimeValue)
    {
        blackMinutes = Mathf.FloorToInt(blackTimeValue / 60);
        blackSeconds = Mathf.FloorToInt(blackTimeValue % 60);

        currentBlackTime = string.Format("{00:00}{1:00}", blackMinutes, blackSeconds);

        blackTimeWrite.text = currentBlackTime[0].ToString() + currentBlackTime[1].ToString() + ":" + currentBlackTime[2].ToString() + currentBlackTime[3].ToString();
    }

    // Mûveletek
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }

            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(1);
                }

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 3, 0, tileSize / 3) + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(0);
                }

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize / 1.5f, 0, tileSize / 1.5f) + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        if (isWhiteTurn)
        {
            blackTimeValue += plusTimeValue;
        }
        else 
        {
            whiteTimeValue += plusTimeValue;
        }
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        ProcessSpecialMove();

        if (CheckForCheckmate())
        {
            CheckMate(cp.team);
        }

        return true;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (tiles[i, j] == hitInfo)
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return -Vector2Int.one;
    }
    private void Resume()
    {
        escMenu.SetActive(false);
        moves.SetActive(true);
        Time.timeScale = 1f;
        gameIsPaused = false;
    }
    private void Pause()
    {
        escMenu.SetActive(true);
        moves.SetActive(false);
        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    private void FillDictionary(int tileCountX, int tileCountY)
    {
        string letter = "";
        int num = 0;
        for (int i = 0; i < tileCountX; i++)
        {
            for (int j = 0; j < tileCountY; j++)
            {
                if (i == 0)
                {
                    letter = "A";
                    num = j + 1;
                }
                else if (i == 1)
                {
                    letter = "B";
                    num = j + 1;
                }
                else if (i == 2)
                {
                    letter = "C";
                    num = j + 1;
                }
                else if (i == 3)
                {
                    letter = "D";
                    num = j + 1;
                }
                else if (i == 4)
                {
                    letter = "E";
                    num = j + 1;
                }
                else if (i == 5)
                {
                    letter = "F";
                    num = j + 1;
                }
                else if (i == 6)
                {
                    letter = "G";
                    num = j + 1;
                }
                else if (i == 7)
                {
                    letter = "H";
                    num = j + 1;
                }
                movesToWrite.Add(new Vector2Int(i, j), letter + num);
            }
        }
    }
    private string TypeCheck(ChessPiece dragging)
    {
        string type = "";
        if (dragging.type == ChessPieceType.Pawn)
        {
            if (dragging.team == 0)
            {
                type = "Világos gyalog";
            }
            else
            {
                type = "Sötét gyalog";
            }
        }
        else if (dragging.type == ChessPieceType.Rook)
        {
            if (currentlyDragging.team == 0)
            {
                type = "Világos bástya";
            }
            else
            {
                type = "Sötét bástya";
            }
        }
        else if (dragging.type == ChessPieceType.Knight)
        {
            if (dragging.team == 0)
            {
                type = "Világos huszár";
            }
            else
            {
                type = "Sötét huszár";
            }
        }
        else if (dragging.type == ChessPieceType.Bishop)
        {
            if (dragging.team == 0)
            {
                type = "Világos futó";
            }
            else
            {
                type = "Sötét futó";
            }
        }
        else if (dragging.type == ChessPieceType.Queen)
        {
            if (dragging.team == 0)
            {
                type = "Világos vezér";
            }
            else
            {
                type = "Sötét Vezér";
            }
        }
        else if (dragging.type == ChessPieceType.King)
        {
            if (dragging.team == 0)
            {
                type = "Világos király";
            }
            else
            {
                type = "Sötét király";
            }
        }

        return type;
    }

    public void OnQueenClick()
    {
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if (targetPawn.type == ChessPieceType.Pawn)
        {
            if (targetPawn.team == 0 && lastMove[1].y == 7)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Queen, 0);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Queen, 1);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
        }
    }
    public void OnBishopClick()
    {
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if (targetPawn.type == ChessPieceType.Pawn)
        {
            if (targetPawn.team == 0 && lastMove[1].y == 7)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Bishop, 0);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Bishop, 1);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
        }
    }
    public void OnKnightClick()
    {
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if (targetPawn.type == ChessPieceType.Pawn)
        {
            if (targetPawn.team == 0 && lastMove[1].y == 7)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Knight, 0);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Knight, 1);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
        }
    }
    public void OnRookClick()
    {
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if (targetPawn.type == ChessPieceType.Pawn)
        {
            if (targetPawn.team == 0 && lastMove[1].y == 7)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Rook, 0);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0)
            {
                ChessPiece newPiece = SpawnSinglePiece(ChessPieceType.Rook, 1);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                pieceMenu.SetActive(false);
            }
        }
    }
}
