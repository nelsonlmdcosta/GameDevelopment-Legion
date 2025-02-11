using UnityEngine;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
public class Rogue : AActor, IAssimilatable
{
	[Header("Skill Variables")]
    [Space(10)]
	[Header("Rogue Blink")]
    [SerializeField] private float blinkDistance = 1;
    //[SerializeField] private float blinkCooldown = 1;

    [Header("Rogue Blink")]
    [SerializeField] private GameObject cloneObject = null;
    [SerializeField] private float cloneDuration = 1;
    //[SerializeField] private float cloneCooldown = 1;

    [Header("Rogue Glitch")]
    [SerializeField] private float glitchDuration = 0;
    //[SerializeField] private float glitchCooldown = 0;
    [SerializeField] private Vector3 higherLimits = Vector3.zero;
    [SerializeField] private Vector3 lowerLimits = Vector3.zero;

    [SerializeField] [Range(0, 1)] private float minLengthPercentile = 0;
    [SerializeField] [Range(0, 1)] private float maxLengthPercentile = 0;

    [SerializeField] private float glitchInterval = 0.2f;

    [Header("Global Cooldown")]
    [SerializeField] private float globalCooldown = 5f;
    private float targetTime;


	[Header("Assimilated Skills")]
	[SerializeField] private float tetherMaxDistance = 0;

    [Header("Lighting")]
    [SerializeField] private float maxIntensity = 5;

    [Header("Rogue Sub Mesh MeshRenderer")]
    [SerializeField] private MeshRenderer[] subMeshes = null;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Light lightSource = null;
    [SerializeField] private AnimationCurve lightCurve = null;

    private int assimilatedBehaviour;
	private Vector3 lineStartPoint = Vector3.zero;
	private Vector3 lineEndPoint = Vector3.zero;
    private Light cloneLightSource;

    public enum ERogueState 
	{ 
		RogueState,			// Acts As A Rogue 
		AssimilatedState    // Plays As Legion
	}

    private enum BehaviourType
	{
		Rogue = 0,
		Tether,
		Cannonball,
		TrailBlazer
	};

	private ERogueState rogueState = ERogueState.RogueState;

	// Rogue Skills

	private int rogueSkillsUnlocked;
    private ASkill[] rogueSkills = new ASkill[3];

	// Cached Components
	private Rigidbody myRigidBody;
	private Transform myTransform;

	private Transform target;
	private LineRenderer line;
	
	// Players Input Controller
    private AController inputController;

	private bool hasCollidedWithLegion;

	// cannonball 
	private bool isPropelled;
	private Vector3 propelledDirection = Vector3.zero;
	[SerializeField] private float propelledVelocity = 14;
    [SerializeField] private float stunDuration = 0; 
    bool canMove = true;

    [Header("Cannon Reticle")]
    [SerializeField] private GameObject cannonReticle = null;

    [Header("Particle Prefabs")]
    [SerializeField] private GameObject blinkParticlePrefab = null;
    [SerializeField] private GameObject cannonParticlePrefab = null;
    [SerializeField] private GameObject stunParticlePrefab = null;
    [SerializeField] private GameObject assimilateParticlePrefab = null;

	public GameObject trailBlazerPrefab;
    public Vector3 trailBlazerDropOffset;

    bool handleLight;

    /// <summary>
    /// To Force The Animator To Transition To A Shape On Caught, use The Following
    /// Parameter Name : SwitchToModel
    /// Tiangle = 1
    /// Circle  = 2
    /// Cross   = 3
    /// Square  = 4
    /// 
    /// Note: 0 Reserved So That The Animations Swap Before Being Caught
    /// </summary>
    private Animator animator;
    private ConfigurableJoint joint;

    private List<Vector3> contactPoints = new List<Vector3>();
    private List<GameObject> intermediatePoints = new List<GameObject>();

    private SoftJointLimit limit = new SoftJointLimit();

