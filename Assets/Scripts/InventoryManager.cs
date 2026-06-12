using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text SwordCountText;
    [SerializeField] Text PoisonCountText;
    [SerializeField] Text AxeCountText;
    [SerializeField] Text MessageText;

    string userKey;

    Dictionary<string, int> inventory = new Dictionary<string, int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shingugimalgosa-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

    }

    public void LoadInventory()
    {
        inventory.Clear();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).Child("Inventory").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 불러오기 실패";
                });
                return;
            }
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 데이터가 없습니다.";
                    });
                    return;
                }

                string inventoryJson = snapshot.Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 불러오기 완료";
                });
            }

        });
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }
        return 0;
    }

    void RefreshUI()
    {
        SwordCountText.text = "Sword : " + GetItemCount("Sword");
        PoisonCountText.text = "Poison : " + GetItemCount("Poison");
        AxeCountText.text = "Axe : " + GetItemCount("Axe");
    }

    void UseItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + "아이템이 없습니다.";
            return;
        }

        if (inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "개수가 부족합니다.";
            return;
        }

        inventory[itemName]--;

        SaveInventory(itemName);
    }

    public void OnClickUseSword()
    {
        UseItem("Sword");
    }
    public void OnClickUsePoison()
    {
        UseItem("Poison");
    }
    public void OnClickUseAxe()
    {
        UseItem("Axe");
    }

    void SaveInventory(string userItemname)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").SetValueAsync(inventoryJson).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 저장 실패";
                });
                return;
            }
            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = userItemname + " 사용했다!";
                });
            }
        });
    }
}
