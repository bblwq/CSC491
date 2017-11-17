using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;
using FullSerializer;

public class Test : MonoBehaviour {

    private string _username = "76175e2e-5b19-4046-bc1f-d40564593cdd";
    private string _password = "wFna6m5bYMaZ";
    private string _url = "https://gateway.watsonplatform.net/conversation/api";
    private string _workspaceId = "c7fb1332-b6b5-4057-8b5d-2f7b0b792b34";

    private Conversation _conversation;
    private string _conversationVersionDate = "2017-05-26";

    private string _question = "1";
    private fsSerializer _serializer = new fsSerializer();
    private Dictionary<string, object> _context = null;
    private bool _waitingForResponse = true;

    void Start()
    {
        LogSystem.InstallDefaultReactors();

        //  Create credential and instantiate service
        Credentials credentials = new Credentials(_username, _password, _url);

        _conversation = new Conversation(credentials);
        _conversation.VersionDate = _conversationVersionDate;

        Runnable.Run(Examples(_question));
    }

    private IEnumerator Examples(string question)
    {
        _waitingForResponse = true;
        AskQuestion("1");
        while (_waitingForResponse)
            yield return null;

        _waitingForResponse = true;
        AskQuestion("go to sushi house");
        while (_waitingForResponse)
            yield return null;

        Log.Debug("ExampleConversation", "Conversation examples complete.");
    }

    private void AskQuestion(string str)
    {
        MessageRequest messageRequest = new MessageRequest()
        {
            input = new Dictionary<string, object>()
            {
                { "text", str }
            },
            context = _context
        };

        if (!_conversation.Message(OnMessage, _workspaceId, messageRequest))
            Log.Debug("ExampleConversation", "Failed to message!");
    }

    private void OnMessage(object resp, string data)
    {
        Log.Debug("ExampleConversation", "Conversation: Message Response: {0}", data);

        //  Convert resp to fsdata
        fsData fsdata = null;
        fsResult r = _serializer.TrySerialize(resp.GetType(), resp, out fsdata);
        if (!r.Succeeded)
            throw new WatsonException(r.FormattedMessages);

        //  Convert fsdata to MessageResponse
        MessageResponse messageResponse = new MessageResponse();
        object obj = messageResponse;
        r = _serializer.TryDeserialize(fsdata, obj.GetType(), ref obj);
        if (!r.Succeeded)
            throw new WatsonException(r.FormattedMessages);

        //  Set context for next round of messaging
        object _tempContext = null;
        (resp as Dictionary<string, object>).TryGetValue("context", out _tempContext);

        if (_tempContext != null)
            _context = _tempContext as Dictionary<string, object>;
        else
            Log.Debug("ExampleConversation", "Failed to get context");
        _waitingForResponse = false;
    }

    public void showText() {
        //GameObject.Find("Response").GetComponent<Text>().text = GameObject.Find("TextField").GetComponent<InputField>().text;
        //content = GameObject.Find("TextField").GetComponent<InputField>().text;
        //GameObject.Find("TextField").GetComponent<InputField>().text = "";
    }
}
