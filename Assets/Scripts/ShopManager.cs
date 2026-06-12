using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;
public class ShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    string userKey;

    int currentCoin;
    Dictionary<string, int> inventory = new Dictionary<string, int>();
    Dictionary<string, bool> unitList = new Dictionary<string, bool>();
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shingugimalgosa-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadUserData();

    }

    public void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "유저 정보 불러오기 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                string inventoryJson = snapshot.Child("Inventory").Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
                string unitJson = snapshot.Child("UnitList").Value.ToString();

                unitList = JsonConvert.DeserializeObject<Dictionary<string, bool>>(unitJson);
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "유저 정보 불러오기 성공";
                });
            }
        });
    }

    void RefreshUI()
    {
        CoinText.text = "Coin : " + currentCoin;
    }

    public void OnClickBuySword()
    {
        BuyItem("Sword", 50);
    }

    public void OnClickBuyPoison()
    {
        BuyItem("Poison", 30);
    }

    public void OnClickBuyAxe()
    {
        BuyItem("Axe", 10);
    }

    public void OnClickBuyUnit2()
    {
        BuyUnit("Unit2", 100);
    }

    public void OnClickBuyUnit3()
    {
        BuyUnit("Unit3", 200);
    }

    public void OnClickBuyUnit4()
    {
        BuyUnit("Unit4", 300);
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }
        currentCoin -= price;
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory.Add(itemName, 1);
        }
        SaveUserData(itemName);
    }

    void BuyUnit(string unitName, int price)
    {
        if (unitList.ContainsKey(unitName) && unitList[unitName])
        {
            MessageText.text = "이미 보유한 유닛입니다.";
            return;
        }

        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;
        unitList[unitName] = true;

        SaveUnitData(unitName);
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();

        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "구매 저장 실패";
                });
                return;
            }
            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = boughtItemName + " 구매 완료";

                    InventoryManager inventoryManager =
                    FindFirstObjectByType<InventoryManager>();

                    if (inventoryManager != null)
                    {
                        inventoryManager.LoadInventory();
                    }
                });
            }
        });
    }

    void SaveUnitData(string unitName)
    {
        string unitJson =
            JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> updateData =
            new Dictionary<string, object>();

        updateData["Coin"] = currentCoin;
        updateData["UnitList"] = unitJson;

        reference.Child("UserInfo")
            .Child(userKey)
            .UpdateChildrenAsync(updateData)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "유닛 구매 실패";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text =
                    unitName + " 구매 완료";
                });
            });
    }
}
