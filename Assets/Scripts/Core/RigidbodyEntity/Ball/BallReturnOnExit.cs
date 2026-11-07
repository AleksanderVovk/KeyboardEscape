using UnityEngine;

public class BallReturnOnExit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что вошёл именно шар
        if (!other.CompareTag("Ball"))
            return;

        // Переспавнить шар через BallSpawner
        if (BallSpawner.Instance != null)
        {
            BallSpawner.Instance.ResetBallToStart();
        }
        else
        {
            Debug.LogWarning("ExitTrigger: BallSpawner.Instance is null");
        }
    }
}