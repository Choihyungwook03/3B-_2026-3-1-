using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;

public class UserRegister : MonoBehaviour
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

    public void OnClickRegister()
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

            if (snapshot.HasChildren)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "이미 사용 중인 닉네임입니다.";
                });
                return;
            }
            CreateUser(nickname);
        });
    }

    // Update is called once per frame
    void CreateUser(string nickname)
    {
        DatabaseReference newUserRef = reference.Child("UserInfo").Push();

        string userKey = newUserRef.Key;

        UserData userData = new UserData(nickname);
        string json = JsonUtility.ToJson(userData);

        newUserRef.SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception);

                dispatcher.Enqueue(() =>
                {
                    checkText.text = "회원가입 실패";
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                PlayerPrefs.SetString("UserKey", userKey);
                PlayerPrefs.SetString("UserNickname", nickname);
                PlayerPrefs.Save();

                checkText.text = "회원가입 성공";
            });
        });

    }
}
