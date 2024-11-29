using Auxiliars;
using RDG;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    public Piece nextPiece { get; private set; }
    public Piece savedPiece { get; private set; }

    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Vector3Int spawnPosition = new Vector3Int(-1, 8, 0);
    public Vector3Int holdiPosition = new Vector3Int(-4, 10, 0);
    public Vector3Int previewPosition = new Vector3Int(3, 11, 0);
    public Vector3Int previewiPosition = new Vector3Int(2, 10, 0);
    public Vector3Int holdPosition = new Vector3Int(-4, 11, 0);

    public Vector3Int[] cells { get; private set; }
    public TetrominoData data;
    private int lineClears;
    private bool pieceHeld = false;
    public static bool pieceSwapped;
    private Score m_score;
    Menu menu;

    [SerializeField]
    private TileBase m_ghostTile;
    public bool FallingTilesAllowed => !this.m_clearAnimationTimer.Started;
    private SpartanTimer m_clearAnimationTimer;
    private SoundBoard m_tetrisSoundBoard;

    private List<int> m_rowsToClear;

    private Dictionary<Vector3Int, TileBase> m_tilemapAfterDeletion;
    private Dictionary<Vector3Int, TileBase> m_cachedTiles;

    private int m_comboCount;
    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    public RectInt NoTopBounds //allows pieces to occupy 1 tile above main board
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, new Vector2Int(10, 21));
        }
    }

    private int maximumMarkedTileYPos;

    private void Awake()
    {
        const int tilesGuess = 200;
        this.m_tilemapAfterDeletion = new Dictionary<Vector3Int, TileBase>(tilesGuess);
        this.m_cachedTiles = new Dictionary<Vector3Int, TileBase>(tilesGuess);
        this.m_comboCount = 0;

        this.m_clearAnimationTimer = new SpartanTimer(TimeMode.RealTime);
        this.m_tetrisSoundBoard = GetComponent<SoundBoard>();
        menu = GameObject.FindAnyObjectByType<Menu>();
        m_score = GameObject.FindObjectOfType<Score>();
        tilemap = GetComponentInChildren<Tilemap>();
        activePiece = GetComponentInChildren<Piece>();
        nextPiece = gameObject.AddComponent<Piece>();
        nextPiece.enabled = false;
        savedPiece = gameObject.AddComponent<Piece>();
        savedPiece.enabled = false;


        for (int i = 0; i < tetrominoes.Length; i++)
        {
            tetrominoes[i].Initialize();
        }
        
    }

    private void SetTilesFlags()
    {
        RectInt bounds = this.Bounds;
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            for (int row = bounds.yMin; row < bounds.yMax; row++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                this.tilemap.SetTileFlags(position, TileFlags.None);
            }
        }
    }

    public void Start()
    {
        Piece.startTouchPosition.Set(0, -10000); //prevent hard drop of first piece after gameover
        pieceSwapped = false;
        pieceHeld = false;
        m_score.ResetScore();
        m_score.SetLevel(1);
        tilemap.ClearAllTiles();
        lineClears = 0;
        SetNextPiece();
        SpawnPiece();
    }

    private void Update()
    {
        if (!this.m_clearAnimationTimer.Started) return;
        Color color = Color.Lerp(Color.white, new Color(0, 0f, 0f, 0f), this.m_clearAnimationTimer.CurrentTimeSeconds * 2f);
        //Now, here we'll iterate over our entries and lerp the colour to alpha 0 in 0.5s
        this.m_tilemapAfterDeletion.ToList()
            .ForEach(entry =>
            {
                this.tilemap.SetTile(entry.Key, entry.Value);
                this.tilemap.SetTileFlags(entry.Key, TileFlags.None);
                this.tilemap.SetColor(entry.Key, color);
            });
    }

    private void SetNextPiece()
    {
        // Clear the existing piece from the board
        if (nextPiece.cells != null)
        {
            Clear(nextPiece);
        }

        // Pick a random tetromino to use
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];

        // Initialize the next piece with the random data
        // Draw it at the "preview" position on the board
        if (random == 0)
        {
            nextPiece.Initialize(this, previewiPosition, data);
        }
        else
        {
            nextPiece.Initialize(this, previewPosition, data);
        }
        
        Set(nextPiece);
    }

    public void SpawnPiece()
    {
        // Initialize the active piece with the next piece data
        activePiece.Initialize(this, spawnPosition, nextPiece.data);

        // Only spawn the piece if valid position otherwise game over
        if (IsValidPosition(activePiece, spawnPosition))
        {
            Set(activePiece);
        }
        else
        {
            GameOver();
        }

        // Set the next random piece
        SetNextPiece();
    }

    public void SwapPiece()
    {
        // Temporarily store the current saved data so we can swap
        TetrominoData savedData = savedPiece.data;
        TetrominoData currentPiece = activePiece.data;

        if ((pieceSwapped || savedPiece.data.tile == activePiece.data.tile) && pieceHeld)
        {
            return;
        }
        pieceSwapped = true;

        // Clear the existing saved piece from the board
        if (savedData.cells != null)
        {
            Clear(savedPiece);
        }

        // Store the active piece as the new saved piece
        // Draw this piece at the "hold" position on the board
        if (currentPiece.tile == tetrominoes[0].tile) //checks For I piece
        {
            savedPiece.Initialize(this, holdiPosition, currentPiece); //I piece is set to a different position
        }
        else
        {
            savedPiece.Initialize(this, holdPosition, currentPiece);
        }

        Set(savedPiece);

        // Swap the saved piece to be the active piece
        if (pieceHeld)
        {
            // Clear the existing active piece before swapping
            Clear(activePiece);

            // Re-initialize the active piece with the saved data
            activePiece.Initialize(this, spawnPosition, savedData);
            Set(activePiece);
        }
        else
        {
            SpawnPiece();
        }
        pieceHeld = true;
    }

    public void GameOver()
    {
        menu.TransferScore();

        tilemap.ClearAllTiles();
        m_score.ResetScore();
        m_score.SetLevel(1);
        lineClears = 0;

        menu.ShowGameOver();
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = NoTopBounds;

        // The position is only valid if every cell is valid
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + position;

            // An out of bounds tile is invalid
            if (!bounds.Contains((Vector2Int)tilePosition))
            {
                return false;
            }

            // A tile already occupies the position, thus invalid
            if (tilemap.HasTile(tilePosition))
            {
                return false;
            }
        }

        return true;
    }

    public void ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;
        bool lineCleared = false;
        this.GetFullRowsIntoDict();
        // Clear from bottom to top
        while (row < bounds.yMax)
        {
            // Only advance to the next row if the current is not cleared
            // because the tiles above will fall down when a row is cleared
            if (IsLineFull(row))
            {
                LineClear(row);
                lineCleared = true;
            }
            else
            {
                row++;
            }
        }
        if (!lineCleared)
        {
            this.m_comboCount = 0;
            m_score.ResetMultiplier();
            this.SpawnPiece();
        }
        else
        {
            this.m_comboCount++;
            if (m_comboCount > 1)
            {
                this.m_score.SendCombo($"Combo x{this.m_comboCount}");
            }
            this.m_clearAnimationTimer.Reset();
            this.m_tetrisSoundBoard.PlaySound(TetrisSound.ClearLine);
            Vibration.Vibrate(500);

            TimeController.StopTimeFor(0.5f, () =>
            {
                foreach (var entry in this.m_cachedTiles)
                {
                    this.tilemap.SetTile(entry.Key, entry.Value);
                    this.tilemap.SetTileFlags(entry.Key, TileFlags.None);
                    this.tilemap.SetColor(entry.Key, Color.white);
                }
                this.m_tilemapAfterDeletion.Clear();
                this.m_cachedTiles.Clear();
                this.m_clearAnimationTimer.Stop();
                this.SpawnPiece();
            });
            
        }
    }

    private void GetFullRowsIntoDict()
    {
        const int maxTilesPerRow = 10;
        List<Vector3Int> positionsToAdd = new List<Vector3Int>(maxTilesPerRow);
        RectInt bounds = this.Bounds;
        for (int row = bounds.yMin; row < bounds.yMax; row++)
        {
            positionsToAdd.Clear();
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                if (!this.tilemap.HasTile(position))
                {
                    positionsToAdd.Clear();
                    break;
                }
                //Row is full
                positionsToAdd.Add(position);
            }
            positionsToAdd.ForEach(position => this.m_tilemapAfterDeletion[position] = this.m_ghostTile);
        }
        const int rowsToTetris = 4;
        if (this.m_tilemapAfterDeletion.Count / maxTilesPerRow >= rowsToTetris)
        {
            this.m_score.SendCombo("Tetris!!!");
        } 
    }

    public bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            // The line is not full if a tile is missing
            if (!tilemap.HasTile(position))
            {
                return false;
            }
        }

        return true;
    }

    public void LineClear(int row) {
        m_score.AddScore(1000);
        m_score.AddMultiplier(1);
        lineClears++;

        if (lineClears > 10)
        {
            m_score.AddLevel(1);
            lineClears = 0;
        }

        RectInt bounds = Bounds;
        this.ClearLineInMap(row, bounds);
    }

    private void ClearLineInMap(int row, RectInt bounds)
    {

        // Clear all tiles in the row
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            tilemap.SetTile(position, null);
            //Record the tilemap's entries to be 
        }
        // Shift every row above down one
        for (int currentRow = row; currentRow < bounds.yMax; currentRow++)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, currentRow + 1, 0);
                TileBase above = tilemap.GetTile(position);
                position = new Vector3Int(col, currentRow, 0);
                this.tilemap.SetTile(position, above);
                this.m_cachedTiles[position] = above;
            }
        }
    }


    public void Save()
    {
        Clear(activePiece);
        m_score.SaveScore();
        if (pieceHeld)
        {
            for (int i = 0; i < 7; i++)
            {
                if (savedPiece.data.tile == tetrominoes[i].tile)
                {
                    PlayerPrefs.SetInt("savedPiece", 1);
                    PlayerPrefs.SetInt("savedPieceTile", i);
                    break;
                }
            }
        }
        else
        {
            PlayerPrefs.SetInt("savedPiece", 0);
        }
        RectInt bounds = Bounds;
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            for (int row = bounds.yMin; row < bounds.yMax; row++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                TileBase temp = tilemap.GetTile(position);
                if (tilemap.HasTile(position))
                {
                    PlayerPrefs.SetInt(col + "_pos_" + row, 1);
                    for (int i = 0; i < 7; i++) //keep checking until the right tile is found
                    {
                        if (temp == tetrominoes[i].tile)
                        {
                            PlayerPrefs.SetInt(col + "_color_" + row, i); //save sprite tile info for every position
                            break;
                        }
                    }
                }
                else
                {
                    PlayerPrefs.SetInt(col + "_pos_" + row, 0);
                }
            }
        }
    }

    public void Load()
    {
        m_score.LoadScore();
        if (PlayerPrefs.GetInt("savedPiece") == 1)
        {
            TetrominoData data = tetrominoes[PlayerPrefs.GetInt("savedPieceTile")];
            // Draw this piece at the "hold" position on the board
            if (data.tile == tetrominoes[0].tile) //checks For I piece
            {
                savedPiece.Initialize(this, holdiPosition, data); //I piece is set to a different position
            }
            else
            {
                savedPiece.Initialize(this, holdPosition, data);
            }
            Set(savedPiece);
            pieceHeld = true;
        }
        RectInt bounds = Bounds;
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            for (int row = bounds.yMin; row < bounds.yMax; row++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                int tempTile = PlayerPrefs.GetInt(col + "_color_" + row);
                if (PlayerPrefs.GetInt(col + "_pos_" + row) == 1)
                {
                    tilemap.SetTile(position, tetrominoes[tempTile].tile);
                }
                else
                {
                    tilemap.SetTile(position, null);
                }
            }
        }
    }
}