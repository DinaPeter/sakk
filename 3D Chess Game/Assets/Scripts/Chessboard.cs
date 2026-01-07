using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion,
}

public enum AIDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

public enum AIColor
{
    White,
    Black
}

public enum MenuState
{
    None,
    Pause,
    Save
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
    [SerializeField] private StockfishManager stockfish;
    [SerializeField] public AIDifficulty aiDifficulty = AIDifficulty.Easy;
    [SerializeField] private Camera mainCamera;      // fehér nézet
    [SerializeField] private Camera secondaryCamera; // fekete nézet
    [SerializeField] public GameObject saveMenu;

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
    private float AIfrom;
    private float AIto;
    private bool whiteKingMoved = false;
    private bool blackKingMoved = false;
    private bool whiteRookLeftMoved = false;
    private bool whiteRookRightMoved = false;
    private bool blackRookLeftMoved = false;
    private bool blackRookRightMoved = false;
    private Vector2Int enPassantTarget = -Vector2Int.one;
    private int halfmoveClock = 0;
    private List<Move> pgnMoves = new List<Move>();
    public AIColor aiColor = AIColor.Black;
    private MenuState currentMenuState = MenuState.None;

    private void Awake()
    {
        GetTimeValue(whiteTime, blackTime, plusTime);
        isWhiteTurn = true;
        moves.SetActive(true);
        FillDictionary(TILE_COUNT_X, TILE_COUNT_Y);

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();

        if (stockfish == null)
        {
            stockfish = FindObjectOfType<StockfishManager>();
        }

        SetupCameraForPlayer();
        ApplyAIDifficulty();

        if (IsAITurn())
        {
            RequestAIMove();
        }
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
            if (currentMenuState == MenuState.None)
            {
                SetMenuState(MenuState.Pause);
            }
            else if (currentMenuState == MenuState.Pause)
            {
                SetMenuState(MenuState.None);
            }
            else if (currentMenuState == MenuState.Save)
            {
                SetMenuState(MenuState.Pause);
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

                RequestAIMove();

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
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece movingPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            foreach (var dir in new int[] { -1, 1 })
            {
                int targetY = lastMove[1].y + dir;
                if (targetY >= 0 && targetY < 8)
                {
                    ChessPiece targetPawn = chessPieces[lastMove[1].x, targetY];
                    if (targetPawn != null && targetPawn.type == ChessPieceType.Pawn && targetPawn.canBeCapturedEnPassant)
                    {
                        chessPieces[targetPawn.currentX, targetPawn.currentY] = null;
                        if (targetPawn.team == 0)
                        {
                            deadWhites.Add(targetPawn);
                            targetPawn.SetScale(Vector3.one * deathSize);
                            targetPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 3, 0, tileSize / 3) + (Vector3.forward * deathSpacing) * deadWhites.Count);
                        }
                        else
                        {
                            deadBlacks.Add(targetPawn);
                            targetPawn.SetScale(Vector3.one * deathSize);
                            targetPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize / 1.5f, 0, tileSize / 1.5f) + (Vector3.back * deathSpacing) * deadBlacks.Count);
                        }
                    }
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
            ChessPiece king = chessPieces[lastMove[1].x, lastMove[1].y];

