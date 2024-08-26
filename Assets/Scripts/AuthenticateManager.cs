using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AuthenticateManager : MonoBehaviour {
    [SerializeField] private string username = "test";
    [SerializeField] private string password = "test";
    
    private string _userId = "";
    private string _userToken = "";
    
    private string _authenticateEndpoint = "http://localhost:8080/auth/signin";

    // authenticate at the beginning
    private void Start() {
        StartCoroutine(AuthenticateUser());
    }

    public string GetUserId() {
        return _userId;
    }

    public string GetUserToken() {
        return _userToken;
    }

    public IEnumerator AuthenticateUser() {
        string requestData = AuthenticateBody(username,password);
        // Create a UnityWebRequest object for a POST request.
        UnityWebRequest webRequest = new UnityWebRequest(_authenticateEndpoint, "POST");

        // Set the request body as a byte array.
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // Set the content type header to indicate that the request body is JSON.
        webRequest.SetRequestHeader("Content-Type", "application/json");
        
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + webRequest.error);
            yield break;
        }
        
        AuthenticateResponse response = JsonUtility.FromJson<AuthenticateResponse>(webRequest.downloadHandler.text);
        _userId = response.userId;
        _userToken = response.accessToken;
    }

    public string AuthenticateBody(string username, string password) {
        string jsonBody = $"{{\"username\": \"{username}\", \"password\": \"{password}\"}}";
        Debug.Log(jsonBody);
        return jsonBody;
    }
}

[Serializable]
public class AuthenticateResponse {
    public string accessToken;
    public string userId;
}