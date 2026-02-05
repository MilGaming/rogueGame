using UnityEngine;
using UnityEngine.UI;

public class ChargeUp : MonoBehaviour
{
    Image chargeBar;
    bool isCharging;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        chargeBar = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isCharging)
        {
            chargeBar.fillAmount += Time.deltaTime*2.5f;
            if(chargeBar.fillAmount == 1)
            {
                chargeBar.color = Color.green;
            }
        }
        else
        {
            chargeBar.fillAmount = 0;
            chargeBar.color = Color.red;
        }
    }

    public void SetChargeBar(bool charging)
    {   
        isCharging = charging;
    }
}

