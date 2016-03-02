using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
public class Rogue : AActor, IAssimilatable
{
	[Header("Skill Variables")]
	[Header("Rogue Speed Buff")]
	[SerializeField] private float speedMultiplier = 1;
	[SerializeField] private float speedDuration = 1;
	[SerializeField] private float speedCooldown = 1;
	
	[Header("Rogue Blink")]
	[SerializeField] private float blinkDistance = 1;
	[SerializeField] private float blinkCooldown = 1;
	
	[Header("Rogue Invisibility")]
	[SerializeField] private float invisibilityDuration = 1;
	[SerializeField] private float invisibilityCooldown = 1;


	[Header("Assimilated Skills")]
	[SerializeField] private float tetherMaxDistance = 0;

    [Header("Assimilated Meshes")]
    [SerializeField] private Mesh tetherMesh = null;

	private int assimilatedBehaviour = 0;
	private Vector3 lineStartPoint = Vector3.zero;
	private Vector3 lineEndPoint = Vector3.zero;

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
	private int skillIndex = 0;
    private int rogueSkillsUnlocked = 0;
    private ASkill[] rogueSkills = new ASkill[3];

	// Cached Components
	private Rigidbody myRigidBody = null;
	private Transform myTransform = null;

	private Transform target = null;
	private LineRenderer line = null;
	
	// Players Input Controller
    private AController inputController = null;

	private float movementOffset = 1;
	private bool canSwitchSkills = true;
	private bool hasCollidedWithLegion = false;

    public GameObject myMeshHolder;
    public GameObject myLegionMeshholder;

    private Light lightSource;

	// cannonball 
	private bool isPropelled = false;
	private Vector3 propelledDirection = Vector3.zero;
	[SerializeField] float propelledVelocity = 14;
	bool canMove = true;

    /// <summary>
    /// /////////////////////////////////////
    /// </summary>
    [SerializeField]
    private LayerMask layerMask;

    private ConfigurableJoint joint;

    private List<Vector3> contactPoints = new List<Vector3>();
    private List<GameObject> intermediatePoints = new List<GameObject>();

    private SoftJointLimit limit = new SoftJointLimit();







	private void Start()
	{
        //GameManager.Instance.RegisterRogueElement(this);

		myRigidBody = GetComponent<Rigidbody>();
        myTransform = GetComponent<Transform>();

		inputController = ControllerManager.Instance.NewController();

		//RogueSpeedUp speedBoost = gameObject.AddComponent<RogueSpeedUp>();
		//speedBoost.Initialize(this, speedMultiplier, speedDuration, speedCooldown);
		//rogueSkills[0] = speedBoost;

        // Temporary Change Until New Skills Are Added
		RogueBlink dash = gameObject.AddComponent<RogueBlink>();
		dash.Initialize(GetComponent<Transform>(), blinkCooldown, blinkDistance);
		rogueSkills[0] = dash;
		rogueSkills[1] = dash;
		rogueSkills[2] = dash;

		//RogueStealth stealth = gameObject.AddComponent<RogueStealth>();
		//stealth.Initialize(GetComponent<MeshRenderer>(), invisibilityDuration, invisibilityCooldown);
		//rogueSkills[2] = stealth;

        lightSource = GetComponentInChildren<Light>();
	}
	private void Update()
	{
	    if (GameManager.Instance.IsGameOver)
	        return;

		if(line != null)
		{
			line.SetPosition(0, lineStartPoint);
			line.SetPosition(1, lineEndPoint);
		}

        HandleMoveInput();

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
			return; 
		}

		HandleMoveInput();

