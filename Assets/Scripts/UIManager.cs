using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Connection UI")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private InputField ipInputField;
    [SerializeField] private Text statusText;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button shutdownButton;

    [Header("Game UI")]
    [SerializeField] private Text turnText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Game gameManager;

    private void Start()
    {
        if (!gameManager)
            gameManager = FindObjectOfType<Game>();

        // 버튼 이벤트 연결
        serverButton.onClick.AddListener(ServerButtonClick);
        clientButton.onClick.AddListener(ClientButtonClick);
        shutdownButton.onClick.AddListener(ShutdownGame);

        // 초기 UI 설정
        ShowConnectionPanel();
        waitingPanel.SetActive(false);
        gamePanel.SetActive(false);
        winnerText.gameObject.SetActive(false);
    }

    private void ServerButtonClick()
    {
        gameManager.ServerStart();
    }

    private void ClientButtonClick()
    {
        if (string.IsNullOrEmpty(ipInputField.text))
        {
            StartCoroutine(ShowWaitingPanel("IP 주소를 입력해주세요.", 3));
            return;
        }

        gameManager.ClientStart();
    }

    public void ShowConnectionPanel()
    {
        connectionPanel.SetActive(true);
        waitingPanel.SetActive(false);
        gamePanel.SetActive(false);
    }

    public void ShowWaitingPanel(string message)
    {
        connectionPanel.SetActive(false);
        waitingPanel.SetActive(true);
        gamePanel.SetActive(false);
        statusText.text = message;
    }

    public void ShowGamePanel()
    {
        connectionPanel.SetActive(false);
        waitingPanel.SetActive(false);
        gamePanel.SetActive(true);
    }
    public IEnumerator ShowWaitingPanel(string message, float time)
    {
        waitingPanel.SetActive(true);
        statusText.text = message;
        yield return new WaitForSeconds(time);
        waitingPanel.SetActive(false);
    }
    public void ShowStatus(string message)
    {
        statusText.text = message;
    }

    public void UpdateTurnText(bool isWhiteTurn)
    {
        if (isWhiteTurn)
            turnText.text = "Your Turn";
        else
            turnText.text = "Wait";
    }

    public void ShowWinner(bool isWhiteWin)
    {
        winnerText.gameObject.SetActive(true);
        winnerText.text = $"게임 종료! {(isWhiteWin ? "백" : "흑")} 승리!";
    }

    public void ShutdownGame()
    {
        winnerText.text = "";
        gameManager.ShutdownGame();
    }
}