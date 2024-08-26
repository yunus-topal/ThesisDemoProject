using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class Trackable : MonoBehaviour, IMixedRealityFocusHandler{
    
    [SerializeField] private TrackingType trackingType;
    [SerializeField] private float trackingThreshold = 1.0f;

    private TrackingManager _trackingManager;

    private float trackingStarted = float.MaxValue;
    

    private void Awake() {
        _trackingManager = FindObjectOfType<TrackingManager>();
        var _manipulator = GetComponent<ObjectManipulator>();
        if (_manipulator != null) {
            _manipulator.OnManipulationStarted.AddListener(OnGrabStarted);
            _manipulator.OnManipulationEnded.AddListener(OnGrabEnded);
        }
    }
    // TODO: count time in update loop in case user finishes recording without exiting interaction.
    
    // Handle gaze tracking.
    public void OnFocusEnter(FocusEventData eventData) {
        Debug.Log("Gaze Entered.");
        if (trackingType != TrackingType.Gaze) return;

        trackingStarted = Time.time;
    }

    public void OnFocusExit(FocusEventData eventData) {
        Debug.Log("Gaze Exited.");
        if (trackingType != TrackingType.Gaze) return;

        float trackingEnded = Time.time;
        if (trackingEnded - trackingStarted > trackingThreshold) {
            _trackingManager.AddTrackedObject(TrackingType.Gaze,gameObject);
        }
    }
    
    // handle grab tracking.
    void OnGrabStarted(ManipulationEventData eventData) {
        Debug.Log("Grab Started.");
        if (trackingType != TrackingType.Grab) return;

        trackingStarted = Time.time;
    }

    void OnGrabEnded(ManipulationEventData eventData) {
        Debug.Log("Grab Ended.");
        if (trackingType != TrackingType.Grab) return;

        float trackingEnded = Time.time;
        if (trackingEnded - trackingStarted > trackingThreshold) {
            _trackingManager.AddTrackedObject(TrackingType.Grab,gameObject);
        }
    }
    
}
