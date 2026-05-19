using System;
using System.Collections.Generic;
using System.Linq;
// using Assets.OVR.Scripts;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

//[RequireComponent(typeof(LineRenderer))]
public class Asset : MonoBehaviour
{
    GameObject collidedObjectDuringRecording; // TODO J - What is this?

    public event Action<Asset> OnMeshUpdated;

    // -------------------------
    // Cached components
    // -------------------------
    private Grabbable _grabbable;
    private MeshRenderer _meshRenderer;
    private Rigidbody _rigidbody;
    private Light _light;
    private ParticleSystem _dust;

    // -------------------------
    // Public state
    // -------------------------

    public Quaternion InitialRotation { get; set; }
    public Vector3 InitialPosition { get; set; }

    [NonSerialized] public List<ForceArrow> forceArrows = new();

    [SerializeField] private Collider grabCollider;
    [SerializeField] public GameObject assetMenu;
    [SerializeField] private GameObject followLineObj;
    [SerializeField] private GameObject colliderVisualizerObj, colliderBoundaryGizmoObj1;

    [NonSerialized] public Material defaultMaterial;

    private int firstFrameOfManualRecording, lastFrameOfManualRecording;


    // -------------------------
    // Visual properties
    // -------------------------
    public Color CurrentColor
    {
        // get => gameObject.GetComponent<MeshRenderer>().material.color;
        // set => gameObject.GetComponent<MeshRenderer>().material.color = value;
        get => _meshRenderer.material.color;
        set => _meshRenderer.material.color = value;
    }

    public bool IsVisible
    {
        get
        {
            // MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

            // Any time we access material or materials on a renderer, that actually creates a copy
            // of the materials assigned just to that renderer. So we cannot use == operator
            String baseMaterialName = defaultMaterial.name;
            String assignedMaterialName = _meshRenderer.sharedMaterial.name;

            return assignedMaterialName.Contains(baseMaterialName);
        }
        set
        {
            if (_meshRenderer == null) return;
            bool isLive = Manager.Instance != null && Manager.Instance.currAppState == Manager.AppState.LIVE;
            //DebugLogger.Instance.Log("SetVisibility: true for " + gameObject.name + " at index " + (int)Recorder.Instance.playbackSlider.value);
            if (value)
            {
                if (isLive)
                {
                    DebugLogger.Instance.Log($"LIVE mode: Setting visibility TRUE for {gameObject.name}");
                    //Set mesh renderer for playbackObject 
                    _meshRenderer.enabled = true;
                    if (defaultMaterial != null) _meshRenderer.material = defaultMaterial;
                    //If gameobject has TextAsset component then call ChangeTextPanelBackground(Material mat)
                    if (gameObject.GetComponentInChildren<TextAsset>() != null)
                    {
                        gameObject.GetComponent<TextAsset>().SetTextVisibility(true);
                    }
                }
                else
                {
                    if (defaultMaterial != null)
                    {
                        Color color = _meshRenderer.material.color;
                        _meshRenderer.material = defaultMaterial;
                        _meshRenderer.material.color = color;
                    }
                }
            }
            else
            {
                if (isLive)
                {
                    DebugLogger.Instance.Log($"LIVE mode: Setting visibility TRUE for {gameObject.name}");
                    //Set mesh renderer for playbackObject 
                    _meshRenderer.enabled = false;
                    if (AssetManager.Instance != null && AssetManager.Instance.translucentMaterial != null)
                    {
                        _meshRenderer.material = AssetManager.Instance.translucentMaterial;
                    }

                    if (gameObject.GetComponentInChildren<TextAsset>() != null)
                    {
                        gameObject.GetComponent<TextAsset>().SetTextVisibility(false);
                    }
                }
                else
                {
                    _meshRenderer.material = AssetManager.Instance.translucentMaterial;
                }
            }
        }
    }


    public void NotifyMeshUpdated()
    {
        DebugLogger.Instance.Log($"Asset {gameObject.name}: mesh updated");
        OnMeshUpdated?.Invoke(this);
    }

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _light = GetComponent<Light>();
        _dust = GetComponent<ParticleSystem>();
        // Use sharedMaterial as the "default" reference (avoids implicit instancing).
        if (_meshRenderer != null)
        {
            defaultMaterial = _meshRenderer.sharedMaterial;
        }

