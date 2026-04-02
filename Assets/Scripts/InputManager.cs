using System;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Serializable]
    public enum Gesture { 
        LEFTHANDNONE, 
        LEFTHANDMENUOPEN, 
        LEFTHANDGRAB, 
        LEFTHANDPINCH, 
        LEFTHANDOPEN, 
        RIGHTHANDNONE, 
        RIGHTHANDGRAB, 
        RIGHTHANDOPEN, 
        RIGHTHANDPINCH
        };

    [Header("Hands")]
    public Hand leftHand, rightHand;

    [Header("Focus Targets")]
    public GameObject leftFocus, rightFocus, gazeFocus;

    [Header("Test Objects")]
    public GameObject testBall, testTarget, floor, testHitMessage, testMissMessage;

    [Header("Collision (last notified)")]
    //public GameObject collidingObjectNotified_1, collidingObjectNotified_2;

    public Dictionary<GameObject, HashSet<GameObject>> currentCollisions = new Dictionary<GameObject, HashSet<GameObject>>();

    [Header("Voice Command")]
    public string currentVoiceCommand;

    [Header("Live Contact Objects")]
    public GameObject leftHandPinchObj; // set { }
    public GameObject rightHandPinchObj;
    public GameObject headContactObj;

    [Header("Playback Contact Objects")]
    public GameObject playbackLeftHandPinchObj, playbackRightHandPinchObj, playbackHeadContactObj, playbackLeftFocus, playbackRightFocus, playbackGazeFocus;

    [Header("Velocity")]
    public Vector3 leftHandVelocity, rightHandVelocity, headVelocity;
    public float velocityScalingFactor = 10;

    public Asset assetInContactWithLeftHand, assetInContactWithRightHand; //TODO J - Seems ununsed, remove?

    private Vector3 lastLeftHandPos, lastRightHandPos, headLastPos;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        currentVoiceCommand = "";
        //CreateTestStates();
    }

    int frameCount = 0;

    // Update is called once per frame
    
    Queue<Vector3> rightHandPositions = new Queue<Vector3>();
    Queue<Vector3> leftHandPositions = new Queue<Vector3>();

    void Update()
    {
        if (Manager.Instance.currAppState == Manager.AppState.LIVE)
        {
            ProcessEvents();
        }
        // Add the current positions to the queues
        rightHandPositions.Enqueue(rightHandPinchObj.transform.position);
        leftHandPositions.Enqueue(leftHandPinchObj.transform.position);

        // If there are more than ten positions in the queue, remove the oldest
        if (rightHandPositions.Count > 10)
        {
            rightHandPositions.Dequeue();
        }
        if (leftHandPositions.Count > 10)
        {
            leftHandPositions.Dequeue();
        }

        // Calculate velocities
        if (rightHandPositions.Count == 10)
        {
            Vector3 currRightHandPos = rightHandPinchObj.transform.position;
            Vector3 tenFramesAgoRightHandPos = rightHandPositions.Peek();
            rightHandVelocity = ((currRightHandPos - tenFramesAgoRightHandPos) / (10 * Time.deltaTime)) / velocityScalingFactor;
        }
        if (leftHandPositions.Count == 10)
        {
            Vector3 currLeftHandPos = leftHandPinchObj.transform.position;
            Vector3 tenFramesAgoLeftHandPos = leftHandPositions.Peek();
            leftHandVelocity = ((currLeftHandPos - tenFramesAgoLeftHandPos) / (10 * Time.deltaTime)) / velocityScalingFactor;
        }

        
    }
    //TODO J - Do we want this?
    public void CreateTestStates()
    {
        Manager.Instance.currAppState = Manager.AppState.LIVE;
        StateMachineModel sm = StateMachineModel.Instance;
        sm.states.Clear();

        State idleState = new("Idle", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Idle OnEnter"); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Idle OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Idle OnExit"); }
        };

        State grabState = new("Grab", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Grab OnEnter"); testBall.GetComponent<Asset>().ApplyFollow(rightHand.transform); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Grab OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("State 2 OnExit"); testBall.GetComponent<Asset>().ApplyUnfollow(); }
        };

        State throwState = new("Throw", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Throw OnEnter"); testBall.GetComponent<Asset>().ApplyForce(rightHand.transform.forward * 1); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Throw OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Throw OnExit"); }
        };

        /*State missState = new()
        {
            OnEnterActions = () => { DebugLogger.Instance.Log("Miss OnEnter"); testHitMessage.SetActive(false); testMissMessage.SetActive(true); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Miss OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Miss OnExit"); }
        };


        State hitState = new()
        {
            OnEnterActions = () => { DebugLogger.Instance.Log("Hit OnEnter"); testHitMessage.SetActive(true); testMissMessage.SetActive(false); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Hit OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Hit OnExit"); }
        };*/

        idleState.AddTransitionTo(grabState, (frame) => frame.rightHandGesture == Gesture.RIGHTHANDPINCH && frame.IsColliding(testBall, rightHandPinchObj)); // TODO J - What is this testBall?
        grabState.AddTransitionTo(throwState, (frame) => frame.rightHandGesture == Gesture.RIGHTHANDOPEN);
        //throwState.AddTransitionTo(hitState, (frame) => { return frame.IsColliding(testBall, testTarget); } );
        //throwState.AddTransitionTo(missState, (frame) => { return frame.IsColliding(testBall, floor); });

        sm.AddState(idleState);
        sm.AddState(grabState);
        sm.AddState(throwState);
        //sm.AddState("Hit", hitState);
        //sm.AddState("Miss", missState);

        sm.SetInitialState(idleState);

        //Print contents of state machine
        DebugLogger.Instance.Log("Test State Machine Contents:", true);
        idleState.PrintDetailsOfState(true);
        grabState.PrintDetailsOfState(true);
        throwState.PrintDetailsOfState(true);
        //hitState.PrintDetailsOfState(true);
        //missState.PrintDetailsOfState(true);
    }

    public void SetLeftHandGesture(GestureBehaviour obj) {
        SetLeftHandGesture(obj.gestureType);
    }

    public void SetLeftHandGesture(Gesture gestureType)
    {
        leftHand.currentGesture = gestureType;
        leftHand.SetGestureText(GestureToString(leftHand.currentGesture));
    }

    public void SetRightHandGesture(GestureBehaviour obj) {
        SetRightHandGesture(obj.gestureType);
    }

    public void SetRightHandGesture(Gesture gestureType)
    {
        rightHand.currentGesture = gestureType;
        rightHand.SetGestureText(GestureToString(rightHand.currentGesture));
    }

    public void NotifyVoiceCommand(string command)
    {
        DebugLogger.Instance.Log("Voice Command: " + command);
        currentVoiceCommand = command;
    }

    public void SaveCurrentCollision(GameObject object1, GameObject object2)
    {
        if (object1 != null && object2 != null)
        {
            DebugLogger.Instance.Log("Collision between " + object1.name + " and " + object2.name);
        }

        if (!currentCollisions.TryGetValue(object1, out var listOfCollidingObjects))
        {
            listOfCollidingObjects = new HashSet<GameObject>();
            currentCollisions[object1] = listOfCollidingObjects;
        }
        
        listOfCollidingObjects.Add(object2);
    }

    public void UnsaveCurrentCollision(GameObject object1, GameObject object2)
    {
        if (currentCollisions.TryGetValue(object1, out var potentialCollisions1))
        {
            potentialCollisions1.Remove(object2);
        }
        
        if (currentCollisions.TryGetValue(object2, out var potentialCollisions2))
        {
            potentialCollisions2.Remove(object1);
        }
    }

    public void ResetCurrentCollisions()
    {
        currentCollisions.Clear();
    }


    //TODO J - Unused?
    // bool rightHandHoldingObject = false;
    // bool leftHandHoldingObject = false;

    void ProcessEvents()
    {
        //TODO : Processframe only if a change in gesture or collision has occured

        Frame frame = new()
        {
            leftHandGesture = leftHand.currentGesture,
            rightHandGesture = rightHand.currentGesture,
            collisions = currentCollisions,
            voiceCommand = currentVoiceCommand
        };
        
        //TODO J - Each frame?
        CustomStateMachine.Instance.ProcessFrame(frame);

    }

    public void SetPlaybackObjectsActive(bool show)
    {
        playbackLeftHandPinchObj.SetActive(show);
        playbackRightHandPinchObj.SetActive(show);
        playbackHeadContactObj.SetActive(show);
        
        leftHandPinchObj.SetActive(!show);
        rightHandPinchObj.SetActive(!show);
        headContactObj.SetActive(!show);
    }


    public string GestureToString(Gesture gesture)
    {
        return gesture switch
        {
            Gesture.LEFTHANDNONE => ("None"),
            Gesture.LEFTHANDMENUOPEN => ("Menu Open"),
            Gesture.LEFTHANDGRAB => ("Closed"),
            Gesture.LEFTHANDPINCH => ("Pinch"),
            Gesture.LEFTHANDOPEN => ("Open"),
            Gesture.RIGHTHANDGRAB => ("Closed"),
            Gesture.RIGHTHANDNONE => ("None"),
            Gesture.RIGHTHANDOPEN => ("Open"),
            Gesture.RIGHTHANDPINCH => ("Pinch"),
            _ => ("ERROR"),
        };
    }

}

