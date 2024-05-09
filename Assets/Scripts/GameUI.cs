using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] PlayerController pc;
    [SerializeField] Dashing dasher;
    [SerializeField] ChargePunching charger;
    [SerializeField] GameObject groundElement;
    [SerializeField] GameObject wallElement;
    [SerializeField] GameObject punchCDElement;
    [SerializeField] GameObject dashCDElement;
    [SerializeField] TextMeshProUGUI speedElement;
    [SerializeField] TextMeshProUGUI YspeedElement;
    [SerializeField] TextMeshProUGUI moveStateElement;
    [SerializeField] GameObject chargeElement;
    [SerializeField] RectTransform chargeBar;

    void Update()
    {
        groundElement.SetActive(pc.OnGround);
        wallElement.SetActive(pc.OnWall);
        punchCDElement.SetActive(charger.PunchOffCooldown);
        dashCDElement.SetActive(dasher.DashOffCooldown);

        speedElement.SetText("{0:2}\nSpeed", pc.CurrentSpeed);
        YspeedElement.SetText("{0:2}\nY Speed", pc.CurrentYSpeed);
        moveStateElement.SetText($"{pc.CurrentMoveState}");

        chargeElement.SetActive(pc.CurrentMoveState == MoveState.Charging);
        if (chargeElement.activeInHierarchy) {
            chargeBar.localScale = new Vector3(1, charger.ChargePercent, 1);
        }
    }
}
