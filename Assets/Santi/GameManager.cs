using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int scoreP1, scoreP2;
    public Text uiScore;
    public string p1Name = "Player1";
    public string p2Name = "Player2";

    public void OnPlayerFell(string who)
    {
        if (who.Contains("1")) scoreP2++; else scoreP1++;
        uiScore.text = $"{p1Name}: {scoreP1}  |  {p2Name}: {scoreP2}";
    }
}
