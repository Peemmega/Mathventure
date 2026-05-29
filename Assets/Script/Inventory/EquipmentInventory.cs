using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using static Utils;

public class EquipmentInventory : MonoBehaviour
{
    public static EquipmentInventory current;
    public MainGameUIController uIController;

    public event Action<GameObject> OnEquipmentSelectedItemChange;
    [field: SerializeField] public List<GameObject> SelectedTargets { get; private set; }
    [field: SerializeField]
    private GameObject equipmentSelectedItem;

    public GameObject EquipmentSelectedItem
    {
        get => equipmentSelectedItem;
        set
        {
            if (equipmentSelectedItem == value) return;
            equipmentSelectedItem = value;
            UpdateSelectedItemDescription(equipmentSelectedItem);
            OnEquipmentSelectedItemChange?.Invoke(equipmentSelectedItem);
        }
    }

    private bool isActive;
    [field: SerializeField] public GameObject HeroEquipmentMenu { get; private set; }
    [field: SerializeField] public TMP_Text EquipmentSelectedItemNameText { get; private set; }
    [field: SerializeField] public TMP_Text EquipmentSelectedItemNameShortdescriptionText { get; private set; }
    [field: SerializeField] public TMP_Text EquipmentSelectedItemNamedescriptionText { get; private set; }
    [field: SerializeField] public Button SellBtn { get; private set; }
    [field: SerializeField] public TMP_Text SellBtnText { get; private set; }

    void Awake()
    {
        current = this;
        GameController.current.OnGameStateChange += HandleGameStateChange;
    }

    //private void Start()
    //{
    //    isActive = gameObject.activeSelf;
    //}

    private void HandleGameStateChange(GameState gameState)
    {
        isActive = true;
        ToggleEquipmentMenuPopup();
    }


    private void UpdateSelectedItemDescription(GameObject equipmentSelectedItem)
    {
        if (equipmentSelectedItem == null)
        {
            EquipmentSelectedItemNameText.text = "itemName";
            EquipmentSelectedItemNameShortdescriptionText.text = "";
            EquipmentSelectedItemNamedescriptionText.text = "";
            return;
        }

        ItemData item = equipmentSelectedItem.GetComponent<ItemData>();

        item.SelectSFX.Play();
        EquipmentSelectedItemNameText.text = item.ItemName;
        EquipmentSelectedItemNameShortdescriptionText.text = item.ShortDescription;
        EquipmentSelectedItemNamedescriptionText.text = item.Description;

        if (equipmentSelectedItem != null)
        {
            SellBtn.interactable = true;
            SellBtnText.text = "ขาย(" + equipmentSelectedItem.GetComponent<ItemData>().SellPrice + "G)";
        }
        else
        {
            SellBtn.interactable = false;
            SellBtnText.text = "ขาย";
        }
    }

    public void SetInventorySelectItem(GameObject item)
    {
        if (item.GetComponent<ItemData>() == null)
        {
            Debug.Log(item + " dont have item data in it");
            return;
        }
        EquipmentSelectedItem = item;
    }

    private float nextUseTime;

    public void ToggleEquipmentMenuPopup()
    {
        if (Time.time < nextUseTime) return;
        nextUseTime = Time.time + 0.3f;

        isActive = !isActive;

        if (uIController == null)
            return;

        if (GameController.current.GameState == Utils.GameState.Shop)
        {
            uIController.CloseUI("heroEquipment");
            uIController.OpenUI("inventory");
            return;
        }
        Debug.Log(isActive);
        if (isActive)
        {
            uIController.OpenUI("inventory");
            uIController.OpenUI("heroEquipment");
        }
        else
        {
            uIController.CloseUI("inventory");
            uIController.CloseUI("heroEquipment");
        }
    }

    public void SellSelectItem()
    {
        Debug.Log(current.equipmentSelectedItem);
        if(current.equipmentSelectedItem != null)
        {
            ItemData itemData = current.equipmentSelectedItem.GetComponent<ItemData>();
            if(itemData != null)
            {
                InventoryController.current.AddCoin(itemData.SellPrice, true);
                Destroy(current.equipmentSelectedItem);
                current.equipmentSelectedItem = null;
            }
        }
    }
}
