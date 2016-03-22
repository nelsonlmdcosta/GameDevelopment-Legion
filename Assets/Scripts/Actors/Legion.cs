using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Legion Actor
/// </summary>
public class Legion : AActor
{	
    [SerializeField] private AController inputController;

	// ComponentCaching
	private Rigidbody myRigidbody = null;
	private Transform myTransform = null;

	private void Start()
	{
        // Component Caching
		myRigidbody = GetComponent<Rigidbody>();
		myTransform = GetComponent<Transform>();
	    PlayerNumber = 1;

		// Add Legion Camera
		inputController = ControllerManager.Instance.NewController();
	}
	private void Update()
    {
        if (GameManager.Instance.IsGameOver)    
            return;

        myRigidbody.velocity = (inputController.MoveDirection() * movementSpeed) + new Vector3(0, myRigidbody.velocity.y, 0);

	    if (inputController.MoveDirection() != Vector3.zero)
	    {
	        myTransform.rotation = Quaternion.LookRotation(inputController.MoveDirection());
	        // myTransform.rotation = Quaternion.Slerp( myTransform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
	    }
	}

	#region Properties

	public AController Controller
	{
		get { return inputController; }
		set { inputController = value; }
	}

	#endregion
}
