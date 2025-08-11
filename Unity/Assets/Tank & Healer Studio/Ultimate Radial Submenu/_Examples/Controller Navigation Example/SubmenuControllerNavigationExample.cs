/* SubmenuControllerNavigationExample.cs */
/* Written by Kaz */
using UnityEngine;

namespace UltimateRadialSubmenuExample
{
	public class SubmenuControllerNavigationExample : MonoBehaviour
	{
		public UltimateRadialSubmenu subMenu;
#if ENABLE_INPUT_SYSTEM
		public UnityEngine.InputSystem.InputAction backButton;
#else
		public string backButton = "Cancel";
#endif


		private void Start ()
		{
#if ENABLE_INPUT_SYSTEM
			backButton.Enable();
#endif
		}

		private void Update ()
		{
#if ENABLE_INPUT_SYSTEM
			if( backButton.WasPressedThisFrame() )
#else
			if( Input.GetButtonDown( backButton ) )
#endif
			{
				subMenu.Disable();
			}
		}
	}
}