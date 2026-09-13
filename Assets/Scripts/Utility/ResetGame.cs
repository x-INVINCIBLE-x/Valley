using UnityEngine;

public class ResetGame : MonoBehaviour
{
    public void StartReset()
    {
        GameManager.Instance.GameReset();
    }
}
