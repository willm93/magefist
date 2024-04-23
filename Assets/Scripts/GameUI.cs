using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] GameObject climbingUI;
    [SerializeField] GameObject groundUI;
    [SerializeField] GameObject wallUI;
    [SerializeField] TextMeshProUGUI speedUI;

    void Update()
    {
        climbingUI.SetActive(playerController.Climbing);
        groundUI.SetActive(playerController.OnGround);
        wallUI.SetActive(playerController.OnWall);

        speedUI.SetText("{0:3}\nSpeed", playerController.CurrentSpeed);
    }
}
