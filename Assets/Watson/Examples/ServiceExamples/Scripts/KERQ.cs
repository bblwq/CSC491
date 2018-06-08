using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.DataTypes;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;

using FullSerializer;

public class KERQ : MonoBehaviour
{
    // Conversation API paramaters
    private string C_username = "76175e2e-5b19-4046-bc1f-d40564593cdd";
    private string C_password = "wFna6m5bYMaZ";
    private string C_url = "https://gateway.watsonplatform.net/conversation/api";
    private string C_workspaceId = "c7fb1332-b6b5-4057-8b5d-2f7b0b792b34";

    private Conversation _conversation;
    private string _conversationVersionDate = "2017-05-26";

    private fsSerializer _serializer = new fsSerializer();
    private Dictionary<string, object> _context = null;
    private bool _waitingForAPI = true;

    // Speech_to_Text Parameters
    private string S2T_username = "b5f19ddc-3a6e-4052-aa40-120fb1823c96";
    private string S2T_password = "qjjdRwXpf21J";
    private string S2T_url = "https://stream-tls10.watsonplatform.net/speech-to-text/api";

    private int _recordingRoutine = 0;
    private string _microphoneID = null;
    private AudioClip _recording = null;
    private int _recordingBufferSize = 2;
    private int _recordingHZ = 22050;

    private SpeechToText _speechToText;

    // Text_to_Speech Parameters
    private string T2S_username = "b2af306b-a1ff-4ad5-a848-b64a2a1d4ba1";
    private string T2S_password = "2xylGQugVUsT";
    private string T2S_url = "https://stream.watsonplatform.net/text-to-speech/api";

    TextToSpeech _textToSpeech;
    //string _testString = "<speak version=\"1.0\"><prosody pitch=\"150Hz\">This is Text to Speech!</prosody><express-as type=\"GoodNews\">I'm sorry. This is Text to Speech!</express-as></speak>";

    private bool _synthesizeTested = false;

    IEnumerator Start()
    {
        GameObject.Find("Status").GetComponent<Text>().text = "[Initializing]";
        LogSystem.InstallDefaultReactors();

        //  Create credential and instantiate TextToSpeech service
        Credentials T2S_credentials = new Credentials(T2S_username, T2S_password, T2S_url);

        _textToSpeech = new TextToSpeech(T2S_credentials);

        //  Create credential and instantiate Conversational Service
        Credentials C_credentials = new Credentials(C_username, C_password, C_url);
        _conversation = new Conversation(C_credentials);
        _conversation.VersionDate = _conversationVersionDate;

        // Send the initial request to Watson
        _waitingForAPI = true;
        Runnable.Run(GenerateConversationCall("1"));
        while (_waitingForAPI)
            yield return null;

        Log.Debug("Conversation API", "Initialization Finished");

        //  Create credential and instantiate Speech_to_Text service
        Credentials S2T_credentials = new Credentials(S2T_username, S2T_password, S2T_url);
        _speechToText = new SpeechToText(S2T_credentials);
        Active = true;
        Log.Debug("Speech_to_Text API", "Initialization Finished");
    }

    private IEnumerator GenerateConversationCall(string content)
    {
        GameObject.Find("Status").GetComponent<Text>().text = "[Understanding]";

        _waitingForAPI = true;
        AskQuestion(content);
        while (_waitingForAPI)
            yield return null;

        Log.Debug("Conversation", "API call complete.");
    }

    private IEnumerator GenerateT2SCall(string text)
    {
        GameObject.Find("Status").GetComponent<Text>().text = "[Speaking]";

        //  Synthesize
        Log.Debug("TextToSpeech", "Attempting synthesize.");

        _textToSpeech.Voice = VoiceType.en_US_Allison;
        _textToSpeech.ToSpeech(text, HandleToSpeechCallback, true);
        while (!_synthesizeTested)
            yield return null;

        Log.Debug("TextToSpeech", "API call complete.");
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

        if (!_conversation.Message(OnMessage, C_workspaceId, messageRequest))
            Log.Debug("Conversation", "Failed to message!");
    }

