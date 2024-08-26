using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum DialogueRole {
    USER,
    SYSTEM,
    ASSISTANT
}

public class DialogueManager : MonoBehaviour {
    private readonly Dictionary<DialogueRole, string> roleText = new Dictionary<DialogueRole, string> {
        { DialogueRole.USER, "user"},
        { DialogueRole.SYSTEM, "system"},
        { DialogueRole.ASSISTANT, "assistant"}
    };

    private TrackingManager _trackingManager;
    private AuthenticateManager _authenticateManager;

    [SerializeField] private TextMeshPro dialogueText;
    [SerializeField] private Interactable recordingButton;
    [SerializeField] private Interactable finishButton;
    [SerializeField] [TextArea(3,10)] private string startingPrompt = "";
    private string _prompt = "";
    private List<Message> _currentMessages = new();
    
    // GPT request parameters.
    private const string GptModel = "gpt-4";
    private const string GptEndpoint = "https://api.openai.com/v1/chat/completions";
    // can be serialized but it is read from a txt file in the demo. File path: Assets/StreamingAssets/GptKey.txt
    private string _gptApiKey = "YOUR_API_KEY";
    
    // backend request details.
    private const string BackendEnpoint = "http://localhost:8080/conversations/add";

    private void Start() {
        _gptApiKey = System.IO.File.ReadAllText(Application.streamingAssetsPath + "/GptKey.txt");
        _trackingManager = FindObjectOfType<TrackingManager>();
        _authenticateManager = FindObjectOfType<AuthenticateManager>();
        _prompt = MessageToJson(roleText[DialogueRole.SYSTEM], startingPrompt);
        _currentMessages.Add(new Message(roleText[DialogueRole.SYSTEM], startingPrompt, "Text"));
    }
    
    #region backend

    public void FinishConversation() {
        Message[] messages = _currentMessages.ToArray();
        
        Conversation conversation = new Conversation();
        conversation.name = "UnityConversation" + Random.Range(0,1000);
        conversation.model = "gpt-4";
        conversation.userId = _authenticateManager.GetUserId();
        conversation.messages = messages;
        
        StartCoroutine(SaveConversation(conversation));
        
        // cleanup for next iteration
        _currentMessages.Clear();
        _prompt = MessageToJson(roleText[DialogueRole.SYSTEM], startingPrompt);
        _currentMessages.Add(new Message(roleText[DialogueRole.SYSTEM], startingPrompt, "Text"));
        dialogueText.text = "";
    }

    public IEnumerator SaveConversation(Conversation conversation) {
        string requestData = JsonUtility.ToJson(conversation);
        
        // Create a UnityWebRequest object for a POST request.
        UnityWebRequest webRequest = new UnityWebRequest(BackendEnpoint, "POST");

        // Set the request body as a byte array.
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // Set the content type header to indicate that the request body is JSON.
        webRequest.SetRequestHeader("Content-Type", "application/json");
        // Set the Authorization header with the Bearer token.
        webRequest.SetRequestHeader("Authorization", "Bearer " + _authenticateManager.GetUserToken());
        
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + webRequest.error);
            yield break;
        }
        Debug.Log("conversation saved successfully.");
    }

    #endregion

    #region Gpt 
    
    // TODO: get list of trackables and add them manually.
    public void HandleUserPrompt(string prompt) {
        _prompt += "," + MessageToJson(roleText[DialogueRole.USER], prompt);
        
        var grabTracks = _trackingManager.GetTrackedObjects(TrackingType.Grab);
        var gazeTracks = _trackingManager.GetTrackedObjects(TrackingType.Gaze);
        if(grabTracks.Count > 0) _currentMessages.Add(new Message(roleText[DialogueRole.USER], string.Join(',', grabTracks), "Grab"));
        if(gazeTracks.Count > 0) _currentMessages.Add(new Message(roleText[DialogueRole.USER], string.Join(',', gazeTracks), "Gaze"));
        
        var trackStr = _trackingManager.TrackingString();
        _trackingManager.ClearTracks();
        if (trackStr.Length > 0) {
            _prompt += "," + MessageToJson(roleText[DialogueRole.SYSTEM], trackStr);
            _currentMessages.Add(new Message(roleText[DialogueRole.SYSTEM], trackStr, "Action"));

        }
        
        StartCoroutine(SendPrompt());
    }
    
    private string MessageToJson(string role, string content) {
        return $"{{\"role\": \"{role}\", \"content\": \"{content}\"}}";
    }

    private IEnumerator SendPrompt() {
        
        string requestData = $"{{\"messages\": [{_prompt}], \"model\": \"{GptModel}\"}}";
        // Create a UnityWebRequest object for a POST request.
        UnityWebRequest webRequest = new UnityWebRequest(GptEndpoint, "POST");

        // Set the request body as a byte array.
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // Set the content type header to indicate that the request body is JSON.
        webRequest.SetRequestHeader("Content-Type", "application/json");
        // Set the Authorization header with the Bearer token.
        webRequest.SetRequestHeader("Authorization", "Bearer " + _gptApiKey);
        
        yield return webRequest.SendWebRequest();
        
        // TODO: handle error gracefully.
        if (webRequest.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + webRequest.error);
            yield break;
        }
        
        string response = ExtractResponse(webRequest.downloadHandler.text);
        Debug.Log("gpt response: " + response);
        _prompt += "," + MessageToJson(roleText[DialogueRole.ASSISTANT], response);
        _currentMessages.Add(new Message(roleText[DialogueRole.ASSISTANT], response, "Text"));
        dialogueText.text = _prompt;
        recordingButton.gameObject.SetActive(true);
    }
    
    private string ExtractResponse(string text) {
        string response = text;
        Debug.Log("gpt raw response: " + response);
        // Print the response data.
        GptResponse res = JsonUtility.FromJson<GptResponse>(response);
        return res.choices[0].message.content; // just the content here
    }
    #endregion

}

#region helper classes

// chatgpt response types
[System.Serializable]
public class GptResponse
{
    public string id;
    public int created;
    public string model;
    public GptChoice[] choices;
}

[System.Serializable]
public class GptChoice
{
    public int index;
    public GptMessage message;
}

[System.Serializable]
public class GptMessage
{
    public string role;
    public string content;
}

// backend conversation data types

[Serializable]
public class Conversation {
    public string name;
    public string model;
    public string userId;
    public Message[] messages;
}

[Serializable]
public class Message {
    public string role;
    public string content;
    public string messageType;

    public Message(string role, string content, string messageType) {
        this.role = role;
        this.content = content;
        this.messageType = messageType;
    }
}


#endregion

