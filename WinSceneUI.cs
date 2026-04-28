using TMPro;
using UnityEngine;

public class WinSceneUI : MonoBehaviour
{
    [SerializeField] private TMP_Text winTimeText;

    private void Start()
    {
        float t = GameManager.LastRunSeconds;
        winTimeText.text = "Time: " + t.ToString("F2") + " s";
    }
}
