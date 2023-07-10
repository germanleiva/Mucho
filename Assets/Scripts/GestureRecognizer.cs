using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GestureDetectEvent : UnityEvent<GameObject>
{
}

[Serializable]
public class Gesture
{
    public string gestureName;
    public List<Vector3> positionsPerFinger; // Relative to hand
    public GestureDetectEvent onRecognized;

    [HideInInspector]
    public float time = 0.0f;

    public Gesture(string name, List<Vector3> positions, GestureDetectEvent onRecognized)
    {
        gestureName = name;
        positionsPerFinger = positions;
        this.onRecognized = onRecognized;
        this.onRecognized.AddListener(GestureDetected);
    }

    public Gesture(string name, List<Vector3> positions)
    {
        gestureName = name;
        positionsPerFinger = positions;
        onRecognized = new GestureDetectEvent();
        this.onRecognized.AddListener(GestureDetected);
    }

    public void GestureDetected(GameObject hand)
    {
        DebugLogger.Instance.Log("Gesture detected: " + gestureName + " detected in " + hand.name);
        DebugLogger.Instance.LogInVR("Gesture detected: " + gestureName + " detected in " + hand.name);
    }
}

[DisallowMultipleComponent]
public class GestureRecognizer : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private List<Gesture> savedGestures = new List<Gesture>();
    [SerializeField] private float threshold = 0.05f;
    [SerializeField] private float delay = 0.2f;
    [SerializeField] private UnityEvent onNothingDetected = default;

    [Header("Objects")]
    [SerializeField] private GameObject hand = default;
    [SerializeField] private GameObject[] fingers = default;

    [Header("Debugging")]
    [SerializeField] private Gesture gestureDetected = default;

    private Gesture _previousGestureDetected = null;

    private void Start()
    {
        onNothingDetected.Invoke();
    }

    private void Update()
    {
        gestureDetected = Recognize();
        
        if (gestureDetected != _previousGestureDetected)
        {
            if (gestureDetected != null)
                gestureDetected.onRecognized.Invoke(gameObject);
            else
                onNothingDetected.Invoke();

            _previousGestureDetected = gestureDetected;
        }
    }

    public void SaveAsGesture()
    {
        //List<Vector3> positions = fingers.Select(t => hand.transform.InverseTransformPoint(t.transform.position)).ToList();
        List<Vector3> positions = fingers.Select(t => hand.transform.InverseTransformPoint(t.transform.position)).ToList();
        savedGestures.Add(new Gesture("Gesture No." + savedGestures.Count, positions));
        //savedGestures.Last().onRecognized.AddListener(() => DebugLogger.Instance.LogInVR("Gesture No." + savedGestures.Count + " recognized in " + gameObject.name));   
    }

    public List<Gesture> GetSavedGestures()
    {
        return savedGestures;
    }

    public void CopySavedGestures(List<Gesture> gestures)
    {
        savedGestures = new List<Gesture>(gestures);
    }

    private Gesture Recognize()
    {
        bool discardGesture = false;
        float minSumDistances = Mathf.Infinity;
        Gesture bestCandidate = null;

        // For each gesture
        for (int g = 0; g < savedGestures.Count; g++)
        {
            // If the number of fingers does not match, it returns an error
            if (fingers.Length != savedGestures[g].positionsPerFinger.Count)
                throw new Exception("Different number of tracked fingers");

            float sumDistances = 0f;

            // For each finger
            for (int f = 0; f < fingers.Length; f++)
            {
                Vector3 fingerRelativePos = hand.transform.InverseTransformPoint(fingers[f].transform.position);

                // If at least one finger does not enter the threshold we discard the gesture
                if (Vector3.Distance(fingerRelativePos, savedGestures[g].positionsPerFinger[f]) > threshold)
                {
                    discardGesture = true;
                    savedGestures[g].time = 0.0f;
                    break;
                }

                // If all the fingers entered, then we calculate the total of their distances
                sumDistances += Vector3.Distance(fingerRelativePos, savedGestures[g].positionsPerFinger[f]);
            }

            // If we have to discard the gesture, we skip it
            if (discardGesture)
            {
                discardGesture = false;
                continue;
            }

            // If it is valid and the sum of its distances is less than the existing record, it is replaced because it is a better candidate 
            if (sumDistances < minSumDistances)
            {
                if (bestCandidate != null)
                    bestCandidate.time = 0.0f;

                minSumDistances = sumDistances;
                bestCandidate = savedGestures[g];
            }
        }

        if (bestCandidate != null)
        {
            bestCandidate.time += Time.deltaTime;

            if (bestCandidate.time < delay)
                bestCandidate = null;
        }

        // If we've found something, we'll return it
        // If we haven't found anything, we return it anyway (newly created object)
        return bestCandidate;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GestureRecognizer))]
public class CustomInspectorGestureRecognizer : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GestureRecognizer gestureRecognizer = (GestureRecognizer)target;
        if (!GUILayout.Button("Save current gesture")) return;
        gestureRecognizer.SaveAsGesture();
    }
}
#endif