    private void Start()
	{
        animator = GetComponent<Animator>();
        myRigidBody = GetComponent<Rigidbody>();
        myTransform = GetComponent<Transform>();
        inputController = ControllerManager.Instance.NewController();
        score = 0;
	    Team = rogueTeamName;

        // Temporary Change Until New Skills Are Added
        RogueBlink blink = gameObject.AddComponent<RogueBlink>();
		blink.Initialize(GetComponent<Transform>(), globalCooldown, blinkDistance, blinkParticlePrefab);

        RogueClone clone = gameObject.AddComponent<RogueClone>();
        clone.Initialize(myTransform, cloneObject, inputController, base.movementSpeed, cloneDuration, globalCooldown);

        RogueGlitch glitch = gameObject.AddComponent<RogueGlitch>();
        glitch.Initialize(Camera.main.gameObject, lowerLimits, higherLimits, glitchDuration, glitchInterval, globalCooldown, minLengthPercentile, maxLengthPercentile);

        rogueSkills[0] = blink;
        rogueSkills[1] = clone;
        rogueSkills[2] = glitch;

        lightSource.intensity = maxIntensity;

	}
	private void Update()
	{
	    if (GameManager.Instance.IsGameOver || GameManager.Instance.IsInLobby())
	        return;

        cloneObject.GetComponent<Animator>().SetBool("Start", true);
        animator.SetBool("Start", true);

		if(line != null)
		{
			line.SetPosition(0, lineStartPoint);
			line.SetPosition(1, lineEndPoint);
		}


        if (handleLight)
            HandleGlobalCooldownLight();

		switch((BehaviourType)assimilatedBehaviour)
		{
		case BehaviourType.Rogue:
			RogueBehaviour();
			return;
		case BehaviourType.Tether:
			TetherBehaviour();
			return;
		case BehaviourType.Cannonball:
			CannonballBehaviour();
			return;
		case BehaviourType.TrailBlazer:
            TrailblazerBehaviour();
			return;
		}

	}

	private void RogueBehaviour()
	{
		if (!canMove) 
		{
            myRigidBody.velocity = Vector2.zero;
            return; 
		}

		HandleMoveInput();

		// Skill handling
        for (int i = 0; i < rogueSkills.Length; i++)
        {
            if (i >= rogueSkillsUnlocked)
            {
                break;
            }

            if (inputController.GetButton((ControllerInputKey)i) && rogueSkills[i].IsReady)
            {
                if (rogueSkills[i].UseSkill())
                {
                    TriggerLight();
                }
            }

            if (inputController.GetButton((ControllerInputKey)i) && rogueSkills[i].IsReady)
            {
                if (rogueSkills[i].UseSkill())
                {
                    TriggerLight();
                }
            }

            if (inputController.GetButton((ControllerInputKey)i) && rogueSkills[i].IsReady)
            {
                if (rogueSkills[i].UseSkill())
                {
                    TriggerLight();
                }
            }
        }
	}
	private void TetherBehaviour()
	{
        HandleMoveInput();

        // updating LineRenderer Points
        UpdateEndPoints();
        UpdateLineRenderer();

        UpdateJointLimits();

        CheckRogueCollision();

        if (UnWrap())
        {
            return;
        }

	    Wrap();
    }
    private void CannonballBehaviour()
    {
        if (myRigidBody.velocity == Vector3.zero)
            isPropelled = false;

        if (Controller.MoveDirection() != Vector3.zero)
        {
            cannonReticle.transform.forward = -Controller.MoveDirection();
            cannonReticle.transform.position = transform.position + Controller.MoveDirection();
        }
		// If FIRE button is pressed, propel forward.
		if (!isPropelled && inputController.GetButton(ControllerInputKey.Circle)) 
		{
			isPropelled = true;
			propelledDirection = inputController.MoveDirection().normalized;
			AudioManager.PlayCannonballFireSound();
		}

		if (isPropelled) 
		{
			myRigidBody.velocity = propelledDirection * propelledVelocity;
		}


    }
    private void TrailblazerBehaviour()
    {
        if (inputController.MoveDirection() != Vector3.zero)
        {
            float x = Mathf.Abs(inputController.MoveDirection().x);
            float z = Mathf.Abs(inputController.MoveDirection().z);

            if (x > z)
            {
                myRigidBody.velocity = new Vector3(inputController.MoveDirection().x, 0, 0) * movementSpeed;
            }
            if (z > x)
            {
                myRigidBody.velocity = new Vector3(0, 0, inputController.MoveDirection().z) * movementSpeed;
            }
            Instantiate(trailBlazerPrefab, myTransform.position, Quaternion.identity);
        }
        else
        {
            myRigidBody.velocity = Vector3.zero;
        }
    }


