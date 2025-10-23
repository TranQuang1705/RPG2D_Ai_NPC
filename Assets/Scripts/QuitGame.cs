using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
        // Thoát game
        Application.Quit();

        // In ra console (chỉ có tác dụng trong Editor để kiểm tra)
        Debug.Log("Game is quitting...");
    }
}
