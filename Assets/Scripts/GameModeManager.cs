using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

enum GameMode
{
    Marathon,
    Challenge
}

public class GameModeManager : MonoBehaviour {

    private static GameModeManager s_instance;

    [SerializeField]
    private GameMode m_currentGameMode;

    [SerializeField]
    private Board m_board;

    [SerializeField]
    private TextMeshPro m_movesText;
    private int m_moveCount;

    private void Awake() {
        s_instance = this;
    }

    public static void DecreaseMove()
    {
        if (s_instance.m_currentGameMode == GameMode.Challenge) return;
        s_instance.m_moveCount--;
        s_instance.m_movesText.text = $"Moves left: {s_instance.m_moveCount}";
        if (s_instance.m_moveCount <= 0)
        {
            s_instance.m_board.GameOver();
        }
    }

    public static void SetGoalCondition(System.Func<bool> condition)
    {

    }

}