/*
using System;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Serializable]
    public enum Gesture { 
        LEFTHANDNONE, 
        LEFTHANDMENUOPEN, 
        LEFTHANDGRAB, 
        LEFTHANDPINCH, 
        LEFTHANDOPEN, 
        RIGHTHANDNONE, 
        RIGHTHANDGRAB, 
        RIGHTHANDOPEN, 
        RIGHTHANDPINCH
        };

    public Hand leftHand, rightHand;

    public GameObject leftFocus, rightFocus, gazeFocus;

    public GameObject testBall, testTarget, floor, testHitMessage, testMissMessage;

    public GameObject collidingObjectNotified_1, collidingObjectNotified_2;

    public string currentVoiceCommand;

    public GameObject leftHandPinchObj;
    public GameObject rightHandPinchObj;
    public GameObject headContactObj;

    public GameObject playbackLeftHandPinchObj, playbackRightHandPinchObj, playbackHeadContactObj, playbackLeftFocus, playbackRightFocus, playbackGazeFocus;

    Vector3 lastLeftHandPos, lastRightHandPos, headLastPos;

    public Vector3 leftHandVelocity, rightHandVelocity, headVelocity;
    public float velocityScalingFactor = 10;

    public Asset assetInContactWithLeftHand, assetInContactWithRightHand;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        currentVoiceCommand = "";
        //CreateTestStates();
    }

    int frameCount = 0;

    // Update is called once per frame
    
    Queue<Vector3> rightHandPositions = new Queue<Vector3>();
    Queue<Vector3> leftHandPositions = new Queue<Vector3>();

    void Update()
    {
        if (Manager.Instance.currAppState == Manager.AppState.LIVE)
        {
            ProcessEvents();
        }
        // Add the current positions to the queues
        rightHandPositions.Enqueue(rightHandPinchObj.transform.position);
        leftHandPositions.Enqueue(leftHandPinchObj.transform.position);

        // If there are more than ten positions in the queue, remove the oldest
        if (rightHandPositions.Count > 10)
        {
            rightHandPositions.Dequeue();
        }
        if (leftHandPositions.Count > 10)
        {
            leftHandPositions.Dequeue();
        }

        // Calculate velocities
        if (rightHandPositions.Count == 10)
        {
            Vector3 currRightHandPos = rightHandPinchObj.transform.position;
            Vector3 tenFramesAgoRightHandPos = rightHandPositions.Peek();
            rightHandVelocity = ((currRightHandPos - tenFramesAgoRightHandPos) / (10 * Time.deltaTime)) / velocityScalingFactor;
        }
        if (leftHandPositions.Count == 10)
        {
            Vector3 currLeftHandPos = leftHandPinchObj.transform.position;
            Vector3 tenFramesAgoLeftHandPos = leftHandPositions.Peek();
            leftHandVelocity = ((currLeftHandPos - tenFramesAgoLeftHandPos) / (10 * Time.deltaTime)) / velocityScalingFactor;
        }

        
    }

    public void CreateTestStates()
    {
        Manager.Instance.currAppState = Manager.AppState.LIVE;
        StateMachineModel sm = StateMachineModel.Instance;
        sm.states.Clear();

        State idleState = new("Idle", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Idle OnEnter"); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Idle OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Idle OnExit"); }
        };

        State grabState = new("Grab", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Grab OnEnter"); testBall.GetComponent<Asset>().ApplyFollow(rightHand.transform); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Grab OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("State 2 OnExit"); testBall.GetComponent<Asset>().ApplyUnfollow(); }
        };

        State throwState = new("Throw", sm)
        {
            //OnEnterActions = () => { DebugLogger.Instance.Log("Throw OnEnter"); testBall.GetComponent<Asset>().ApplyForce(rightHand.transform.forward * 1); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Throw OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Throw OnExit"); }
        };

        /*State missState = new()
        {
            OnEnterActions = () => { DebugLogger.Instance.Log("Miss OnEnter"); testHitMessage.SetActive(false); testMissMessage.SetActive(true); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Miss OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Miss OnExit"); }
        };


        State hitState = new()
        {
            OnEnterActions = () => { DebugLogger.Instance.Log("Hit OnEnter"); testHitMessage.SetActive(true); testMissMessage.SetActive(false); },
            //OnUpdateActions = () => { DebugLogger.Instance.Log("Hit OnUpdate"); },
            //OnExitActions = () => { DebugLogger.Instance.Log("Hit OnExit"); }
        };* /

        idleState.AddTransitionTo(grabState, (frame) => frame.rightHandGesture == Gesture.RIGHTHANDPINCH && frame.IsColliding(testBall, rightHandPinchObj));
        grabState.AddTransitionTo(throwState, (frame) => frame.rightHandGesture == Gesture.RIGHTHANDOPEN);
        //throwState.AddTransitionTo(hitState, (frame) => { return frame.IsColliding(testBall, testTarget); } );
        //throwState.AddTransitionTo(missState, (frame) => { return frame.IsColliding(testBall, floor); });

        sm.AddState(idleState);
        sm.AddState(grabState);
        sm.AddState(throwState);
        //sm.AddState("Hit", hitState);
        //sm.AddState("Miss", missState);

        sm.SetInitialState(idleState);

        //Print contents of state machine
        DebugLogger.Instance.Log("Test State Machine Contents:", true);
        idleState.PrintDetailsOfState(true);
        grabState.PrintDetailsOfState(true);
        throwState.PrintDetailsOfState(true);
        //hitState.PrintDetailsOfState(true);
        //missState.PrintDetailsOfState(true);


    }

    public void SetLeftHandGesture(GestureBehaviour obj) {
        SetLeftHandGesture(obj.gestureType);
    }

    public void SetLeftHandGesture(Gesture gestureType)
    {
        leftHand.currentGesture = gestureType;
        leftHand.SetGestureText(GestureToString(leftHand.currentGesture));
    }

    public void SetRightHandGesture(GestureBehaviour obj) {
        SetRightHandGesture(obj.gestureType);
    }

    public void SetRightHandGesture(Gesture gestureType)
    {
        rightHand.currentGesture = gestureType;
        rightHand.SetGestureText(GestureToString(rightHand.currentGesture));
    }

    public void NotifyVoiceCommand(string command)
    {
        DebugLogger.Instance.Log("Voice Command: " + command);
        currentVoiceCommand = command;
    }

    public void SaveCurrentCollision(GameObject object1, GameObject object2)
    {
        if (object1 != null && object2 != null)
        {
            DebugLogger.Instance.Log("Collision between " + object1.name + " and " + object2.name);
        }

        collidingObjectNotified_1 = object1;
        collidingObjectNotified_2 = object2;
    }

    bool rightHandHoldingObject = false;
    bool leftHandHoldingObject = false;

    void ProcessEvents()
    {
        //TODO : Processframe only if a change in gesture or collision has occured

        Frame frame = new()
        {
            leftHandGesture = leftHand.currentGesture,
            rightHandGesture = rightHand.currentGesture,
            collidingObjectThisFrame_1 = collidingObjectNotified_1,
            collidingObjectThisFrame_2 = collidingObjectNotified_2,
            voiceCommand = currentVoiceCommand
        };
        

        CustomStateMachine.Instance.ProcessFrame(frame);

    }

    public void SetPlaybackObjectsActive(bool show)
    {
        playbackLeftHandPinchObj.SetActive(show);
        playbackRightHandPinchObj.SetActive(show);
        playbackHeadContactObj.SetActive(show);
        
        leftHandPinchObj.SetActive(!show);
        rightHandPinchObj.SetActive(!show);
        headContactObj.SetActive(!show);
    }


    public string GestureToString(Gesture gesture)
    {
        return gesture switch
        {
            Gesture.LEFTHANDNONE => ("None"),
            Gesture.LEFTHANDMENUOPEN => ("Menu Open"),
            Gesture.LEFTHANDGRAB => ("Closed"),
            Gesture.LEFTHANDPINCH => ("Pinch"),
            Gesture.LEFTHANDOPEN => ("Open"),
            Gesture.RIGHTHANDGRAB => ("Closed"),
            Gesture.RIGHTHANDNONE => ("None"),
            Gesture.RIGHTHANDOPEN => ("Open"),
            Gesture.RIGHTHANDPINCH => ("Pinch"),
            _ => ("ERROR"),
        };
    }

}
*/
