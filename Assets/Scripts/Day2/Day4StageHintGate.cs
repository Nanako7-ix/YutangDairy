using UnityEngine;

public sealed class Day4StageHintGate : MonoBehaviour
{
    private const int HintCost = 5;

    private int currentDay;
    private GameManager gameManager;
    private bool guidanceUnlocked;
    private GUIStyle buttonStyle;

    public bool GuidanceVisible => currentDay != 4 || guidanceUnlocked;

    public static Day4StageHintGate Ensure(GameObject owner, int day, GameManager manager)
    {
        Day4StageHintGate gate = owner.GetComponent<Day4StageHintGate>();
        if (gate == null)
        {
            gate = owner.AddComponent<Day4StageHintGate>();
        }

        gate.currentDay = day;
        gate.gameManager = manager;
        gate.guidanceUnlocked = day != 4;
        return gate;
    }

    private void OnGUI()
    {
        if (currentDay != 4 || guidanceUnlocked)
        {
            return;
        }

        EnsureStyle();
        Rect buttonRect = new Rect(Screen.width - 230f, 32f, 180f, 48f);
        if (!GUI.Button(buttonRect, "\u63d0\u793a\uff08-5\u5206\uff09", buttonStyle))
        {
            return;
        }

        PurchaseHint();
    }

    public void PurchaseHint()
    {
        if (currentDay != 4 || guidanceUnlocked)
        {
            return;
        }

        if (gameManager != null)
        {
            int actualCost = Mathf.Min(HintCost, gameManager.CurrentScore);
            gameManager.AddScore(-actualCost);
        }

        guidanceUnlocked = true;
    }

    private void EnsureStyle()
    {
        if (buttonStyle != null)
        {
            return;
        }

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
    }
}
