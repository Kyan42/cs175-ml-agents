using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class WalkerAgent : Agent
{
    [Header("Walk Speed")]
    [Range(0.1f, 10)]
    [SerializeField]
    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = 10;

    public float MTargetWalkingSpeed // property
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    const float m_maxWalkingSpeed = 10; //The max walking speed

    //Should the agent sample a new goal velocity each episode?
    //If true, walkSpeed will be randomly set between zero and m_maxWalkingSpeed in OnEpisodeBegin()
    //If false, the goal velocity will be walkingSpeed
    public bool randomizeWalkSpeedEachEpisode;

    //The direction an agent will walk during training.
    private Vector3 m_WorldDirToWalk = Vector3.right;

    [Header("Target To Walk Towards")] public Transform target; //Target the agent will walk towards during training.
    [Header("Arena Settings")]
    public Collider arenaFloor;
    private Vector3 m_BoundsCenter;
    private Vector3 m_BoundsExtents;
    private float m_previousTargetDistToEdge;
    private float m_previousDistToTarget;

    [Header("Body Parts")] public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

        //Setup each body part
        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // cache the bounds of the arena
        m_BoundsCenter = arenaFloor.bounds.center;
        m_BoundsExtents = arenaFloor.bounds.extents;
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        hips.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;
        // teleport the cube to its starting position
        // The Curriculum Spawn (5 to 7 units away at a random angle)
        float spawnDistance = Random.Range(5f, 7f); 
        Vector2 randomDir = Random.insideUnitCircle.normalized; 
        
        target.position = new Vector3(
            m_BoundsCenter.x + (randomDir.x * spawnDistance), 
            m_BoundsCenter.y + 0.5f, 
            m_BoundsCenter.z + (randomDir.y * spawnDistance)
        );

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector3.zero;
            targetRb.angularVelocity = Vector3.zero;
        }

        // Accurately measure the starting distances for the economy
        m_previousDistToTarget = Vector3.Distance(hips.position, target.position);
        
        float targetDistX = Mathf.Abs(target.position.x - m_BoundsCenter.x);
        float targetDistZ = Mathf.Abs(target.position.z - m_BoundsCenter.z);
        m_previousTargetDistToEdge = Mathf.Min(m_BoundsExtents.x - targetDistX, m_BoundsExtents.z - targetDistZ);


        // reset the distance based on the bounds of the arena
        m_previousTargetDistToEdge = Mathf.Min(m_BoundsExtents.x, m_BoundsExtents.z);
        m_previousDistToTarget = Vector3.Distance(hips.position, target.position);
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.linearVelocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position));

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(hips.forward, cubeForward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(target.transform.position));

        // add observations about the agent to the edge of the ring, and the target block to the end of the ring
        Vector3 myPos = hips.position; 
        Vector3 targetPos = target.position;

        // agent distance to edge
        sensor.AddObservation(m_BoundsExtents.x - Mathf.Abs(myPos.x - m_BoundsCenter.x));
        sensor.AddObservation(m_BoundsExtents.z - Mathf.Abs(myPos.z - m_BoundsCenter.z));

        // How close is the block to the edge?
        sensor.AddObservation(m_BoundsExtents.x - Mathf.Abs(targetPos.x - m_BoundsCenter.x));
        sensor.AddObservation(m_BoundsExtents.z - Mathf.Abs(targetPos.z - m_BoundsCenter.z));
        
        // THIS WILL BE WHERE THE OPPONENT'S INFORMATION IS
        // padding with 0s for now, to avoid the network breaking later when we add this information
        for (int j = 0; j < 225; j++)
        {
            sensor.AddObservation(0f);
        }

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        bpDict[chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[thighL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[thighR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[footR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[footL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[armL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[armR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

        //update joint strength settings
        bpDict[chest].SetJointStrength(continuousActions[++i]);
        bpDict[spine].SetJointStrength(continuousActions[++i]);
        bpDict[head].SetJointStrength(continuousActions[++i]);
        bpDict[thighL].SetJointStrength(continuousActions[++i]);
        bpDict[shinL].SetJointStrength(continuousActions[++i]);
        bpDict[footL].SetJointStrength(continuousActions[++i]);
        bpDict[thighR].SetJointStrength(continuousActions[++i]);
        bpDict[shinR].SetJointStrength(continuousActions[++i]);
        bpDict[footR].SetJointStrength(continuousActions[++i]);
        bpDict[armL].SetJointStrength(continuousActions[++i]);
        bpDict[forearmL].SetJointStrength(continuousActions[++i]);
        bpDict[armR].SetJointStrength(continuousActions[++i]);
        bpDict[forearmR].SetJointStrength(continuousActions[++i]);
    }

    //Update OrientationCube and DirectionIndicator
    void UpdateOrientationObjects()
    {
        m_WorldDirToWalk = target.position - hips.position;
        m_OrientationCube.UpdateOrientation(hips, target);
        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }
    }

    void FixedUpdate()
    {
        UpdateOrientationObjects();

        // existential Penalty: forces the agent to move (in theory)
        AddReward(-0.0001f);

        // setup the dynamic ring boundaries using CACHED variables
        Vector3 myPos = hips.position;
        Vector3 targetPos = target.position;

        // reward the agent for getting closer to the block
        float currentDistToTarget = Vector3.Distance(myPos, targetPos);
        float agentProgress = m_previousDistToTarget - currentDistToTarget;
        AddReward(agentProgress * 0.5f); 
        m_previousDistToTarget = currentDistToTarget;

        // calculate absolute distances from the cached center
        float targetDistX = Mathf.Abs(targetPos.x - m_BoundsCenter.x);
        float targetDistZ = Mathf.Abs(targetPos.z - m_BoundsCenter.z);
        float myDistX = Mathf.Abs(myPos.x - m_BoundsCenter.x);
        float myDistZ = Mathf.Abs(myPos.z - m_BoundsCenter.z);

        // dense Progress Reward (greater than the agent getting closer to the block)
        float currentTargetDistToEdge = Mathf.Min(m_BoundsExtents.x - targetDistX, m_BoundsExtents.z - targetDistZ);
        float progress = m_previousTargetDistToEdge - currentTargetDistToEdge;
        AddReward(progress * 1.0f); 
        m_previousTargetDistToEdge = currentTargetDistToEdge;

        // body parts dict
        // var dict = m_JdController.bodyPartsDict;

        // // reward a tall, high posture
        // float postureBonus = (head.position.y - hips.position.y);
        // AddReward(postureBonus * 0.001f); 

        // punish falling or getting close to the floor to encourage bipedalism
        // 1. Did the chest or hips physically hit the floor?
        // bool hitMat = dict[hips].groundContact.touchingGround || dict[chest].groundContact.touchingGround;
        
        // // 2. Is the agent doing a push-up, crawling, or crouching too low? 
        // float currentHipHeight = hips.position.y - m_BoundsCenter.y;
        // bool hipsTooLow = currentHipHeight < 0.55f;
        

        // if (hitMat || hipsTooLow)
        // {
        //     // Debug.Log($"hipsTooLow: {hipsTooLow}  ;  hip position: {currentHipHeight}");
        //     AddReward(-1.0f);
        //     EndEpisode();
        //     return;
        // }

        // 4. Win/Loss Conditions
        if (targetDistX > m_BoundsExtents.x || targetDistZ > m_BoundsExtents.z)
        {
            AddReward(1.0f);
            EndEpisode();
        }
        else if (myDistX > m_BoundsExtents.x || myDistZ > m_BoundsExtents.z)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in m_JdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.linearVelocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    //normalized value of the difference in avg speed vs goal walking speed.
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
    }
}
