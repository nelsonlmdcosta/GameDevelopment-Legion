using UnityEngine;
/// <summary>
/// Gets All Local Player's Keyboard and Mouse Input.
/// </summary>
public class KMInput : AController
{
	private string moveHorizontal;
	private string moveVertical;
	private string firePower;
	
	public KMInput()
    {
		moveHorizontal 	= "KMMoveHorizontal";
		moveVertical	= "KMMoveVertical";
		firePower 		= "KMFirePower";
    }

    public override void UpdateController()
	{
		moveDirection.x = Input.GetAxis(moveHorizontal);
		moveDirection.z = Input.GetAxis(moveVertical);

		aimDirection.x = ((Input.mousePosition.x / Screen.width) - 0.5f) * 2;
		aimDirection.z = ((Input.mousePosition.y / Screen.height) - 0.5f) * 2;

        isFiringPower = Input.GetButton(firePower);
    }
}
