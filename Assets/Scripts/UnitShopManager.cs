using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class UnitShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    string userKey;
    int currentCoin;

    Dictionary<string, bool> unitList =
        new Dictionary<string, bool>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shingugimalgosa-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadUserData();
    }
    void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        reference.Child("UserInfo")
            .Child(userKey)
            .GetValueAsync()
            .ContinueWith(task =>
            {
                DataSnapshot snapshot = task.Result;

                currentCoin =
                    int.Parse(snapshot.Child("Coin").Value.ToString());

                string unitJson =
                    snapshot.Child("UnitList").Value.ToString();

                unitList =
                    JsonConvert.DeserializeObject
                    <Dictionary<string, bool>>(unitJson);

                dispatcher.Enqueue(() =>
                {
                    CoinText.text = "Coin : " + currentCoin;
                });
            });
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
    void BuyUnit(string unitName, int price)
    {
        if (unitList[unitName])
        {
            MessageText.text =
                "이미 보유한 유닛입니다.";

            return;
        }

        if (currentCoin < price)
        {
            MessageText.text =
                "코인이 부족합니다.";

            return;
        }

        currentCoin -= price;

        unitList[unitName] = true;

        SaveUnitData(unitName);
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
                dispatcher.Enqueue(() =>
                {
                    CoinText.text =
                        "Coin : " + currentCoin;

                    MessageText.text =
                        unitName + " 구매 완료";
                });
            });
    }
}