            // Bal oldal
            if (lastMove[1].x == 2)
            {
                ChessPiece rook = chessPieces[0, lastMove[1].y];
                chessPieces[3, lastMove[1].y] = rook;
                PositionSinglePiece(3, lastMove[1].y);
                chessPieces[0, lastMove[1].y] = null;
            }
            // Jobb oldal
            else if (lastMove[1].x == 6)
            {
                ChessPiece rook = chessPieces[7, lastMove[1].y];
                chessPieces[5, lastMove[1].y] = rook;
                PositionSinglePiece(5, lastMove[1].y);
                chessPieces[7, lastMove[1].y] = null;
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
        bool resetHalfmove = false;

        // Alapértelmezett en passant törlés minden gyalogra
        foreach (var piece in chessPieces)
            if (piece != null && piece.type == ChessPieceType.Pawn)
                piece.canBeCapturedEnPassant = false;

        // En passant ütés kezelése
        if (cp.type == ChessPieceType.Pawn && enPassantTarget == new Vector2Int(x, y))
        {
            int captureY = (cp.team == 0) ? y - 1 : y + 1;
            ChessPiece capturedPawn = chessPieces[x, captureY];
            if (capturedPawn != null && capturedPawn.type == ChessPieceType.Pawn)
            {
                RemoveCapturedPiece(capturedPawn);
                chessPieces[x, captureY] = null;
            }
        }

        // Ha ütés történik
        if (chessPieces[x, y] != null)
            resetHalfmove = true;

        // Ha gyalog lép
        if (cp.type == ChessPieceType.Pawn)
            resetHalfmove = true;

        // Gyalog dupla lépés
        if (cp.type == ChessPieceType.Pawn)
        {
            int startRow = (cp.team == 0) ? 1 : 6;

            if (previousPosition.y == startRow && Mathf.Abs(y - previousPosition.y) == 2)
            {
                int epY = (previousPosition.y + y) / 2;
                enPassantTarget = new Vector2Int(x, epY);
            }
        }

        // Király mozgás
        if (cp.type == ChessPieceType.King)
        {
            if (cp.team == 0) whiteKingMoved = true;
            else blackKingMoved = true;
        }

        // Bástya mozgás
        if (cp.type == ChessPieceType.Rook)
        {
            if (cp.team == 0)
            {
                if (previousPosition.x == 0) whiteRookLeftMoved = true;
                if (previousPosition.x == 7) whiteRookRightMoved = true;
            }
            else
            {
                if (previousPosition.x == 0) blackRookLeftMoved = true;
                if (previousPosition.x == 7) blackRookRightMoved = true;
            }
        }

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

            // Ha bástyát ütnek, sánc jog elveszik
            if (ocp.type == ChessPieceType.Rook)
            {
                if (ocp.team == 0)
                {
                    if (x == 0) whiteRookLeftMoved = true;
                    if (x == 7) whiteRookRightMoved = true;
                }
                else
                {
                    if (x == 0) blackRookLeftMoved = true;
                    if (x == 7) blackRookRightMoved = true;
                }
            }
        }
        
        // Bábu mozgatás a táblán
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        if (resetHalfmove)
            halfmoveClock = 0;
        else
            halfmoveClock++;

        // Játékosváltás
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

        // PGN hozzáadás
        Move m = new Move
        {
            piece = cp.type,
            from = previousPosition,
            to = new Vector2Int(x, y),
            isCapture = chessPieces[x, y] != null,
            isCheck = false, // késõbb update
            isCheckmate = false,
            promotion = specialMove == SpecialMove.Promotion ? ChessPieceType.Queen : ChessPieceType.None, // alap Queen, a promóció menü állíthatja
            isKingSideCastle = specialMove == SpecialMove.Castling && x == 6,
            isQueenSideCastle = specialMove == SpecialMove.Castling && x == 2
        };

        WriteMoveToUI(cp, previousPosition, new Vector2Int(x, y));
        pgnMoves.Add(m);

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
    private void RemoveCapturedPiece(ChessPiece piece)
    {
        if (piece.team == 0)
        {
            deadWhites.Add(piece);
            piece.SetScale(Vector3.one * deathSize);
            piece.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 3, 0, tileSize / 3) + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else
        {
            deadBlacks.Add(piece);
            piece.SetScale(Vector3.one * deathSize);
            piece.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize / 1.5f, 0, tileSize / 1.5f) + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }

        // Ha bástya ütés történt, frissítjük a sáncolási jogot
        if (piece.type == ChessPieceType.Rook)
        {
            if (piece.team == 0)
            {
                if (piece.currentX == 0) whiteRookLeftMoved = true;
                if (piece.currentX == 7) whiteRookRightMoved = true;
            }
            else
            {
                if (piece.currentX == 0) blackRookLeftMoved = true;
                if (piece.currentX == 7) blackRookRightMoved = true;
            }
        }