	private void OnCollisionEnter(Collision obj)
	{
        if (gameObject.CompareTag("CannonBall"))
        {
            ((GameObject)Instantiate(cannonParticlePrefab, transform.position, Quaternion.Euler(90, 0, 0))).GetComponent<Transform>().parent = myTransform;
        }

        if (obj.gameObject.CompareTag("Legion") && obj.gameObject.CompareTag("Tether") && obj.gameObject.CompareTag("TrailBlazer") && hasCollidedWithLegion)
            return;

		if(obj.gameObject.CompareTag("Legion") && assimilatedBehaviour == (int)BehaviourType.Rogue)
		{
			hasCollidedWithLegion = true;
			Assimilate();
		}
        
		if (assimilatedBehaviour == (int)BehaviourType.Cannonball) 
		{
            myRigidBody.velocity = Vector3.zero;

			if(obj.gameObject.CompareTag("Rogue") && isPropelled)
			{
			    var stunnedRogue = obj.gameObject.GetComponent<Rogue>();
			    if (stunnedRogue != null)
			    {
                    StartCoroutine(StunRogue(stunnedRogue));
                }
                myTransform.position = target.position - target.forward;    // we want to return to legion even if we hit the clone
            }

            AudioManager.PlayCannonballIntoWallSound();
			isPropelled = false;
		}
	}
    // Controls Consistant Wall Collisions For CannonBall
    private void OnCollisionStay(Collision obj)
    {
        if (obj.gameObject.CompareTag("Floor"))
        {
            return;
        }

        if (assimilatedBehaviour == (int)BehaviourType.Cannonball)
        {
            myRigidBody.velocity = Vector3.zero;

            if (obj.gameObject.CompareTag("Rogue") && isPropelled)
            {
 			    var stunnedRogue = obj.gameObject.GetComponent<Rogue>();
			    if (stunnedRogue != null)
			    {
                    StartCoroutine(StunRogue(stunnedRogue));
                }
                myTransform.position = target.position - target.forward;    // we want to return to legion even if we hit the clone
            }
            isPropelled = false;
        }
    }

	private void UpdateLineRendererPoints(Vector3 positionA, Vector3 positionB)
	{
		lineStartPoint = positionA;
		lineEndPoint = positionB;
	}

