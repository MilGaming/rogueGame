using TMPro;
using UnityEngine;



public class PopUpCreator : MonoBehaviour
{
    [SerializeField] GameObject damagePopupPrefab;
    
    public void CreatePopUp(float damage, Vector3 position, int type)
    {   
        float offset = 0f;
        switch (type)
        {
            case 1:
                offset = Random.Range(-1.0f,0.0f);
                break;
            case 2:
                offset = Random.Range(-1.0f,1.0f);
                break;
            case 3:
                offset = Random.Range(0.0f, 0.5f);
                break;
            case 4:
                offset = Random.Range(0.5f, 1.5f);
                break;
            case 5:
                offset = 2.5f;
                break;
        }
        var pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
        GameObject popup = Instantiate(damagePopupPrefab, pos, Quaternion.identity);

        popup.GetComponent<DamagePopup>().Setup(damage, type);
    }

}