        // Ha király ütés történik (matt)
        if (piece.type == ChessPieceType.King)
        {
            CheckMate(piece.team == 0 ? 1 : 0);
        }
    }
    void ClearBoard()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }

        moveList.Clear();
    }
    ChessPieceType FenCharToPiece(char c)
    {
        return char.ToLower(c) switch
        {
            'p' => ChessPieceType.Pawn,
            'r' => ChessPieceType.Rook,
            'n' => ChessPieceType.Knight,
            'b' => ChessPieceType.Bishop,
            'q' => ChessPieceType.Queen,
            'k' => ChessPieceType.King,
            _ => ChessPieceType.Pawn
        };
    }
    public string BoardToFEN()
    {
        string fen = "";

        // Táblakép (8 -> 1 sor)
        for (int y = 7; y >= 0; y--) 
        {
            int empty = 0;

            for (int x = 0; x < 8; x++)
            {
                ChessPiece piece = chessPieces[x,y];
                
                if (piece == null)
                {
                    empty++;
                }
                else
                {
                    if(empty > 0)
                    {
                        fen += empty;
                        empty = 0;
                    }

                    fen += PieceToFEN(piece);
                }
            }

            if (empty > 0)
            {
                fen += empty;
            }

            if(y > 0)
            {
                fen += "/";
            }
        }

        // Ki jön
        fen += isWhiteTurn ? " w " : " b ";

        // Sáncolás
        string castling = "";
        if (!whiteKingMoved && !whiteRookRightMoved) castling += "K";
        if (!whiteKingMoved && !whiteRookLeftMoved) castling += "Q";
        if (!blackKingMoved && !blackRookRightMoved) castling += "k";
        if (!blackKingMoved && !blackRookLeftMoved) castling += "q";
        fen += castling == "" ? "- " : castling + " ";

        // En passant
        if (enPassantTarget == -Vector2Int.one)
        {
            fen += "- ";
        }
        else
        {
            char file = (char)('a' + enPassantTarget.x);
            char rank = (char)('1' + enPassantTarget.y);
            fen += $"{file}{rank} ";
        }

        // Halfmove clock
        fen += halfmoveClock + " ";

        // Fullmove number
        fen += (moveList.Count / 2) + 1;

        return fen;
    }
    private char PieceToFEN(ChessPiece piece)
    {
        char c = piece.type switch
        {
            ChessPieceType.Pawn => 'p',
            ChessPieceType.Rook => 'r',
            ChessPieceType.Knight => 'n',
            ChessPieceType.Bishop => 'b',
            ChessPieceType.Queen => 'q',
            ChessPieceType.King => 'k',
            _ => ' '
        };

        return piece.team == 0 ? char.ToUpper(c) : c;
    }
    string SquareToAlg(Vector2Int pos)
    {
        char file = (char)('a' + pos.x);
        char rank = (char)('1' + pos.y);
        return $"{file}{rank}";
    }
    string PieceToPGN(ChessPieceType type)
    {
        return type switch
        {
            ChessPieceType.Knight => "N",
            ChessPieceType.Bishop => "B",
            ChessPieceType.Rook => "R",
            ChessPieceType.Queen => "Q",
            ChessPieceType.King => "K",
            _ => "" // gyalog
        };
    }
    string MoveToPGN(Move move)
    {
        // Sáncolás
        if (move.isKingSideCastle) return "O-O";
        if (move.isQueenSideCastle) return "O-O-O";

        StringBuilder sb = new StringBuilder();
        string piece = PieceToPGN(move.piece);
        sb.Append(piece);

        if (move.isCapture)
        {
            if (move.piece == ChessPieceType.Pawn)
                sb.Append((char)('a' + move.from.x)); // gyalog ütésnél oszlop
            sb.Append("x");
        }

        sb.Append(SquareToAlg(move.to));

        if (move.promotion != ChessPieceType.None)
        {
            sb.Append("=");
            sb.Append(PieceToPGN(move.promotion));
        }

        if (move.isCheckmate) sb.Append("#");
        else if (move.isCheck) sb.Append("+");

        return sb.ToString();
    }
    private Vector2Int UCIToPosition(char file, char rank)
    {
        int x = file - 'a';
        int y = rank - '1';
        return new Vector2Int(x, y);
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
    private void WriteMoveToUI(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        string fromText = movesToWrite[from];
        string toText = movesToWrite[to];
        string type = TypeCheck(piece);

        write.text += type + ": " + fromText + " -> " + toText + "\n";
    }

    // Menü kezelés
    public void SetMenuState(MenuState newState)
    {
        currentMenuState = newState;

        escMenu.SetActive(newState == MenuState.Pause);
        saveMenu.SetActive(newState == MenuState.Save);
        moves.SetActive(newState == MenuState.None);

        Time.timeScale = (newState == MenuState.None) ? 1f : 0f;
        gameIsPaused = newState != MenuState.None;
    }
    private void Resume()
    {
        SetMenuState(MenuState.None);
    }
    private void Pause()
    {
        SetMenuState(MenuState.Pause);
    }
    public string ExportPGN()
    {
        StringBuilder pgn = new StringBuilder();

        // Header
        pgn.AppendLine("[Event \"Unity Chess\"]");
        pgn.AppendLine("[Site \"Local\"]");
        pgn.AppendLine("[White \"Player\"]");
        pgn.AppendLine("[Black \"Stockfish\"]");
        pgn.AppendLine("[Result \"*\"]");
        pgn.AppendLine();

        for (int i = 0; i < pgnMoves.Count; i++)
        {
            if (i % 2 == 0)
                pgn.Append($"{(i / 2) + 1}. ");

            pgn.Append(MoveToPGN(pgnMoves[i]));
            pgn.Append(" ");
        }

        return pgn.ToString();
    }
    public void SavePGNToFile()
    {
        string pgn = ExportPGN();
        string path = Application.persistentDataPath + "/game.pgn";
        System.IO.File.WriteAllText(path, pgn);

        Debug.Log("PGN saved to: " + path);
    }
    public void LoadFromFEN(string fen)
    {
        ClearBoard();

        string[] parts = fen.Split(' ');
        string boardPart = parts[0];
        string turnPart = parts[1];
        string castlingPart = parts[2];
        string enPassantPart = parts[3];
        halfmoveClock = int.Parse(parts[4]);
        int fullmove = int.Parse(parts[5]);

        // Táblakép
        string[] ranks = boardPart.Split('/');
        for (int y = 7; y >= 0; y--)
        {
            int x = 0;
            foreach (char c in ranks[7 - y])
            {
                if (char.IsDigit(c))
                {
                    x += (int)char.GetNumericValue(c);
                }
                else
                {
                    ChessPieceType type = FenCharToPiece(c);
                    int team = char.IsUpper(c) ? 0 : 1;

                    ChessPiece piece = SpawnSinglePiece(type, team);
                    piece.currentX = x;
                    piece.currentY = y;
                    piece.transform.position = GetTileCenter(x, y);
                    chessPieces[x, y] = piece;

                    x++;
                }
            }
        }

        // Ki jön
        isWhiteTurn = turnPart == "w";

        // Sáncolási jogok
        whiteKingMoved = !castlingPart.Contains("K") && !castlingPart.Contains("Q");
        whiteRookRightMoved = !castlingPart.Contains("K");
        whiteRookLeftMoved = !castlingPart.Contains("Q");

        blackKingMoved = !castlingPart.Contains("k") && !castlingPart.Contains("q");
        blackRookRightMoved = !castlingPart.Contains("k");
        blackRookLeftMoved = !castlingPart.Contains("q");

        // En passant
        if (enPassantPart == "-")
        {
            enPassantTarget = -Vector2Int.one;
        }
        else
        {
            int file = enPassantPart[0] - 'a';
            int rank = enPassantPart[1] - '1';
            enPassantTarget = new Vector2Int(file, rank);
        }

        Debug.Log("Game loaded from FEN");
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
        Move lastMove2 = pgnMoves[pgnMoves.Count - 1];
        lastMove2.promotion = ChessPieceType.Queen;
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
        Move lastMove2 = pgnMoves[pgnMoves.Count - 1];
        lastMove2.promotion = ChessPieceType.Bishop;
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
        Move lastMove2 = pgnMoves[pgnMoves.Count - 1];
        lastMove2.promotion = ChessPieceType.Knight;
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
        Move lastMove2 = pgnMoves[pgnMoves.Count - 1];
        lastMove2.promotion = ChessPieceType.Rook;
    }
    public void SaveGameToSlot(int slot)
    {
        string fen = BoardToFEN();
        string baseKey = $"ChessSave_{slot}";

        PlayerPrefs.SetString(baseKey, fen);
        PlayerPrefs.SetString(baseKey + "_Time", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.SetInt(baseKey + "_Moves", moveList.Count);

        PlayerPrefs.Save();

        Debug.Log($"Game saved to slot {slot}: {fen}");
    }
    public void LoadGameFromSlot(int slot)
    {
        string key = $"ChessSave_{slot}";

        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"No save found in slot {slot}");
            return;
        }

        string fen = PlayerPrefs.GetString(key);
        LoadFromFEN(fen);

        Debug.Log($"Game loaded from slot {slot}");
    }
    public void SaveSlotAndResume(int slot)
    {
        SaveGameToSlot(slot);
        SetMenuState(MenuState.None);
    }
    public void LoadSlotAndResume(int slot)
    {
        LoadGameFromSlot(slot);
        SetMenuState(MenuState.None);
    }
    public bool HasSaveInSlot(int slot)
    {
        return PlayerPrefs.HasKey($"ChessSave_{slot}");
    }
    public void DeleteSlot(int slot)
    {
        string key = $"ChessSave_{slot}";
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            Debug.Log($"Slot {slot} deleted");
        }
    }

    // AI
    private bool IsAITurn()
    {
        if (aiColor == AIColor.White)
            return isWhiteTurn;
        else
            return !isWhiteTurn;
    }
    private void SetupCameraForPlayer()
    {
        if (aiColor == AIColor.White)
        {
            // AI fehér -> játékos fekete
            mainCamera.gameObject.SetActive(false);
            secondaryCamera.gameObject.SetActive(true);
            mainCamera.GetComponent<AudioListener>().enabled = false;
            secondaryCamera.GetComponent<AudioListener>().enabled = true;
        }
        else
        {
            // AI fekete -> játékos fehér
            mainCamera.gameObject.SetActive(true);
            secondaryCamera.gameObject.SetActive(false);
            mainCamera.GetComponent<AudioListener>().enabled = true;
            secondaryCamera.GetComponent<AudioListener>().enabled = false;
        }
    }
    public async void RequestAIMove()
    {
        if (!IsAITurn()) return; // AI fekete

        string fen = BoardToFEN();
        Debug.Log("FEN sent to Stockfish: " + fen);

        int thinkTime = aiDifficulty switch
        {
            AIDifficulty.Easy => 200,
            AIDifficulty.Medium => 500,
            AIDifficulty.Hard => 1000,
            AIDifficulty.Expert => 2000,
            _ => 500
        };

        string bestMove = await stockfish.GetBestMove(fen, thinkTime);
        Debug.Log("Stockfish move: " + bestMove);

        // pl. e2e4
        Vector2Int from = UCIToPosition(bestMove[0], bestMove[1]);
        Vector2Int to = UCIToPosition(bestMove[2], bestMove[3]);

        bool isPromotion = bestMove.Length == 5;
        char promotionChar = isPromotion ? bestMove[4] : ' ';

        ChessPiece aiPiece = chessPieces[from.x, from.y];
        if (aiPiece == null)
        {
            Debug.LogError("AI piece not found!");
            return;
        }

        availableMoves = aiPiece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
        specialMove = aiPiece.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

        MoveTo(aiPiece, to.x, to.y);

        // AI promóció kezelése
        if (isPromotion)
        {
            PromoteAIPawn(aiPiece, promotionChar);
        }
    }
    private void PromoteAIPawn(ChessPiece pawn, char promotionChar)
    {
        ChessPieceType newType = promotionChar switch
        {
            'q' => ChessPieceType.Queen,
            'r' => ChessPieceType.Rook,
            'b' => ChessPieceType.Bishop,
            'n' => ChessPieceType.Knight,
            _ => ChessPieceType.Queen
        };

        int x = pawn.currentX;
        int y = pawn.currentY;
        int team = pawn.team;

        // Régi gyalog törlése
        Destroy(pawn.gameObject);
        chessPieces[x, y] = null;

        // Új figura létrehozása
        ChessPiece newPiece = SpawnSinglePiece(newType, team);
        newPiece.currentX = x;
        newPiece.currentY = y;
        newPiece.transform.position = GetTileCenter(x, y);
        chessPieces[x, y] = newPiece;
    }
    private void ApplyAIDifficulty()
    {
        switch (aiDifficulty)
        {
            case AIDifficulty.Easy:
                stockfish.SetSkillLevel(3);
                break;

            case AIDifficulty.Medium:
                stockfish.SetSkillLevel(8);
                break;

            case AIDifficulty.Hard:
                stockfish.SetSkillLevel(14);
                break;

            case AIDifficulty.Expert:
                stockfish.SetSkillLevel(20);
                break;
        }
    }
}
