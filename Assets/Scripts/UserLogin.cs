using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;

public class UserLogin : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] InputField NickNameInput;
    [SerializeField] Text checkText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shingugimalgosa-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickLogin()
    {
        string nickname = NickNameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            checkText.text = "닉네임을 입력해주세요.";
            return;
        }
        reference.Child("UserInfo").OrderByChild("NickName").EqualTo(nickname).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "Firebase 읽기 오류";
                });
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.HasChildren)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "존재하지 않는 닉네임입니다.";
                });
                return;
            }

            foreach (DataSnapshot userSnapShot in snapshot.Children)
            {
                string userKey = userSnapShot.Key;

                dispatcher.Enqueue(() =>
                {
                    PlayerPrefs.SetString("UserKey", userKey);
                    PlayerPrefs.SetString("UserNickname", nickname);
                    PlayerPrefs.Save();

                    checkText.text = "로그인 성공";
                });
                break;
            }
        });
    }
}