        _grabbable = GetComponent<Grabbable>();
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
        }
    }

    public bool _IsAnimated = false;

    public void LightOn()
    {
        if (_light == null) _light = GetComponent<Light>();
        if (_light == null)
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.intensity = 1;
        }
        else
        {
            _light.enabled = true;
        }
    }

    public void DustOn()
    {
        if (_dust == null) _dust = GetComponent<ParticleSystem>();
        if (_dust == null)
        {
            _dust = gameObject.AddComponent<ParticleSystem>();
            var main = _dust.main;
            main.startLifetime = 2f; // Duration
            main.startSize = 0.5f; // Dimension
            main.startSpeed = 1f; // Velocity
            main.startColor = new Color(0.6f, 0.6f, 0.6f); // Color
            main.startRotation = 0f; // Rotation

            var shape = _dust.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.5f;

            //Set render material to DustMaterial
            var renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = AssetManager.Instance.translucentMaterial;

            var velocityOverLifetime = _dust.velocityOverLifetime;
            velocityOverLifetime.y = -0.5f;
        }

        // Start the particle system immediatelys
        if (!_dust.isPlaying) _dust.Play();
    }


    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
        }
    }

    private void HandlePointerEventRaised(PointerEvent pointerEvent)
    {
        //Check if the event is a release
        if (pointerEvent.Type == PointerEventType.Unselect)
        {
            SetInitialVisualMainValues();

            Recorder.Instance.UpdateAllAssetFramesAndCollisions((int)Recorder.Instance.playbackSlider.value, Recorder.Instance.currentActiveExample,
                Recorder.Instance.RecreateTimelineUI_Collisions);
        }
    }

    public void SetInitialVisualMainValues()
    {
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
    }

    void Start()
    {
        defaultMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    void FixedUpdate()
    {
        /*
        if(recordable.currentRecordingMode == Recordable.AssetRecordingType.ManualAnimation)
        {
            if (Vector3.Distance(recordable.gameObject.transform.position, lastAssetPosition) > movementRecordThreshold)
            {
                DebugLogger.Instance.Log("Added new frame data for " + recordable.gameObject.name + " at " + mainRecorder.playbackSlider.value);

                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "Collide(" + Manager.Instance.CleanString(recordable.gameObject.name) + ", hand)", true);
                recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, actionStr: "None", collisionStr: "None", propagateValueToSubsequentFrames: true);
                //Increment the slider value by a small value proportional to the total recording time
                mainRecorder.playbackSlider.value += playbackSpeed;
            }
            lastAssetPosition = recordable.gameObject.transform.position;

        }

        if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);
            //Increment the slider value by frame duration
            if( Recorder.Instance.playbackSlider.value + 1 <= Recorder.Instance.playbackSlider.maxValue)
                Recorder.Instance.playbackSlider.value += 1;
            //ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, propagateValueToSubsequentFrames: true);
        }
        */
    }

    public void PlaybackAssetFrame()
    {
        int currentFrameNum = (int)Recorder.Instance.playbackSlider.value;
        // var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames; // CHECKIFSAFETODELETE 140426
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.GetAssetFramesReadOnly(this);

        //foreach (var recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        //{
        //var data = Recorder.Instance.currentActiveExample.assetDataDict[recordable];
        if (Recorder.Instance.currentActiveExample.RecordedDataCount > 0 && currentFrameNum < currentAssetRecordedData.Count)
        {
            //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
            transform.position = currentAssetRecordedData[currentFrameNum].rootPosition;
            transform.rotation = currentAssetRecordedData[currentFrameNum].rootRotation;
            CurrentColor = currentAssetRecordedData[currentFrameNum].color;
            IsVisible = currentAssetRecordedData[currentFrameNum].isVisible;
            _IsAnimated = currentAssetRecordedData[currentFrameNum].isAnimated;

            if (_IsAnimated)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }

            /*if (data[currentFrameNum].IsVisible)
            {
                //DebugLogger.Instance.Log("Showing " + recordable.playbackObject.name + " at " + currentFrameNum);
                recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.defaultMaterial;
            }
            else
            {
                //DebugLogger.Instance.Log("Hiding " + recordable.playbackObject.name + " at " + currentFrameNum);
                if(Recorder.Instance.isAutomaticPlayback)
                    recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.transparentMaterial;
                else
                    recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
            }*/
        }
        //}
    }

    public AssetActionSequence RecordAction(ACTION_ENUM actionType, Action actionDelegate, int frameStart = -1)
    {
        if (frameStart == -1)
        {
            frameStart = (int)Recorder.Instance.playbackSlider.value;
        }

        var newAction = new AssetActionSequence
        {
            StartIndex = frameStart,
            ActionType = actionType,
            ActionDelegate = actionDelegate,
            TargetAsset = this
        };
        // Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(newAction);// CHECKIFSAFETODELETE 140426
        Recorder.Instance.currentActiveExample.RegisterActionForGivenAsset(this, newAction);

        if (newAction.IsFollow())
        {
            var frameEndForFollow = FindFollowEndIndex((int)Recorder.Instance.playbackSlider.value);
            // TODO frameEndForFollow can be 0 if there is no end action. Knowing that later will do a clipLength = frameEnd - frameStart = 0 - frameStart, are we creating a negative length?
            CreateFollowEndAction(newAction, frameEndForFollow, frameStart);
        }

        Recorder.Instance.UpdateAllAssetFramesAndCollisions(frameStart, Recorder.Instance.currentActiveExample, () =>
        {
            Recorder.Instance.RecreateTimelineUI_Collisions();
            Recorder.Instance.RecreateTimelineUI_ActionsForAsset(this);
        });

        return newAction;
    }

    public void CreateFollowEndAction(AssetActionSequence followAction, int frameEndForFollow, int frameStart)
    {
        followAction.Length = frameEndForFollow - frameStart;

        if (followAction.associatedEndAction != null)
        {
            followAction.associatedEndAction.StartIndex = frameEndForFollow;
        }
        else
        {
            var newEndAction = new AssetActionSequence
            {
                StartIndex = frameEndForFollow,
                ActionType = ACTION_ENUM.FOLLOW_END,
                ActionDelegate = ApplyUnfollow,
                TargetAsset = this
            };

            followAction.associatedEndAction = newEndAction;

            // Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(newEndAction); // CHECKIFSAFETODELETE 140426
            Recorder.Instance.currentActiveExample.RegisterActionForGivenAsset(this, newEndAction);
        }
    }

    public void SaveMainVisualValuesIn(AssetFrame assetFrame)
    {
        assetFrame.rootPosition = transform.position;
        assetFrame.rootRotation = transform.rotation;
        assetFrame.color = CurrentColor;
        // assetFrame.IsVisible = gameObject.GetComponent<MeshRenderer>().enabled;
        assetFrame.isVisible = IsVisible;
        assetFrame.isAnimated = _IsAnimated;
    }

    public void ResetMainVisualValues()
    {
        transform.position = InitialPosition;
        transform.rotation = InitialRotation;
        CurrentColor = Color.gray;
        IsVisible = true;
        _IsAnimated = false;
    }

    //For assets
    public void RecordHide()
    {
        RecordAction(ACTION_ENUM.HIDE, () => { GetComponent<Asset>().IsVisible = false; });

        /*
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 0.5 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
        AssetFrame currentAssetFrame = currentAssetRecordedData[(int)Recorder.Instance.playbackSlider.value];
        //showStatus = false;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + base.gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value,
                                    actionType: ACTION_ENUM.HIDE,
                                    collisionType: currentAssetFrame.CollisionType,
                                    collidedObject: currentAssetFrame.CollidedObject,
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(false); });

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].IsVisible = false;
            }
        }

        SetVisibility(false);

        Recorder.Instance.CreateTimelineActionsForAsset(this);*/
    }

    //For assets
    public void RecordShow()
    {
        RecordAction(ACTION_ENUM.SHOW, () => { GetComponent<Asset>().IsVisible = true; });

        /*var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 1 alpha
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        //showStatus = true;
        AssetFrame currentAssetFrame = currentAssetRecordedData[(int)Recorder.Instance.playbackSlider.value];;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value,
                                    actionType: ACTION_ENUM.SHOW,
                                    collisionType: currentAssetFrame.CollisionType,
                                    collidedObject: currentAssetFrame.CollidedObject,
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(true); });


            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].IsVisible = true;
            }
        }

        SetVisibility(true);*/
    }

    public void RecordAnimate()
    {
        RecordAction(ACTION_ENUM.ANIMATE, () =>
        {
            this.StartAnimation();
            this._IsAnimated = true;
        });
    }

    public void RecordStopAnimate()
    {
        RecordAction(ACTION_ENUM.STOP_ANIMATE, () =>
        {
            this.StopAnimation();
            this._IsAnimated = false;
        });
    }

    public void StartAnimation()
    {
        // if gameObject name is Lamp then add a light component 
        if (gameObject.name.Contains("Lamp"))
        {
            // Light lightComponent = gameObject.GetComponent<Light>();
            // if (lightComponent != null)
            // {
            //     lightComponent.enabled = true;
            // }
            // else
            // {
            LightOn();
            // }
        }
        // if gameObject name is Book then remove the particle system component
        else if (gameObject.name.Contains("Book"))
        {
            // ParticleSystem dustParticleSystem = gameObject.GetComponent<ParticleSystem>();
            // if (dustParticleSystem != null)
            // {
            //     if (!dustParticleSystem.isPlaying)
            //         dustParticleSystem.Play();
            // }
            // else
            // {
            DustOn();
            // }
        }
    }

    private void StopAnimation()
    {
        if (gameObject.name.Contains("Lamp"))
        {
            if (_light == null) _light = gameObject.GetComponent<Light>();
            if (_light != null) _light.enabled = false;
        }
        else if (gameObject.name.Contains("Book"))
        {
            if (_dust == null) _dust = gameObject.GetComponent<ParticleSystem>();
            if (_dust != null) _dust.Stop();
        }
    }

    public void RecordColorChange(UnityEngine.UI.Image buttonImage)
    {
        var color = buttonImage.color;
        var assetActionSequence = RecordAction(ACTION_ENUM.CHANGE_COLOR, () => CurrentColor = color);
        assetActionSequence.StoredColor = this.CurrentColor;
        // RecordAction(ACTION_ENUM.CHANGE_COLOR, () =>
        // {
        //     //TODO this is not considering live vs other modes
        //     GetComponent<Asset>().CurrentColor = color;
        // });
    }

    public void ReplaceMesh(GameObject newMeshObj)
    {
        //Replace the mesh of the current gameObject with the mesh of newMeshObj
        var mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null) return;

        var otherMf = newMeshObj.GetComponent<MeshFilter>();
        if (otherMf == null) return;

        mf.mesh = otherMf.mesh;
    }

    public void RecordPin(Vector3 position)
    {
        //TODO For now, this behaviour is the same for all the modes of the app
        var assetActionSequence = RecordAction(ACTION_ENUM.PIN, () => Pin(position));
        assetActionSequence.StoredPinPosition = position;
    }

    public void Pin(Vector3 location)
    {
        transform.position = location;
    }

    private int FindFollowEndIndex(int frameStart)
    {
        if (Recorder.Instance == null || Recorder.Instance.currentActiveExample == null) return 0;

        var placeholders = Recorder.Instance.currentActiveExample.StatePlaceholders;
        foreach (var sp in placeholders)
        {
            int end = sp.StartIndex + sp.Length;
            if (end > frameStart) return end;
        }

        return 0;
    }

    //For assets
    public void RecordFollow(FollowLineTrigger.FollowTargetType followTargetType)
    {
        GameObject liveTarget; // followTargetInLiveMode
        GameObject playbackTarget; // followTargetInPlayback
        ACTION_ENUM actionType;

        switch (followTargetType)
        {
            case FollowLineTrigger.FollowTargetType.LEFTHAND:
                actionType = ACTION_ENUM.FOLLOW_LEFT_HAND;
                liveTarget = InputManager.Instance.leftHandPinchObj;
                playbackTarget = InputManager.Instance.playbackLeftHandPinchObj;
                break;
            case FollowLineTrigger.FollowTargetType.RIGHTHAND:
                actionType = ACTION_ENUM.FOLLOW_RIGHT_HAND;
                liveTarget = InputManager.Instance.rightHandPinchObj;
                playbackTarget = InputManager.Instance.playbackRightHandPinchObj;
                break;
            case FollowLineTrigger.FollowTargetType.RIGHTFOCUS:
                actionType = ACTION_ENUM.FOLLOW_R_FOCUS;
                liveTarget = InputManager.Instance.rightFocus;
                playbackTarget = InputManager.Instance.playbackRightFocus;
                break;
            case FollowLineTrigger.FollowTargetType.LEFTFOCUS:
                actionType = ACTION_ENUM.FOLLOW_L_FOCUS;
                liveTarget = InputManager.Instance.leftFocus;
                playbackTarget = InputManager.Instance.playbackLeftFocus;
                break;
            case FollowLineTrigger.FollowTargetType.GAZEFOCUS:
                actionType = ACTION_ENUM.FOLLOW_G_FOCUS;
                liveTarget = InputManager.Instance.gazeFocus;
                playbackTarget = InputManager.Instance.playbackGazeFocus;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(followTargetType), followTargetType, null);
        }

        RecordAction(actionType, () =>
        {
            bool isLive = Manager.Instance != null && Manager.Instance.currAppState == Manager.AppState.LIVE;
            var target = isLive ? liveTarget : playbackTarget;
            if (target != null) ApplyFollow(target.transform);

            // if (Manager.Instance.currAppState == Manager.AppState.LIVE)
            // {
            //     GetComponent<Asset>().ApplyFollow(liveTarget.transform);
            // }
            // else
            // {
            //     GetComponent<Asset>().ApplyFollow(playbackTarget.transform);
            // }
        });
    }

    // //For assets
    public void Detach()
    {
        RecordUnfollow((int)Recorder.Instance.playbackSlider.value);
    }

    public void RecordUnfollow(int startFrame) //TODO Why is startFrame ignored?
    {
        var currentFrame = (int)Recorder.Instance.playbackSlider.value;
        //Find the associated Follow action
        // var associatedFollowAction = Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.OrderBy(x => x.StartIndex).LastOrDefault(assetAction => assetAction.IsFollow() && assetAction.StartIndex < currentFrame) ?? null; // CHECKIFSAFETODELETE 140426
        var associatedFollowAction = Recorder.Instance.currentActiveExample.GetAssetActionsReadOnly(this)
            .OrderBy(x => x.StartIndex).LastOrDefault(assetAction => 
                assetAction.IsFollow() && assetAction.StartIndex < currentFrame) ?? null;

        if (associatedFollowAction == null)
        {
            return;
        }

        var frameStart = associatedFollowAction.StartIndex;

        CreateFollowEndAction(associatedFollowAction, currentFrame, frameStart);

        Recorder.Instance.UpdateAllAssetFramesAndCollisions(frameStart, Recorder.Instance.currentActiveExample, () =>
        {
            Recorder.Instance.RecreateTimelineUI_Collisions();
            Recorder.Instance.RecreateTimelineUI_ActionsForAsset(this);
        });
    }

    private Rigidbody EnsureRigidbody()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        return _rigidbody;
    }

    //For assets
    public void ApplyForce(Vector3 initialVelocity)
    {
        var rb = EnsureRigidbody();
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;

        //Added Time.fixedDeltaTime to account when we run the simulation at 10x
        //0.02f should be the default fixedDeltaTime
        rb.AddForce(initialVelocity / Time.fixedDeltaTime * 0.02f, ForceMode.VelocityChange);
    }


    public void ApplyForceInLiveMode(Vector3 initialVelocity)
    {
        DebugLogger.Instance.Log($"ApplyForceInLiveMode : Velocity during recording is {initialVelocity}");

        var rb = EnsureRigidbody();
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;

        //Find whether InputManager.Instance.leftPinchObj or InputManager.Instance.rightPinchObj is closest to the asset
        float distLeft = Vector3.Distance(transform.position, InputManager.Instance.leftHandPinchObj.transform.position);
        float distRight = Vector3.Distance(transform.position, InputManager.Instance.rightHandPinchObj.transform.position);

        if (distLeft < distRight)
        {
            DebugLogger.Instance.Log($"ApplyForceInLiveMode : Applying force (left hand) speed {InputManager.Instance.leftHandVelocity.magnitude}");
            rb.AddForce(InputManager.Instance.leftHandVelocity, ForceMode.VelocityChange);
        }
        else
        {
            DebugLogger.Instance.Log($"ApplyForceInLiveMode : Applying force (right hand) speed {InputManager.Instance.rightHandVelocity.magnitude}");
            rb.AddForce(InputManager.Instance.rightHandVelocity, ForceMode.VelocityChange);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        var currentFrameIndex = (int)Recorder.Instance.playbackSlider.value;

        //Added to save the PotentialAssetToChange while dragging a PremadeAsset such as the Basketball
        if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK && other.gameObject.name.StartsWith("Premade"))
        {
            other.gameObject.GetComponent<MeshCopy>().PotentialAssetToChange = gameObject;
        }

        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //If we are simulating we need to save the collision
            string[] userHandsAndHeadTags = { "Head", "LeftHand", "RightHand" };

            if (!other.gameObject.CompareTag("Untagged"))
            {
                if (!userHandsAndHeadTags.Contains(other.gameObject.tag))
                {
                    List<AssetActionSequence> addedResetPhysicsActions = new();

                    // foreach (var action in Recorder.Instance.currentActiveExample.assetsDict[this].assetActions) // CHECKIFSAFETODELETE 140426
                    foreach (var action in Recorder.Instance.currentActiveExample.GetAssetActionsReadOnly(this))
                    {
                        if (action.ActionType == ACTION_ENUM.APPLY_FORCE_START && action.Length == 0 &&
                            action.StartIndex <= currentFrameIndex)
                        {
                            //This asset has a pending unclosed APPLY_FORCE_START action, this collision is the end of that action
                            action.Length = currentFrameIndex - action.StartIndex;

                            var newResetPhysicsAction = new AssetActionSequence
                            {
                                StartIndex = currentFrameIndex,
                                ActionType = ACTION_ENUM.APPLY_FORCE_END,
                                ActionDelegate = ResetPhysicsPropertiesInLiveMode,
                                TargetAsset = this
                            };

                            action.associatedEndAction = newResetPhysicsAction;

                            addedResetPhysicsActions.Add(newResetPhysicsAction);

                            ResetPhysicsPropertiesInLiveMode();
                        }
                    }

                    foreach (var resetPhysicsActionToAdd in addedResetPhysicsActions)
                    {
                        // Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(resetPhysicsActionToAdd); // CHECKIFSAFETODELETE 140426
                        Recorder.Instance.currentActiveExample.RegisterActionForGivenAsset(this, resetPhysicsActionToAdd);
                    }
                }

                DebugLogger.Instance.Log("Asset.OnCollisionEnter: AddNewCollision >> collision between "
                                         + base.gameObject.name + " and " + other.gameObject.name);
                // if (!base.gameObject.CompareTag("Floor") &&
                //     !other.gameObject.CompareTag("Floor"))
                // {
                Recorder.Instance.currentActiveExample.AddNewCollision((int)Recorder.Instance.playbackSlider.value,
                    base.gameObject, other.gameObject);
                // }
            }
        }

        InputManager.Instance.SaveCurrentCollision(base.gameObject, other.gameObject);

        //TODO Check if this is needed

        //Return if asset is colliding with inputmanager's left or right pinch objects
        //Another way of saying this is: if you are colliding with the real-time hands or head ignore the collision
        // if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || 
        //     collision.collider.name == "HeadContactSphere" || collision.collider.name == "OVRRightHandVisual_Playback" || 
        //     collision.collider.name == "OVRLeftHandVisual_Playback")
        // {
        //     return;
        // }

        // if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        // {            
        //     Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //
        //     DebugLogger.Instance.Log("Recordable.OnCollisionEnter: Collision detected between " + base.gameObject.name + " and " + collision.collider.name);            
        //
        //     ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
        //                         // actionType: ACTION_ENUM.APPLY_FORCE_END, 
        //                         // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + "," + Manager.Instance.CleanAssetName(collision.collider.name) + ")", 
        //                         collisionType: COLLISION_ENUM.COLLIDE, 
        //                         // actionDelegate: () => { GetComponent<Asset>().ResetPhysicsPropertiesInLiveMode(); }, 
        //                         collidedObject: collision.collider.gameObject);
        //
        //     ResetPhysicsProperties();           
        // }
    }

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }

    //For assets
    public void ResetPhysicsProperties()
    {
        DebugLogger.Instance.Log("ResetPhysicsProperties called for " + gameObject.name);
        //Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //Recorder.Instance.isMainPlaybackOn = false;
        InputManager.Instance.leftHandPinchObj.SetActive(true);
        InputManager.Instance.rightHandPinchObj.SetActive(true);

        InputManager.Instance.ResetCurrentCollisions();

        var rb = EnsureRigidbody();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.useGravity = false;
    }

    public void ResetPhysicsPropertiesInLiveMode()
    {
        //Recorder.Instance.isMainPlaybackOn = false;
        DebugLogger.Instance.Log("Resetting physics properties in live mode");

        var rb = EnsureRigidbody();
        rb.mass = 0f; //TODO Check we don't do this when ApplyForce
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.useGravity = false;
    }

    //For assets
    void OnCollisionStay(Collision collision)
    {
        if (Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Notifying collision detected between " + base.gameObject.name + " and " + collision.collider.name);
            if (collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" ||
                collision.collider.name == "HeadContactSphere")
            {
                InputManager.Instance.SaveCurrentCollision(base.gameObject, collision.collider.gameObject);
            }
        }
        else //Recording
        {
            //NotifyAsset(collision.collider.gameObject); //Notify the asset that it has collided with another object during recording
            collidedObjectDuringRecording = collision.collider.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Added to remove the PotentialAssetToChange while dragging a PremadeAsset such as the Basketball
        if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK && other.gameObject.name.StartsWith("Premade"))
        {
            var meshCopy = other.gameObject.GetComponent<MeshCopy>();
            if (meshCopy != null)
            {
                meshCopy.PotentialAssetToChange = null;
            }
        }

        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            if (!other.gameObject.CompareTag("Untagged"))
            {
                DebugLogger.Instance.Log("EndPreviousCollision >> collision ended between " + gameObject.name + " and " + other.name);

                Recorder.Instance.currentActiveExample.EndPreviousCollision((int)Recorder.Instance.playbackSlider.value, base.gameObject, other.gameObject);
            }
        }

        if (Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            if (other.name == "LeftHandPinchContactSphere" || other.name == "RightHandPinchContactSphere" || other.name == "HeadContactSphere")
            {
                InputManager.Instance.UnsaveCurrentCollision(base.gameObject, other.transform.gameObject);
            }
        }
        else //Recording
        {
            //NotifyAsset(null); //Notify the asset that it has stopped colliding with another object during recording
            collidedObjectDuringRecording = null;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        OnTriggerExit(collision.collider);
    }

    //For assets
    public void ApplyFollow(Transform other)
    {
        transform.SetParent(other);
    }

    //For assets
    public void ApplyUnfollow()
    {
        transform.SetParent(null);
    }

    Manager.AppState oldAppState;

    public void ShowColliderVisualizer(bool status)
    {
        colliderVisualizerObj.SetActive(status);
        colliderBoundaryGizmoObj1.SetActive(status);
        if (status)
        {
            oldAppState = Manager.Instance.currAppState;
            Manager.Instance.currAppState = Manager.AppState.EDIT_BOUNDING_SPHERE;
        }
        else
        {
            Manager.Instance.currAppState = oldAppState;
        }
    }
}

[System.Serializable]
public class AssetFrame : ICloneable
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    [FormerlySerializedAs("isShown")] [FormerlySerializedAs("showStatusForThisFrame")]
    public bool isVisible = true;

    public Color color;

    public bool isAnimated = false;

    //public string voiceCommand = "None";

    //public Vector3 force;

    //Assets 
    public AssetFrame(Vector3 _position,
        Quaternion _rotation,
        bool _showStatus,
        Color _color,
        bool _isAnimated)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        isVisible = _showStatus;
        color = _color;
        isAnimated = _isAnimated;
    }

    public object Clone()
    {
        // Create a new instance of the class
        AssetFrame clonedFrame = new
        (
            this.rootPosition,
            this.rootRotation,
            this.isVisible,
            this.color,
            this.isAnimated
        );

        // Return the cloned object
        return clonedFrame;
    }
}