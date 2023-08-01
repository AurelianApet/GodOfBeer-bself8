using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductEvent : MonoBehaviour
{
    public int pId;
    public GameObject manager;
    private bool lang_type = false;//korean
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void onChangeLanguage()
    {
        manager.GetComponent<MainManager>().onChangeLanguage(pId, lang_type);
        lang_type = !lang_type;
    }

    public void onSoldout()
    {
        manager.GetComponent<MainManager>().Soldout(pId);
    }

    public void onSet()
    {
        manager.GetComponent<MainManager>().editSet(pId);
    }

    public void onSave()
    {
        manager.GetComponent<MainManager>().saveSet(pId);
    }

    public void onConfirmSavePopup()
    {
        manager.GetComponent<MainManager>().closeSavePopup(pId);
    }

    public void onWash()
    {
        manager.GetComponent<MainManager>().Wash(pId);
    }

    public void onConfirmWashPopup()
    {
        manager.GetComponent<MainManager>().onConfirmWashPopup(pId);
    }

    public void onChangeBottle()
    {
        manager.GetComponent<MainManager>().BottleChange(pId);
    }

    public void onConfirmBottlePopup()
    {
        manager.GetComponent<MainManager>().onConfirmBottlePopup(pId);
    }

    public void onConfirmBottleInitPopup()
    {
        manager.GetComponent<MainManager>().onConfirmBottleInitPopup(pId);
    }

    public void onCancelBottleInitPopup()
    {
        manager.GetComponent<MainManager>().onCancelBottleInitPopup(pId);
    }

    public void closeErrorPopup()
    {
        manager.GetComponent<MainManager>().closeErrorPopup(pId);
    }

    public void onBack()
    {
        manager.GetComponent<MainManager>().onBack(pId);
    }

    public void onBottomLeft()
    {
        manager.GetComponent<MainManager>().onBottomLeft(pId);
    }

    public void onBottomRight()
    {
        manager.GetComponent<MainManager>().onBottomRight(pId);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
