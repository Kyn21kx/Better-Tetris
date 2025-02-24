using RDG;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }

    public static float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float moveTime;
    private float lockTime;

    Score score;

    //Touch Input Variables
    public static Vector2 startTouchPosition;
    private Vector2 currentPosition;
    private Vector2 endTouchPosition;
    private bool moving = false;
    private bool movingDown = false;
    private float touchDelay = 0.2f;
    private float touchTime;
    public static float leftRightSensitivity = 65f;
    public static float hardDropSensitivity = 200f;
    private float tapRange = 65f;
    public static bool leftCounterclockwise = false;
    private static bool swap = false;

    private SoundBoard m_tetrisSoundBoard;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        score = GameObject.FindObjectOfType<Score>();
        this.m_tetrisSoundBoard = GetComponent<SoundBoard>();
        this.data = data;
        this.board = board;
        this.position = position;

        rotationIndex = 0;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;

        if (cells == null)
        {
            cells = new Vector3Int[data.cells.Length];
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        //TODO: Handle Pause input when tiles are not allowed to fall
        board.Clear(this);
        // We use a timer to allow the player to make adjustments to the piece
        // before it locks in place
        lockTime += Time.deltaTime;

        if (!Menu.gamePaused)
        {
            TouchInput();
            HandleKeyInputs();
            SwapThisFrame();
        }

        // Advance the piece to the next row every x seconds
        if (Time.time > stepTime)
        {
            Step();
        }

        board.Set(this);
    }

    private void TouchInput()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            startTouchPosition = Input.GetTouch(0).position;

        }
        if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Moved || moving))
        {
            currentPosition = Input.GetTouch(0).position;
            Vector2 distance = currentPosition - startTouchPosition;

            if (distance.x < -leftRightSensitivity && Mathf.Abs(distance.y) < Mathf.Abs(distance.x)) //detect swipe left
            {
                float tempDistance = distance.x;
                
                while (tempDistance < -leftRightSensitivity)
                {
                    Move(Vector2Int.left);
                    startTouchPosition.x -= leftRightSensitivity;
                    moving = true;
                    tempDistance = currentPosition.x - startTouchPosition.x;
                }
                
            }

            else if (distance.x > leftRightSensitivity && Mathf.Abs(distance.y) < Mathf.Abs(distance.x)) //detect swipe right
            {
                float tempDistance2 = distance.x;

                while (tempDistance2 > leftRightSensitivity)
                {
                    Move(Vector2Int.right);
                    startTouchPosition.x += leftRightSensitivity;
                    moving = true;
                    tempDistance2 = currentPosition.x - startTouchPosition.x;
                }
                
            }

            //To be valid, Y needs to be significantly bigger than X
            float diffVerticalToHorizontal = Mathf.Abs(distance.y) - Mathf.Abs(distance.x);
            const float thresholdDiff = 5f;

            if (movingDown || (distance.y < -hardDropSensitivity && diffVerticalToHorizontal > thresholdDiff)) //detect swipe down
            {
                Move(Vector2Int.down);
                if (!movingDown)
                {
                    touchTime = Time.time + touchDelay;
                }
                else if (distance.y < -hardDropSensitivity && Time.time >= touchTime)
                {
                    startTouchPosition.y = currentPosition.y;
                }
                moving = true;
                movingDown = true;

            }

            else if (!moving && distance.y > hardDropSensitivity && 120 > Mathf.Abs(distance.x)) //detect swipe up
            {
                board.SwapPiece();
            }
        }
        
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            endTouchPosition = Input.GetTouch(0).position;
            Vector2 Distance = endTouchPosition - startTouchPosition;
            if (Mathf.Abs(Distance.x) < tapRange && Mathf.Abs(Distance.y) < tapRange && endTouchPosition.y < Screen.height * 0.86 && !moving) //detect tap
            {
                if (leftCounterclockwise && endTouchPosition.x < Screen.width / 2)
                {
                    Rotate(-1);
                }
                else
                {
                    Rotate(1);
                }
            }
            else if (Distance.y < -hardDropSensitivity && Mathf.Abs(Distance.y) > Mathf.Abs(Distance.x) && Time.time < touchTime) //detect quick swipe down
            {
                HardDrop();
            }
            moving = false;
            movingDown = false;
        }
    }

    private void HandleKeyInputs()
    {
        // Handle rotation
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Rotate(-1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Rotate(1);
        }

        // Handle hard drop
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }

        // Allow the player to hold movement keys but only after a move delay
        // so it does not move too fast
        if (Time.time > moveTime)
        {
            HandleMoveInputs();
        }
    }

    private void HandleMoveInputs()
    {
        // Soft drop movement
        if (Input.GetKey(KeyCode.S))
        {
            if (Move(Vector2Int.down))
            {
                // Update the step time to prevent double movement
                stepTime = Time.time + stepDelay;
            }
        }

        // Left/right movement
        if (Input.GetKey(KeyCode.A))
        {
            Move(Vector2Int.left);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            Move(Vector2Int.right);
        }
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        // Step down to the next row
        Move(Vector2Int.down);

        // Once the piece has been inactive for too long it becomes locked
        if (lockTime >= lockDelay)
        {
            Lock();
        }
    }

    private void HardDrop()
    {
        while (Move(Vector2Int.down))
        {
            continue;
        }
        //After the drop we can send a teeeeny shake
        this.m_tetrisSoundBoard.PlaySound(TetrisSound.Drop);
        CameraActions.SendCameraShake(0.05f, 0.05f);
        TimeController.StopTimeFor(0.2f);
        //ASMR
        Lock();
    }

    private void Lock()
    {
        score.AddScore(10);
        Board.pieceSwapped = false;
        board.Set(this);
        board.ClearLines();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = board.IsValidPosition(this, newPosition);

        // Only save the movement if the new position is valid
        if (valid)
        {
            position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f; // reset
        }

        return valid;
    }

    private void Rotate(int direction)
    {
        Vibration.Vibrate(200);
        this.m_tetrisSoundBoard.PlaySound(TetrisSound.Rotate);
        // Store the current rotation in case the rotation fails
        // and we need to revert
        int originalRotation = rotationIndex;

        // Rotate all of the cells using a rotation matrix
        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        // Revert the rotation if the wall kick tests fail
        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix;

        // Rotate all of the cells using the rotation matrix
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];

            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    // "I" and "O" are rotated from an offset center point
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }

            cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];

            if (Move(translation))
            {
                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }

    public void SwapNextFrame()
    {
        swap = true;
    }

    private void SwapThisFrame()
    {
        if (swap)
        {
            board.SwapPiece();
            swap = false;
        }
    }
}