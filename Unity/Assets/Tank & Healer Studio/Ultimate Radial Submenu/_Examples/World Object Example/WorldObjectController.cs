/* WorldObjectController.cs */
/* Written by Kaz */
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateRadialSubmenuExample
{
	public class WorldObjectController : MonoBehaviour
	{
		[System.Serializable]
		public class ObjectColors
		{
			public Color color = Color.white;
			public UltimateRadialSubButtonInfo buttonInfo;
		}
		public ObjectColors[] objectColors;

		[System.Serializable]
		public class ObjectMesh
		{
			public MeshFilter meshFilter;
			public UltimateRadialButtonInfo buttonInfo;
		}
		public ObjectMesh[] objectMeshes;

		Transform onMouseDownTransform;
		Renderer selectedRenderer;

		public string radialMenuName = "ObjectExample";
		UltimateRadialMenu radialMenu;
		UltimateRadialSubmenu submenu;
		

		private void Start ()
		{
			// Store the radial and sub menus.
			radialMenu = UltimateRadialMenu.ReturnComponent( radialMenuName );
			submenu = UltimateRadialSubmenu.ReturnComponent( radialMenuName );

			// Loop through all the object meshes assigned...
			for( int i = 0; i < objectMeshes.Length; i++ )
			{
				// Store the button id for reference and register the object to the radial menu.
				objectMeshes[ i ].buttonInfo.id = i;
				radialMenu.RegisterButton( UpdateObjectMesh, objectMeshes[ i ].buttonInfo );
			}
		}

		private void Update ()
		{
			Vector2 mousePosition = Vector2.zero;
			bool mouseButtonDown = false;
			bool mouseButtonUp = false;
#if ENABLE_INPUT_SYSTEM
			// Store the mouse from the input system.
			Mouse mouse = InputSystem.GetDevice<Mouse>();

			// If the mouse couldn't be stored from the input system, then return.
			if( mouse == null )
				return;

			// Store the mouse data.
			mousePosition = mouse.position.ReadValue();
			mouseButtonDown = mouse.leftButton.wasPressedThisFrame;
			mouseButtonUp = mouse.leftButton.wasReleasedThisFrame;
#else
			// Store the mouse data.
			mousePosition = Input.mousePosition;
			mouseButtonDown = Input.GetMouseButtonDown( 0 );
			mouseButtonUp = Input.GetMouseButtonUp( 0 );
#endif

			// If the left mouse button is down on the frame...
			if( mouseButtonDown )
			{
				// Cast a ray so that we can check if the mouse position is over an object.
				Ray ray = Camera.main.ScreenPointToRay( mousePosition );

				RaycastHit hit;

				// If the raycast hit something, then store the hit transform.
				if( Physics.Raycast( ray, out hit ) )
					onMouseDownTransform = hit.transform;
				// Else the raycast didn't hit an object...
				else
				{
					// So if the radial menu is currently enabled...
					if( radialMenu.IsEnabled )
					{
						// If the radial menu is not interactable, and the sub menu is enabled, and the button index of the sub menu is unassigned, OR the radial menu doesn't have any input on it, then there is no input on the menus. Disable the menu.
						if( ( !radialMenu.Interactable && submenu.IsEnabled && submenu.CurrentButtonIndex < 0 ) || radialMenu.CurrentButtonIndex < 0 )
							radialMenu.Disable();
					}
				}
			}

			// If the left mouse button came up on this frame...
			if( mouseButtonUp )
			{
				// Cast a ray so that we can check if the mouse position is over an object.
				Ray ray = Camera.main.ScreenPointToRay( mousePosition );

				RaycastHit hit;

				// If the raycast hit something...
				if( Physics.Raycast( ray, out hit ) )
				{
					// If the hit transform is the same as the transform when the mouse button was pressed...
					if( hit.transform == onMouseDownTransform )
					{
						// Configure the screen position of the hit transform.
						Vector3 screenPosition = Camera.main.WorldToScreenPoint( hit.transform.position );

						// Call SetPosition() on the radial menu to move it to the transform's position.
						radialMenu.SetPosition( screenPosition );

						// If the radial menu is currently disabled, then enable the menu.
						if( !radialMenu.IsEnabled )
							radialMenu.Enable();

						// Store the selected renderer as the hit transform.
						selectedRenderer = hit.transform.GetComponent<Renderer>();

						// If the selected renderer is found...
						if( selectedRenderer != null )
						{
							// Find the mesh of the renderer...
							Mesh rendererMesh = selectedRenderer.GetComponent<MeshFilter>().mesh;

							// Loop through the object options assigned in the inspector...
							for( int i = 0; i < objectMeshes.Length; i++ )
							{
								// If the name of the mesh for the object is the same as the hit mesh, then exclusively select the radial menu button and break the loop.
								if( objectMeshes[ i ].meshFilter.mesh.name == rendererMesh.name )
								{
									objectMeshes[ i ].buttonInfo.SelectButton( true );
									break;
								}
							}
						}
					}
				}
			}
		}

		public void UpdateObjectMesh ( int id )
		{
			// If the selected renderer is null, then just return.
			if( selectedRenderer == null )
				return;

			// Select this button exclusively on the radial menu.
			objectMeshes[ id ].buttonInfo.SelectButton( true );

			// Assign the mesh and force the name so that we can tell what mesh this object is using.
			selectedRenderer.GetComponent<MeshFilter>().mesh = objectMeshes[ id ].meshFilter.mesh;
			selectedRenderer.GetComponent<MeshFilter>().mesh.name = objectMeshes[ id ].meshFilter.mesh.name;

			// Clear the sub menu.
			submenu.ClearMenu();

			// Loop through all the color options.
			for( int i = 0; i < objectColors.Length; i++ )
			{
				// Store the id of this option into the button info.
				objectColors[ i ].buttonInfo.id = i;

				// Register the button to the sub menu.
				submenu.RegisterButton( UpdateCubeColor, objectColors[ i ].buttonInfo );

				// Set the color of the sub button.
				objectColors[ i ].buttonInfo.subButton.icon.color = objectColors[ i ].color;

				if( selectedRenderer.material.color == objectColors[ i ].color )
					objectColors[ i ].buttonInfo.Selected = true;
				else
					objectColors[ i ].buttonInfo.Selected = false;
			}

			// Enable the sub menu.
			submenu.Enable();
		}

		public void UpdateCubeColor ( int id )
		{
			// If the selected renderer is null, then just return.
			if( selectedRenderer == null )
				return;

			// Set the color of the material to the selected color.
			selectedRenderer.material.color = objectColors[ id ].color;

			// Select this button on the submenu exclusively.
			objectColors[ id ].buttonInfo.SelectButton( true );
		}
	}
}