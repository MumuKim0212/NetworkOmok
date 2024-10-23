using System;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    enum State
    {
        Intro = 0,
        WaitingForConnection,
        Start,
        Game,
        End,
    };
    enum Stone
    {
        None = 0,
        White,
        Black,
    };

    [SerializeField] int port = 10000;
    [SerializeField] int boardSize = 19;    // 19x19 바둑판
    [SerializeField] float cellSize = 50f;  // 각 셀의 크기
    [SerializeField] float stoneSize = 40f; // 돌의 크기

    [SerializeField] InputField ip;
    [SerializeField] Texture texBoard;
    [SerializeField] Texture texWhite;
    [SerializeField] Texture texBlack;
    [SerializeField] UIManager uiManager;


    Tcp tcp;
    int[] board;
    State state;
    Stone stoneTurn;
    Stone stoneI;
    Stone stoneYou;
    Stone stoneWinner;

    Vector2 boardOffset = new Vector2(55f, 50f); // 보드의 시작 위치

    private void Start()
    {
        tcp = GetComponent<Tcp>();
        state = State.Intro;
        board = new int[boardSize * boardSize];

        for (int i = 0; i < board.Length; i++)
        {
            board[i] = (int)Stone.None;
        }

        if (!uiManager)
            uiManager = FindObjectOfType<UIManager>();
    }

    public void ServerStart()
    {
        if (tcp.StartServer(port, 10))
        {
            state = State.WaitingForConnection;
            uiManager.ShowWaitingPanel("상대방의 접속을 기다리는 중...");
        }
        else
        {
            uiManager.ShowStatus("서버 시작 실패");
            uiManager.ShowConnectionPanel();
        }
    }

    public void ClientStart()
    {
        if (tcp.Connect(ip.text, port))
        {
            state = State.WaitingForConnection;
            uiManager.ShowWaitingPanel("클라이언트 접속 성공");
        }
        else
        {
            uiManager.ShowStatus("클라이언트 접속 실패");
            uiManager.ShowConnectionPanel();
        }
    }

    private void OnGUI()
    {
        if (state != State.Game)
            return;
        if (!Event.current.type.Equals(EventType.Repaint))
            return;

        float boardWidth = cellSize * boardSize;
        float boardHeight = cellSize * boardSize;

        // 바둑판 그리기
        Graphics.DrawTexture(new Rect(boardOffset.x, boardOffset.y, boardWidth, boardHeight), texBoard);

        // 돌 그리기
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                int index = y * boardSize + x;
                if (board[index] != (int)Stone.None)
                {
                    float posX = boardOffset.x + x * cellSize - stoneSize / 2 + cellSize / 2;
                    float posY = boardOffset.y + y * cellSize - stoneSize / 2 + cellSize / 2;

                    Texture tex = (board[index] == (int)Stone.White) ? texWhite : texBlack;
                    Graphics.DrawTexture(new Rect(posX, posY, stoneSize, stoneSize), tex);
                }
            }
        }

        // 현재 턴 표시
        if (state == State.Game)
        {
            if (stoneTurn == Stone.White)
            {
                Graphics.DrawTexture(new Rect(boardOffset.x, boardHeight + boardOffset.y + 10, 40, 40), texWhite);
            }
            else
                Graphics.DrawTexture(new Rect(boardOffset.x + boardWidth - 40, boardHeight + boardOffset.y + 10, 40, 40), texBlack);
        }

        // 승자 표시
        if (state == State.End)
        {
            Texture winnerTex = (stoneWinner == Stone.White) ? texWhite : texBlack;
            Graphics.DrawTexture(new Rect(boardOffset.x + boardWidth / 2 - 20, boardHeight + boardOffset.y + 10, 40, 40), winnerTex);
        }
    }

    private void Update()
    {
        if (!tcp.IsConnect())
        {
            if (state != State.Intro && state != State.WaitingForConnection)
            {
                ShutdownGame();
            }
            return;
        }
        switch (state)
        {

            case State.WaitingForConnection:
                Debug.Log("연결?");
                if (tcp.IsConnect())
                {
                    Debug.Log("연결 성공");
                    UpdateStart();
                }
                break;
            case State.Game:
                UpdateGame();
                break;
            case State.End:
                UpdateEnd();
                break;
        }
    }

    public void ShutdownGame()
    {
        StartCoroutine(uiManager.ShowWaitingPanel("연결이 끊어졌습니다.", 2));
        uiManager.ShowConnectionPanel();
        tcp.StopServer();
        ResetGame();
    }

    private void UpdateStart()
    {
        state = State.Game;
        uiManager.ShowGamePanel();
        stoneTurn = Stone.Black;
        if (tcp.IsServer())
        {
            stoneI = Stone.Black;
            stoneYou = Stone.White;
            uiManager.ShowStatus("게임 시작! 당신이 흑입니다.");
        }
        else
        {
            stoneI = Stone.White;
            stoneYou = Stone.Black;
            uiManager.ShowStatus("게임 시작! 당신이 백입니다.");
        }
    }

    private void UpdateGame()
    {
        uiManager.UpdateTurnText(stoneTurn == stoneI);

        bool bSet = false;
        if (stoneTurn == stoneI)
        {
            bSet = MyTurn();
        }
        else
        {
            bSet = YourTurn();
        }
        if (bSet == false)
            return;

        stoneWinner = CheckBoard();
        if (stoneWinner != Stone.None)
        {
            state = State.End;
            uiManager.ShowWinner(stoneWinner == Stone.White);
            Debug.Log("승리 : " + (int)stoneWinner);
        }
        stoneTurn = (stoneTurn == Stone.White) ? Stone.Black : Stone.White;
    }
    public void ResetGame()
    {
        state = State.Intro;
        for (int i = 0; i < board.Length; i++)
        {
            board[i] = (int)Stone.None;
        }
        stoneTurn = Stone.None;
        stoneI = Stone.None;
        stoneYou = Stone.None;
        stoneWinner = Stone.None;
    }
    private Stone CheckBoard()
    {
        // 8방향 체크 (가로, 세로, 대각선)
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                int current = board[y * boardSize + x];
                if (current == (int)Stone.None) continue;

                // 8방향으로 5목 체크
                for (int dir = 0; dir < 8; dir++)
                {
                    int count = 1;
                    for (int i = 1; i <= 4; i++)
                    {
                        int nx = x + dx[dir] * i;
                        int ny = y + dy[dir] * i;

                        if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize)
                            break;

                        if (board[ny * boardSize + nx] != current)
                            break;

                        count++;
                    }

                    if (count == 5)
                        return (Stone)current;
                }
            }
        }
        return Stone.None;
    }

    private bool YourTurn()
    {
        byte[] data = new byte[2]; // x, y 좌표를 위해 2바이트
        int iSize = tcp.Receive(ref data, data.Length);
        if (iSize <= 0)
            return false;

        int x = data[0];
        int y = data[1];
        int index = y * boardSize + x;

        Debug.Log($"받음 : ({x}, {y})");

        bool ret = SetStone(index, stoneYou);
        if (ret == false)
            return false;

        return true;
    }

    bool SetStone(int index, Stone stone)
    {
        if (index < 0 || index >= board.Length)
            return false;

        if (board[index] == (int)Stone.None)
        {
            board[index] = (int)stone;
            return true;
        }
        return false;
    }

    Vector2Int ScreenToGridPosition(Vector3 screenPos)
    {
        float x = screenPos.x - boardOffset.x - 20; // 보드판에 맞춰 위치 조정
        float y = boardOffset.y + boardSize * cellSize - screenPos.y;

        // 가장 가까운 격자점으로 반올림
        int gridX = Mathf.RoundToInt(x / cellSize);
        int gridY = Mathf.RoundToInt(y / cellSize);

        // 유효한 범위 체크
        if (gridX < 0 || gridX >= boardSize || gridY < 0 || gridY >= boardSize)
            return new Vector2Int(-1, -1);

        return new Vector2Int(gridX, gridY);
    }

    private bool MyTurn()
    {
        if (!Input.GetMouseButtonDown(0))
            return false;

        Vector2Int gridPos = ScreenToGridPosition(Input.mousePosition);
        if (gridPos.x == -1)
            return false;

        int index = gridPos.y * boardSize + gridPos.x;
        bool bSet = SetStone(index, stoneI);
        if (bSet == false)
            return false;

        // 상대방에게 좌표 전송
        byte[] data = new byte[2];
        data[0] = (byte)gridPos.x;
        data[1] = (byte)gridPos.y;
        tcp.Send(data, data.Length);

        Debug.Log($"보냄 : ({gridPos.x}, {gridPos.y})");
        return true;
    }

    private void UpdateEnd()
    {
        // 게임 재시작 등의 기능 추가 가능
    }
}