		// Skill handling
		if (rogueSkillsUnlocked > 0)
		{
			if (inputController.SwitchingPower() && canSwitchSkills)
			{
				SwitchSkill();
				StartCoroutine(SwitchPowerCD(0.5f));
			}
			
			if (inputController.FiringPower())
			{
                if (rogueSkills[skillIndex].UseSkill())
                {
                    lightSource.intensity = 0;
                }
			}
		}
	}
	private void TetherBehaviour()
	{
        // updating LineRenderer Points
        UpdateEndPoints();
        UpdateLineRenderer();

        // Updating Joint
        UpdateJointLimits();

        CheckRogueCollision();

        if (UnWrap())
        {
            return;
        }

        if (Wrap())
        {
            return;
        }
	}
    private void CannonballBehaviour()
    {

		// If FIRE button is pressed, propel forward.
		if (!isPropelled && inputController.FiringPower ()) 
		{
			isPropelled = true;
			propelledDirection = inputController.MoveDirection();
		}

		if (isPropelled) 
		{
			myRigidBody.velocity = propelledDirection * propelledVelocity;
		}


    }

    private void TrailblazerBehaviour()
    {

    }

	private void OnCollisionEnter(Collision obj)
	{
		if(obj.gameObject.CompareTag("Legion") && !hasCollidedWithLegion)
		{
			hasCollidedWithLegion = true;
			Assimilate();
		}

		if (assimilatedBehaviour == (int)BehaviourType.Cannonball) 
		{
			if(obj.gameObject.CompareTag("Rogue") && isPropelled == true)
			{
				StunRogue(obj.gameObject);
			}
			isPropelled = false;
		}
	}

	void StunRogue (GameObject rogueObject)
	{
		var rogue = GetComponent<Rogue> ();

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
	}
	public void UpdateRogueSkillCount()
    {
        rogueSkillsUnlocked++;
    }
    public void SwitchActorBehaviour()
    {
        // Tether Probe
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

            myMeshHolder.SetActive(false);
            myLegionMeshholder.SetActive(true);

            gameObject.tag = "Untagged";
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
        // Assimilate To Tether Legion
		else if (assimilatedBehaviour == (int)BehaviourType.Cannonball)
        {
            myMeshHolder.SetActive(false);
            myLegionMeshholder.SetActive(true);

            gameObject.tag = "Untagged";


        }
        // Assimilate To Probe legion
		else if (assimilatedBehaviour == (int)BehaviourType.TrailBlazer)
        {
            myMeshHolder.SetActive(false);
            myLegionMeshholder.SetActive(true);

            gameObject.tag = "Untagged";

        }

        for (int i = 0; i < rogueSkills.Length; i++)
        {
            Destroy(rogueSkills[i]);
        }
    }

	private void SwitchSkill()
	{
		if(++skillIndex >= rogueSkillsUnlocked)
		{
			skillIndex = 0;
		}
	}
	private IEnumerator SwitchPowerCD(float time) // Use Lambda Expresion/Action
	{
		return HoldRogue (time, (b) => canSwitchSkills = b);
	}

	private IEnumerator StunRogue(float time)
	{
		return HoldRogue (time, (b) => canMove = b);
	}

	private IEnumerator HoldRogue(float time, Action<bool> setConstraint)
	{
		setConstraint(false);
		yield return new WaitForSeconds(time);
		setConstraint(true);
	}


	private void HandleMoveInput()
	{
		/// Rogue Behaviour
		/// Translation and Rotation Handling
		myRigidBody.velocity = inputController.MoveDirection() * movementSpeed * movementOffset;

        if (inputController.MoveDirection() != Vector3.zero)
        {
            myTransform.rotation = Quaternion.LookRotation(inputController.MoveDirection());
            //myTransform.rotation = Quaternion.Slerp(myTransform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
        }
    }

    private void HandleGlobalCooldownLight()
    {
        lightSource.intensity = Mathf.Lerp(lightSource.intensity, 1, Time.deltaTime / blinkCooldown);
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
                hit.collider.gameObject.GetComponent<Rogue>().Assimilate();
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

	public float MovementOffset
	{
		get { return movementOffset; }
		set { movementOffset = value; }
	}

	#endregion 
}
