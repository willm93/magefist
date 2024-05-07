using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] PlayerController pc;
    [SerializeField] GameObject groundUI;
    [SerializeField] GameObject wallUI;
    [SerializeField] TextMeshProUGUI speedUI;
    [SerializeField] TextMeshProUGUI YspeedUI;
    [SerializeField] TextMeshProUGUI moveStateUI;

    void Update()
    {
        groundUI.SetActive(pc.OnGround);
        wallUI.SetActive(pc.OnWall);

        speedUI.SetText("{0:2}\nSpeed", pc.CurrentSpeed);
        YspeedUI.SetText("{0:2}\nY Speed", pc.CurrentYSpeed);
        moveStateUI.SetText($"{pc.CurrentMoveState}");
    }
}
