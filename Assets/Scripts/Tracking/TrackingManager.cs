using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TrackingType {
    Grab,
    Gaze,
    Point
}
public class TrackingManager : MonoBehaviour {
    private DialogueManager _dialogueManager;
    private List<GameObject> grabTracks = new();
    private List<GameObject> gazeTracks = new();
    private bool _isTracking = false;

    private void Awake() {
        _dialogueManager = FindObjectOfType<DialogueManager>();
    }

    public void StartTracking() {
        _isTracking = true;
    }

    public void StopTracking() {
        _isTracking = false;
    }
    
    public void AddTrackedObject(TrackingType type,GameObject trackedObject) {
        if(!_isTracking) return;

        switch (type) {
            case TrackingType.Gaze:
                if(!gazeTracks.Contains(trackedObject)) gazeTracks.Add(trackedObject);
                break;
            case TrackingType.Grab:
                if(!grabTracks.Contains(trackedObject)) grabTracks.Add(trackedObject);
                break;
        }
    }

    public string TrackingString() {
        if(grabTracks.Count == 0 && gazeTracks.Count == 0) return "";
        
        string grabStr = "";
        if (grabTracks.Count > 0) {
            grabStr = "User grabbed: ";
            foreach (var obj in grabTracks) {
                grabStr += $"{obj.name}, ";
            }
            grabStr = grabStr[..^2];
        }

        string gazeStr = "";
        if (gazeTracks.Count > 0) {
            gazeStr = "User gazed: ";
            foreach (var obj in gazeTracks) {
                gazeStr += $"{obj.name}, ";
            }
            gazeStr = gazeStr[..^2];
        }

        
        return $"{grabStr} {gazeStr} during last prompt.";
    }

    public List<GameObject> GetTrackedObjects(TrackingType type) {
        switch (type) {
            case TrackingType.Gaze:
                return gazeTracks;
            case TrackingType.Grab:
                return grabTracks;
        }
        return null;
    }

    public void ClearTracks() {
        grabTracks.Clear();
        gazeTracks.Clear();
    }
}
