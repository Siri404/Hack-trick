using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ChatManager : MonoBehaviour
{
    public static ChatManager instance { get; set; }
    
    [FormerlySerializedAs("chatPannel")] [SerializeField]
    private GameObject chatPanel;

    [SerializeField]
    private GameObject textObject;

    private List<Message> messages = new List<Message>(26);
    private int messageLimit = 25;

    public void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void SendToActionLog(string text)
    {
        if (messages.Count >= messageLimit)
        {
            Destroy(messages[0].TextObject.gameObject);
            messages.Remove(messages[0]);
        }
        Message newMessage = new Message();
        newMessage.Text = text;
        GameObject newText = Instantiate(textObject, chatPanel.transform);
        newMessage.TextObject = newText.GetComponent<TMP_Text>();
        newMessage.TextObject.text = text;
        messages.Add(newMessage);
    }
}

public class Message
{
    public string Text;
    public TMP_Text TextObject;
}