	public void Assimilate()
	{
		GameManager.Instance.Assimilate(this);
        assimilatedBehaviour = GameManager.Instance.GetBehaviourIndex();
        
        SwitchActorBehaviour();

		AudioManager.PlayAssimilationSound ();
	}
	public void UpdateRogueSkillCount()
    {
        if(rogueSkillsUnlocked < rogueSkills.Length)
            rogueSkillsUnlocked++;
    }
    public void SwitchActorBehaviour()
    {
        // HACK: Debug Code To Force Assimilation, Delete After Testing Phase
        //assimilatedBehaviour = (int)BehaviourType.Cannonball;

        ((GameObject)Instantiate(assimilateParticlePrefab, transform.position, Quaternion.Euler(90, 0, 0))).GetComponent<Transform>().parent = myTransform;

        Destroy(cloneObject);

		if (assimilatedBehaviour == (int)BehaviourType.Tether)
        {
            target = GameObject.FindGameObjectWithTag("Legion").GetComponent<Transform>();

            myTransform = GetComponent<Transform>();

            contactPoints.Add(myTransform.position);
            contactPoints.Add(target.position);

            line = GetComponent<LineRenderer>();

            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = target.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            limit.limit = tetherMaxDistance;
            joint.linearLimit = limit;

            movementSpeed *= 3.5f;
            animator.SetInteger("SwitchToModel", 3); // Transition Model To Cross

            myRigidBody.constraints = RigidbodyConstraints.None;
            myRigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            gameObject.tag = "Tether";
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
		else if (assimilatedBehaviour == (int)BehaviourType.Cannonball)
        {
            animator.SetInteger("SwitchToModel", 2); // Transition Model To Circle

            cannonReticle.SetActive(true);

            gameObject.tag = "CannonBall";
            gameObject.layer = LayerMask.NameToLayer("Default");

            target = GameObject.FindGameObjectWithTag("Legion").GetComponent<Transform>();
        }
        else if (assimilatedBehaviour == (int)BehaviourType.TrailBlazer)
        {
            animator.SetInteger("SwitchToModel", 4); // Transition Model To Square

            gameObject.tag = "TrailBlazer";
            gameObject.layer = LayerMask.NameToLayer("Default");

            movementSpeed *= 2;
        }
        else
        {
            animator.SetInteger("SwitchToModel", 1); // Transition Model To Square
        }

        SetRogueColors( GameManager.Instance.LegionColor );

        for (int i = 0; i < rogueSkills.Length; i++)
        {
            Destroy(rogueSkills[i]);
        }
    }

	private IEnumerator StunRogue(Rogue rogue)
	{
	    if (rogue != null)  // we could end up here because we hit the clone
	    {
	        rogue.canMove = false;
	        Instantiate(stunParticlePrefab, rogue.transform.position, Quaternion.Euler(90, 0, 0));
	        AudioManager.PlayCannonballStunSound();
	        yield return new WaitForSeconds(stunDuration);
	        rogue.canMove = true;
	    }
	}

	private void HandleMoveInput()
	{
		/// Rogue Behaviour
		/// Translation and Rotation Handling
        myRigidBody.velocity = (inputController.MoveDirection() * movementSpeed) + new Vector3(0, myRigidBody.velocity.y, 0);

        if (inputController.MoveDirection() != Vector3.zero)
        {
            myTransform.rotation = Quaternion.LookRotation(inputController.MoveDirection());
        }
    }

    private void TriggerLight()
    {
        handleLight = true;
        lightSource.intensity = 0;

        targetTime = Time.time;
    }
    private void HandleGlobalCooldownLight()
    {
        float normalizedTime = Mathf.Clamp01((Time.time - targetTime) / globalCooldown);
        float intensity = lightCurve.Evaluate(   normalizedTime     ) * maxIntensity;
        lightSource.intensity = intensity;
        cloneLightSource.intensity = intensity;
        if (normalizedTime == 1)
        {
            handleLight = false;
        }
    }

    #region Tether and wrapping

    private void UpdateEndPoints()
    {
        contactPoints[0] = myTransform.position;
        contactPoints[contactPoints.Count - 1] = target.position;
    }
    private void UpdateLineRenderer()
    {
        line.SetVertexCount(contactPoints.Count);
        for (int i = 0; i < contactPoints.Count; i++)
        {
            line.SetPosition(i, contactPoints[i]);
        }
    }

    private void UpdateJointLimits()
    {
        if (contactPoints.Count > 2)
        {
            // Get Total Rope Distance
            float totalDistanceFromParent = 0;
            for (int i = 0; i < contactPoints.Count - 1; i++)
            {
                totalDistanceFromParent += Vector3.Distance(contactPoints[i], contactPoints[i + 1]);
            }
            //Debug.Log(totalDistanceFromParent);

            if (totalDistanceFromParent > tetherMaxDistance)
            {
                float delta = totalDistanceFromParent - tetherMaxDistance;
                float epsilon = 0.000001f; // floating point values cannot be guarenteed to be exactly 0
                while (delta > epsilon)
                {
                    float closestPointDistance = Vector3.Distance(contactPoints[0], contactPoints[1]);

                    float limitDistance = delta > closestPointDistance ? 0 : (closestPointDistance - delta);

                    limit.limit = limitDistance;
                    joint.linearLimit = limit;

                    if (limitDistance == 0)
                    {
                        contactPoints.RemoveAt(1);
                        if (intermediatePoints.Count > 0)
                            intermediatePoints.RemoveAt(0);

                        if (intermediatePoints.Count > 0)
                        {
                            joint.connectedBody = intermediatePoints[0].GetComponent<Rigidbody>();
                        }
                        else
                        {
                            joint.connectedBody = target.GetComponent<Rigidbody>();
                        }
                    }
                    delta -= closestPointDistance - limitDistance;
                }
            }


        }
        // Reset Connected Body To Closest Point [Can Be Optimised by placing an event sorta system]
        if (intermediatePoints.Count > 0)
        {
            joint.connectedBody = intermediatePoints[0].GetComponent<Rigidbody>();
        }
        else
        {
            joint.connectedBody = target.GetComponent<Rigidbody>();
            limit.limit = tetherMaxDistance;
            joint.linearLimit = limit;
        }
    }

    private bool UnWrap()
    {
        if (contactPoints.Count > 2)
        {
            RaycastHit hit;
            //// UnWrap From Target Side
            if (!Physics.Linecast(contactPoints[contactPoints.Count - 1], contactPoints[contactPoints.Count - 2], out hit, layerMask))
            {
                if (!Physics.Linecast(contactPoints[contactPoints.Count - 1], contactPoints[contactPoints.Count - 3], out hit, layerMask))
                {
                    contactPoints.RemoveAt(contactPoints.Count - 2);

                    GameObject obj = intermediatePoints[intermediatePoints.Count - 1];
                    intermediatePoints.Remove(obj);
                    Destroy(obj);

                    return true;
                }
            }

            // UnWrap From MySide
            if (!Physics.Linecast(contactPoints[0], contactPoints[1], out hit, layerMask))
            {
                if (!Physics.Linecast(contactPoints[0], contactPoints[2], out hit, layerMask)) // Have to see both to be a valid unwrappoint
                {
                    contactPoints.RemoveAt(1);

                    GameObject obj = intermediatePoints[intermediatePoints.Count - 1];
                    intermediatePoints.Remove(obj);
                    Destroy(obj);

                    return true;
                }
            }
        }
        return false;
    }
    private bool Wrap()
    {
        RaycastHit hit;
        // Wrap From Target Side
        if (Physics.Linecast(contactPoints[contactPoints.Count - 1], contactPoints[contactPoints.Count - 2], out hit, layerMask))
        {
            GameObject obj = new GameObject("ContactPoint" + (contactPoints.Count - 1), typeof(Rigidbody));
            Vector3 dirDiff = (hit.point - hit.collider.transform.position).normalized / 10; // So the normalized vector is substantially smaller
            obj.GetComponent<Transform>().position = hit.point + dirDiff;
            obj.GetComponent<Rigidbody>().isKinematic = true;
            contactPoints.Insert(contactPoints.Count - 1, hit.point + dirDiff);
            intermediatePoints.Add(obj);
            return true;
        }

        // Wrap From MySide
        if (Physics.Linecast(contactPoints[0], contactPoints[1], out hit, layerMask))
        {
            GameObject obj = new GameObject("ContactPoint" + (contactPoints.Count - 1), typeof(Rigidbody));
            Vector3 dirDiff = (hit.point - hit.collider.transform.position).normalized / 10;
            obj.GetComponent<Transform>().position = hit.point + dirDiff;
            obj.GetComponent<Rigidbody>().isKinematic = true;
            contactPoints.Insert(1, hit.point + dirDiff);
            intermediatePoints.Add(obj);
            return true;
        }
        return false;
    }

    private void CheckRogueCollision()
    {
        RaycastHit hit;
        for (int i = 0; i < contactPoints.Count - 1; i++)
        {
            if (Physics.Linecast(contactPoints[i], contactPoints[i + 1], out hit, (1 << LayerMask.NameToLayer("Rogue")) ))
            {
                var capturedRogue = hit.collider.gameObject.GetComponent<Rogue>();
                if (capturedRogue != null)
                {
                    capturedRogue.Assimilate();
                }
            }
        }
    }

    #endregion

    #region Properties

    public ERogueState RogueState
	{
		get { return rogueState; }
		set { rogueState = value; }
	}

	public AController Controller
	{
		get { return inputController; }
		set { inputController = value; }
	}

	#endregion 

    public void SetRogueColors(Color color)
    {
        for (int i = 0; i < subMeshes.Length; i++)
        {
            subMeshes[i].material.color = color;
        }
        cloneObject = Instantiate(cloneObject, Vector3.zero, Quaternion.identity) as GameObject;

        cloneObject.GetComponent<RogueCloneMeshReferences>().UpdateCloneColors(color);

        cloneObject.GetComponent<Rigidbody>().isKinematic = true;
        cloneObject.GetComponent<Transform>().position = new Vector3(10000, 10000, 10000);

        cloneLightSource = cloneObject.transform.GetChild(0).GetComponent<Light>();

        lightSource.color = color;
    }
}