    private void OnMessage(object resp, string data)
    {
        Log.Debug("Conversation", "Message Response: {0}", data);

        string T2S_content = "";

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
            Log.Debug("Conversation", "Failed to get context");

        //  Retrieve the output text
        object _tempOutput = null;
        object _responseObj = null;
        (resp as Dictionary<string, object>).TryGetValue("output", out _tempOutput);
        if (_tempOutput != null)
        {
            Log.Debug("Conversation", "Response: " + _tempOutput.ToString());

            (_tempOutput as Dictionary<string, object>).TryGetValue("text", out _responseObj);
            List<object> _responseList = null;
            if (_responseObj != null)
            {
                _responseList = _responseObj as List<object>;
                if (_responseList.Count != 0)
                {
                    GameObject.Find("Response").GetComponent<Text>().text = _responseList[0].ToString();
                    T2S_content = _responseList[0].ToString();
                }
                else
                {
                    Log.Debug("Conversation", "Unrecognized intent");
                    GameObject.Find("Response").GetComponent<Text>().text = "Sorry, I could not understand that. Please say help marry to see what you can respond.";
                    T2S_content = "Sorry, I could not understand that. Please say help marry to see what you can respond.";
                }
            }
            else
            {
                Log.Debug("Conversation", "No Response returned from the API.");
                GameObject.Find("Response").GetComponent<Text>().text = "Sorry, we cannot respond for now. Please try again.";
                T2S_content = "Sorry, we cannot respond for now. Please try again.";
            }
        }
        else
        {
            Log.Debug("Conversation", "Failed to get output");
            GameObject.Find("Response").GetComponent<Text>().text = "Sorry, we cannot respond for now. Please try again.";
            T2S_content = "Sorry, we cannot respond for now. Please try again.";
        }

        T2S_content = "<speak version=\"1.0\"><prosody pitch=\"150Hz\">" + T2S_content + "</prosody></speak>";
        //T2S_content = "<speak version=\"1.0\"><express-as type=\"GoodNews\">" + T2S_content + "</express-as></speak>";
        Runnable.Run(GenerateT2SCall(T2S_content));

        _waitingForAPI = false;
    }

    public bool Active
    {
        get { return _speechToText.IsListening; }
        set
        {
            if (value && !_speechToText.IsListening)
            {
                _speechToText.DetectSilence = true;
                _speechToText.EnableWordConfidence = false;
                _speechToText.EnableTimestamps = false;
                _speechToText.SilenceThreshold = 0.03f;
                _speechToText.MaxAlternatives = 1;
                _speechToText.EnableContinousRecognition = true;
                _speechToText.EnableInterimResults = true;
                _speechToText.OnError = OnError;
                _speechToText.StartListening(OnRecognize);
                List<string> keywords = new List<string>();
                keywords.Add("hello");
                _speechToText.KeywordsThreshold = 0.5f;
                _speechToText.Keywords = keywords.ToArray();
            }
            else if (!value && _speechToText.IsListening)
            {
                _speechToText.StopListening();
            }
        }
    }

    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }

    private void OnError(string error)
    {
        Active = false;
        Log.Debug("SpeechStreaming", "Error! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        Log.Debug("SpeechStreaming", "devices: {0}", Microphone.devices);
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let _recordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("MicrophoneWidget", "Microphone disconnected.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
                record.MaxLevel = Mathf.Max(samples);
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                _speechToText.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }
        }
        yield break;
    }

    private void OnRecognize(SpeechRecognitionEvent result)
    {
        if (result != null && result.results.Length > 0)
        {
            if (result.results[0].final)
            {
                Log.Debug("Result", result.results[0].alternatives[0].transcript);
                GameObject.Find("Response").GetComponent<Text>().text = result.results[0].alternatives[0].transcript;
                Runnable.Run(GenerateConversationCall(result.results[0].alternatives[0].transcript));
            }
        }
    }

    void HandleToSpeechCallback(AudioClip clip, string customData)
    {
        StartCoroutine(PlayClip(clip));
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (Application.isPlaying && clip != null)
        {
            GameObject audioObject = new GameObject("AudioObject");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            source.loop = false;
            source.clip = clip;
 
            // Stop Listening when Marry is speaking.
            StopRecording();
 
            // Play the response audio from Marry.
            source.Play();
            yield return new WaitWhile(() => source.isPlaying);

            // Start listening again after Marry is done.
            StartRecording();
            GameObject.Find("Status").GetComponent<Text>().text = "[Listening]";

            Destroy(audioObject, clip.length);
            _synthesizeTested = true;
        }
    }
}
