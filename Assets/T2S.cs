/**
* Copyright 2015 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using UnityEngine;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Utilities;
using System.Collections;

public class T2S : MonoBehaviour
{
    private string _username = "7cf2570a-dc7e-449b-8aca-9b2c6e61e42e";
    private string _password = "w4nnmnNgFpa6";
    private string _url = "https://stream.watsonplatform.net/text-to-speech/api";

    TextToSpeech _textToSpeech;
    string _testString = "<speak version=\"1.0\"><prosody pitch=\"150Hz\">This is Text to Speech!</prosody><express-as type=\"GoodNews\">I'm sorry. This is Text to Speech!</express-as></speak>";

    private bool _synthesizeTested = false;

    void Start()
    {
        LogSystem.InstallDefaultReactors();

        //  Create credential and instantiate service
        Credentials credentials = new Credentials(_username, _password, _url);

        _textToSpeech = new TextToSpeech(credentials);

        Runnable.Run(Examples());
    }

    private IEnumerator Examples()
    {
        //  Synthesize
        Log.Debug("ExampleTextToSpeech", "Attempting synthesize.");

        _textToSpeech.Voice = VoiceType.en_US_Allison;
        _textToSpeech.ToSpeech(_testString, HandleToSpeechCallback, true);
        while (!_synthesizeTested)
            yield return null;

        Log.Debug("ExampleTextToSpeech", "Text to Speech examples complete.");
    }

    void HandleToSpeechCallback(AudioClip clip, string customData)
    {
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (Application.isPlaying && clip != null)
        {
            GameObject audioObject = new GameObject("AudioObject");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            source.loop = false;
            source.clip = clip;
            source.Play();

            Destroy(audioObject, clip.length);

            _synthesizeTested = true;
        }
    }
}
