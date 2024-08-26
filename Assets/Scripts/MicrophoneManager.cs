using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

public class MicrophoneManager : MonoBehaviour
{
    private AudioSource _audioSource;
    public WhisperManager whisper;
    private MicrophoneRecord _microphoneRecord;
    public bool streamSegments = true;

    [FormerlySerializedAs("button")] [Header("UI")] 
    public Interactable recordButton;
    public TextMeshPro buttonText;
    public TextMeshPro outputText;
    public Interactable submitButton;
    
    private string _buffer;

    private TrackingManager _trackingManager;
    private DialogueManager _dialogueManager;
    
    private void Awake() {
        _audioSource = GetComponent<AudioSource>();
        _microphoneRecord = GetComponent<MicrophoneRecord>();
        whisper.OnNewSegment += OnNewSegment;
        _microphoneRecord.OnRecordStop += OnRecordStop;
        recordButton.OnClick.AddListener(OnButtonPressed);
        submitButton.OnClick.AddListener(SubmitVoice);
        _microphoneRecord.echo = false;
        
        _trackingManager = FindObjectOfType<TrackingManager>();
        _dialogueManager = FindObjectOfType<DialogueManager>();
    }
    
    private void OnButtonPressed() {
        if (!_microphoneRecord.IsRecording) {
            _microphoneRecord.StartRecord();
            _trackingManager.StartTracking();
            buttonText.text = "Stop";
        }
        else {
            _microphoneRecord.StopRecord();
            _trackingManager.StopTracking();
            buttonText.text = "Record";
        }
    }

    private async void OnRecordStop(AudioChunk recordedAudio) {
        buttonText.text = "Record";
        _buffer = "";

        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (res == null || !outputText)
            return;

        var text = res.Result;

        outputText.text = text;
        if (text == "" || text == " " || text == null) {
            TellAboutMissingAudio();
        }
        else {
            submitButton.gameObject.SetActive(true);
        }
    }
    
    private void TellAboutMissingAudio() {
        Debug.LogError("empty recording!");
    }

    private void OnNewSegment(WhisperSegment segment) {
        if (!streamSegments || !outputText)
            return;

        _buffer += segment.Text;
        outputText.text = _buffer + "...";
    }
    
    public void SubmitVoice() {
        _dialogueManager.HandleUserPrompt(outputText.text);
        outputText.text = "";
        submitButton.gameObject.SetActive(false);
        recordButton.gameObject.SetActive(false);
    }
    
}
