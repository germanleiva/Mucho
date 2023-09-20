using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};

    public Recordable leftHand, rightHand;

    public GameObject testBall, testTarget, floor, testHitMessage, testMissMessage;

    public GameObject collidingObject1, collidingObject2;

    public GameObject leftHandPinchObj, rightHandPinchObj;

    //public 

    public Recordable assetInContactWithLeftHand, assetInContactWithRightHand;

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
        CreateTestStates();
    }

    // Update is called once per frame
    void Update()
    {
        ProcessEvents();        
    }

    void CreateTestStates()
    {
        CustomStateMachine sm = CustomStateMachine.Instance;

        State idleState = new State();
        idleState.OnEnterActions = () => { DebugLogger.Instance.Log("Idle OnEnter"); };
        //state1.OnEnterActions += () => { Debug.Log("State 1 OnEnter 2"); };
        idleState.OnUpdateActions = () => { DebugLogger.Instance.Log("Idle OnUpdate"); };
        idleState.OnExitActions = () => { DebugLogger.Instance.Log("Idle OnExit"); };
        
        State grabState = new State();
        grabState.OnEnterActions = () => { DebugLogger.Instance.Log("Grab OnEnter"); testBall.GetComponent<Recordable>().Follow(rightHand.transform); };
        grabState.OnUpdateActions = () => { DebugLogger.Instance.Log("Grab OnUpdate"); };
        grabState.OnExitActions = () => { DebugLogger.Instance.Log("State 2 OnExit");  testBall.GetComponent<Recordable>().Unfollow(); };

        State throwState = new State();
        throwState.OnEnterActions = () => { DebugLogger.Instance.Log("Throw OnEnter"); testBall.GetComponent<Recordable>().ApplyForce(rightHand.transform.forward * 1); };
        throwState.OnUpdateActions = () => { DebugLogger.Instance.Log("Throw OnUpdate"); };
        throwState.OnExitActions = () => { DebugLogger.Instance.Log("Throw OnExit"); };

        State missState = new State();
        missState.OnEnterActions = () => { DebugLogger.Instance.Log("Miss OnEnter"); testHitMessage.SetActive(false); testMissMessage.SetActive(true); };
        missState.OnUpdateActions = () => { DebugLogger.Instance.Log("Miss OnUpdate"); };
        missState.OnExitActions = () => { DebugLogger.Instance.Log("Miss OnExit"); };


        State hitState = new State();
        hitState.OnEnterActions = () => { DebugLogger.Instance.Log("Hit OnEnter"); testHitMessage.SetActive(true); testMissMessage.SetActive(false); };
        hitState.OnUpdateActions = () => { DebugLogger.Instance.Log("Hit OnUpdate"); };
        hitState.OnExitActions = () => { DebugLogger.Instance.Log("Hit OnExit"); };

        idleState.AddTransitionTo(grabState, (frame) => { return frame.rightHandGesture == Gesture.RIGHTHANDPINCH && frame.isColliding(testBall, rightHandPinchObj); });
        grabState.AddTransitionTo(throwState, (frame) => { return frame.rightHandGesture == Gesture.RIGHTHANDTHROW; });
        throwState.AddTransitionTo(hitState, (frame) => { return frame.isColliding(testBall, testTarget); } );
        throwState.AddTransitionTo(missState, (frame) => { return frame.isColliding(testBall, floor); });

        sm.AddState("Idle", idleState);
        sm.AddState("Grab", grabState);
        sm.AddState("Throw", throwState);
        sm.AddState("Hit", hitState);
        sm.AddState("Miss", missState);
        sm.SetInitialState("Idle");
    }

    public void SetLeftHandGesture(string gestureStr)
    {
        leftHand.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);
        leftHand.SetGestureText(GestureToString(leftHand.currentGesture));
    }

    public void SetRightHandGesture(string gestureStr)
    {
        rightHand.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);
        rightHand.SetGestureText(GestureToString(rightHand.currentGesture));
    }

    public void NotifyCollision(GameObject object1, GameObject object2)
    {
        DebugLogger.Instance.Log("Collision between " + object1.name + " and " + object2.name);
        collidingObject1 = object1;
        collidingObject2 = object2;
    }

    void ProcessEvents()
    {
        CustomStateMachine sm = CustomStateMachine.Instance;

        Frame frame = new Frame
        {
            leftHandGesture = leftHand.currentGesture,
            rightHandGesture = rightHand.currentGesture,
            collidingObject1 = collidingObject1,
            collidingObject2 = collidingObject2
        };
        

        if (Manager.Instance.currAppState == Manager.AppState.LIVE)
        {
            //DebugLogger.Instance.Log("Passing events to state machine");
            sm.ProcessFrame(frame);
            collidingObject1 = null;
            collidingObject2 = null;
        }
        else if (Manager.Instance.currAppState == Manager.AppState.INIT || Manager.Instance.currAppState == Manager.AppState.PLAYBACK || Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING)
        {
            //DebugLogger.Instance.Log("Processing gestures in init/playback mode");
            //DebugLogger.Instance.Log("Left Hand: " + leftHand.currentGesture.ToString() + " Right Hand: " + rightHand.currentGesture.ToString());

            //DebugLogger.Instance.Log("Processing collisions between hands and assets");

            if (assetInContactWithLeftHand != null)
            {
                //DebugLogger.Instance.Log("Left Hand in contact with " + assetInContactWithLeftHand.name);
                if (leftHand.currentGesture == Gesture.LEFTHANDPINCH)
                {
                    //DebugLogger.Instance.Log("Left Hand is pinching " + assetInContactWithLeftHand.name);
                    //AttachAssetToHand(assetInContactWithLeftHand, leftHand);
                    assetInContactWithLeftHand.Follow(leftHand.transform);
                }
                else if (leftHand.currentGesture == Gesture.LEFTHANDNONE)
                {
                    //DebugLogger.Instance.Log("Left Hand is no longer pinching " + assetInContactWithLeftHand.name);
                    assetInContactWithLeftHand.Unfollow();
                }               
            }
            else
            {
                //assetInContactWithLeftHand.Unfollow();
            }


            if (assetInContactWithRightHand != null)
            {
                //DebugLogger.Instance.Log("Right Hand in contact with " + assetInContactWithRightHand.name);
                if (rightHand.currentGesture == Gesture.RIGHTHANDPINCH)
                {
                    //DebugLogger.Instance.Log("Right Hand is pinching " + assetInContactWithRightHand.name);
                    assetInContactWithRightHand.Follow(rightHand.transform);
                }    
                else if (rightHand.currentGesture == Gesture.RIGHTHANDNONE)
                {
                    //DebugLogger.Instance.Log("Right Hand is no longer pinching " + assetInContactWithRightHand.name);
                    assetInContactWithRightHand.Unfollow();
                }
            }
            else
            {
                //assetInContactWithRightHand.Unfollow();
            }

            //DebugLogger.Instance.Log("Processing collisions between assets");
            if (collidingObject1 != null && collidingObject2 != null)
            {
                //DebugLogger.Instance.Log("Collision between " + collidingObject1.name + " and " + collidingObject2.name);
                //collidingObject1.GetComponent<Recordable>().ProcessCollision(collidingObject2.GetComponent<Recordable>());
                //collidingObject2.GetComponent<Recordable>().ProcessCollision(collidingObject1.GetComponent<Recordable>());
            }

        } 
    }

    public void SetAssetInContactWithLeftHand(Recordable asset)
    {
        if (assetInContactWithLeftHand != asset) //assign only if it is a different asset
        {
            assetInContactWithLeftHand = asset;
        }
    }

    public void SetAssetInContactWithRightHand(Recordable asset)
    {
        if (assetInContactWithRightHand != asset) //assign only if it is a different asset
        {
            assetInContactWithRightHand = asset;
        }
    }

    public string GestureToString(Gesture gesture)
    {
        return gesture switch
        {
            Gesture.LEFTHANDNONE => ("None"),
            Gesture.LEFTHANDMENUOPEN => ("Menu Open"),
            Gesture.LEFTHANDGRAB => ("Closed"),
            Gesture.LEFTHANDPINCH => ("Pinch"),
            Gesture.LEFTHANDTHROW => ("Open"),
            Gesture.RIGHTHANDGRAB => ("Closed"),
            Gesture.RIGHTHANDNONE => ("None"),
            Gesture.RIGHTHANDTHROW => ("Open"),
            Gesture.RIGHTHANDPINCH => ("Pinch"),
            _ => ("ERROR"),
        };
    }

}

public class GestureSequence
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public InputManager.Gesture GestureType { get; set; }
}


