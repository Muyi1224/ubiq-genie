/* UltimateRadialSubmenu.cs */
/* Written by Kaz */
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent( typeof( CanvasGroup ) )]
public class UltimateRadialSubmenu : MonoBehaviour
{
	#pragma warning disable IDE0044
	#region ACCESSORS AND SUBMENU POSITIONING
	// ACCESSORS //
	/// <summary>
	/// Returns the overall angle that these submenu buttons fit in to.
	/// </summary>
	public float OverallAngle
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the current input angle so that other scripts can access the current input angle of this submenu.
	/// </summary>
	public float CurrentInputAngle
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the current distance of the input from the center of the main radial menu.
	/// </summary>
	public float CurrentInputDistance
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the calculated minimum range of the submenu.
	/// </summary>
	public float CalculatedMinRange
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the calculated maximum range of the submenu.
	/// </summary>
	public float CalculatedMaxRange
	{
		get;
		private set;
	}
	/// <summary>
	/// The base RectTransform component.
	/// </summary>
	public RectTransform BaseTransform
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the position of the base transform.
	/// </summary>
	public Vector3 BasePosition
	{
		get
		{
			if( BaseTransform == null )
				return Vector3.zero;

			return BaseTransform.position;
		}
	}
	/// <summary>
	/// Returns the index of the button that has focus.
	/// </summary>
	public int CurrentButtonIndex
	{
		get;
		private set;
	} = -1;
	/// <summary>
	/// Returns the current state of the submenu.
	/// </summary>
	public bool IsEnabled
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns if the input is currently on the submenu or not.
	/// </summary>
	public bool InputInRange
	{
		get;
		private set;
	}
	/// <summary>
	/// Returns the current interactable state of the submenu. Setting this value to false will not allow the submenu to be interacted with.
	/// </summary>
	public bool Interactable
	{
		get;
		set;
	}
	/// <summary>
	/// The current radial button on the parent Ultimate Radial Menu that this submenu is associated with.
	/// </summary>
	public UltimateRadialMenu.UltimateRadialButton CurrentRadialButton
	{
		get;
		private set;
	}
	
	// SUBMENU POSITIONING //
	[Header( "Submenu Positioning" )] [Tooltip( "The Ultimate Radial Menu that this submenu will be used with." )]
	public UltimateRadialMenu radialMenu;
	[SerializeField] [Tooltip( "The distance that the submenu should be from the main radial menu." )] [Range( 0.01f, 1.0f )]
	private float submenuDistance = 0.6f;
	[SerializeField] [Tooltip( "The angle between each button on the submenu." )] [Range( 0.0f, 90.0f )]
	private float anglePerButton = 24.0f;
	[SerializeField] [Tooltip( "The size of each button of the submenu." )] [Range( 0.0f, 1.0f )]
	private float buttonSize = 0.25f;
	[SerializeField] [Tooltip( "The minimum range of calculating input on the submenu." )] [Range( 0.0f, 2.0f )]
	private float minRange = 1.0f;
	[SerializeField] [Tooltip( "The maximum range of calculating input on the submenu." )] [Range( 0.0f, 2.0f )]
	private float maxRange = 1.5f;
	[SerializeField] [Tooltip( "Determines whether or not the maximum range should be calculated as infinite." )]
	private bool infiniteMaxRange = false;
	[SerializeField] [Tooltip( "This determines the angle at which the submenu will lose focus and clear itself. A value of 0 will mean it will never lose focus regardless of how far away the input angle is." )] [Range( 0.0f, 45.0f )]
	private float submenuDeactivationAngle = 5.0f;
	float deactivationAngleRange = 0.0f;
	[SerializeField] [Tooltip( "Determines whether or not the submenu buttons should follow the rotation of the menu." )]
	private bool followOrbitalRotation = true;
	[SerializeField] [Tooltip( "Should the submenu buttons try to always be sequenced left to right? Disabling this option means the buttons will always sequence clockwise." )]
	private bool smartSequencing = true;
	[SerializeField] [Tooltip( "Should the submenu disable itself when the sub button is interacted with?" )]
	private bool disableOnInteract = false;
	[SerializeField] [Tooltip( "Determines how the submenu buttons must be interacted with for them to be called." )]
	private UltimateRadialMenuInputManager.InvokeAction invokeAction = UltimateRadialMenuInputManager.InvokeAction.OnButtonClick;
	[SerializeField] [Tooltip( "Determines if this component should set the sibling index of the submenu object or not, and what index to place it at in the hierarchy." )]
	private UltimateRadialMenu.SetSiblingIndex submenuSiblingIndex = UltimateRadialMenu.SetSiblingIndex.Disabled;
	int buttonIndexOnInputDown = -1;
	bool inputInRangeLastFrame = false;
	int submenuChildCount = 0;
	bool firstFrameSkipped = false;
	#endregion

	#region SUBMENU OPTIONS
	// SUBMENU OPTIONS //
	[SerializeField] [Tooltip( "The sprite to be applied to each sub button." )]
	private Sprite normalSprite;
	/// <summary>
	/// The normal sprite that is applied to each sub button. Assigning a new sprite here will change the sprites of each button on the menu.
	/// </summary>
	public Sprite NormalSprite
	{
		get
		{
			return normalSprite;
		}
		set
		{
			normalSprite = value;

			for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
			{
				if( UltimateRadialSubButtonList[ i ].buttonImage == null )
					continue;

				UltimateRadialSubButtonList[ i ].buttonImage.sprite = normalSprite;
			}

			for( int i = 0; i < UltimateRadialSubButtonPool.Count; i++ )
			{
				if( UltimateRadialSubButtonPool[ i ].buttonImage == null )
					continue;

				UltimateRadialSubButtonPool[ i ].buttonImage.sprite = normalSprite;
			}
		}
	}
	[SerializeField] [Tooltip( "The default color to apply to the sub button image." )]
	private Color normalColor = Color.white;
	/// <summary>
	/// The normal color that is applied to each sub button. Providing a new color to this property will change the color of each sub button.
	/// </summary>
	public Color NormalColor
	{
		get
		{
			return normalColor;
		}
		set
		{
			normalColor = value;

			for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
			{
				if( UltimateRadialSubButtonList[ i ].buttonImage == null )
					continue;

				UltimateRadialSubButtonList[ i ].buttonImage.color = normalColor;
			}

			for( int i = 0; i < UltimateRadialSubButtonPool.Count; i++ )
			{
				if( UltimateRadialSubButtonPool[ i ].buttonImage == null )
					continue;

				UltimateRadialSubButtonPool[ i ].buttonImage.color = normalColor;
			}
		}
	}
	// Radial Menu Interaction //
	[SerializeField] [Tooltip( "Should the current Ultimate Radial Menu button be selected when the input is on the submenu?" )]
	private bool selectRadialButton = true;
	[SerializeField] [Tooltip( "Should the submenu overwrite the radial menus text to display the current sub button information?" )]
	private bool overwriteRadialMenuText = true;
	// Menu Toggle //
	[Header( "Menu Toggle" )] [SerializeField] [Tooltip( "Determines if the submenu should toggle itself from enabled and disabled over time or not." )]
	private bool useMenuToggle = false;
	[SerializeField] [Tooltip( "Should the menu toggle using the canvas group alpha?" )]
	private bool menuToggleAlpha = true;
	[SerializeField] [Tooltip( "Should the menu toggle by scaling the sub buttons?" )]
	private bool menuToggleScale = false;
	[SerializeField] [Tooltip( "The time in seconds to enable the submenu." )]
	private float toggleInDuration = 0.25f;
	[SerializeField] [Tooltip( "The time in seconds to disable the submenu." )]
	private float toggleOutDuration = 0.1f;
	CanvasGroup canvasGroup;
	bool toggleIn = false, toggleOut = false;
	float toggleLerpValue = 0.0f;
	float toggleInSpeed = 0.0f, toggleOutSpeed = 0.0f;
	// Base Image //
	[Header( "Base Image" )] [SerializeField] [Tooltip( "Should an image be used as a base for the submenu buttons?" )]
	private bool useBaseImage = false;
	/// <summary>
	/// The image component of the base used for the submenu.
	/// </summary>
	public Image BaseImage
	{
		get
		{
			if( !useBaseImage || baseImage == null )
				return null;
			else
				return baseImage;
		}
	}
	[SerializeField] [Tooltip( "The image component to be used for the base of the submenu." )]
	private Image baseImage = null;
	[SerializeField] [Tooltip( "The size of the submenu base image." )] [Range( 0.5f, 2.0f )]
	private float baseImageSize = 1.25f;
	[SerializeField] [Tooltip( "Should this image fill to fit the amount of buttons currently on the submenu?" )]
	private bool baseImageUseFill = false;
	[SerializeField] [Tooltip( "Determines if any amount of angle should be added to the fill of the base image." )] [Range( -45.0f, 45.0f )]
	private float baseImageAngleModifier = 0.0f;
	// Pointer //
	/// <summary>
	/// The current visible state of the pointer image.
	/// </summary>
	public bool PointerActive
	{
		get;
		private set;
	}
	bool pointerFadeIn = false, pointerFadeOut = false;
	float pointerLerpValue = 0.0f;
	float pointerFadeInSpeed = 0.0f, pointerFadeOutSpeed = 0.0f;
	float pointerTargetSpeed = 5.0f;
	Quaternion pointerTargetRotation;
	[Header( "Pointer Settings" )] [SerializeField] [Tooltip( "Should an image be used to point to the current submenu button?" )]
	private bool usePointer = false;
	/// <summary>
	/// The image component of the pointer used for the submenu.
	/// </summary>
	public Image PointerImage
	{
		get
		{
			if( !usePointer || pointerImage == null )
				return null;
			else
				return pointerImage;
		}
	}
	[SerializeField] [Tooltip( "The image to be used for the pointer." )]
	private Image pointerImage = null;
	[SerializeField] [Tooltip( "The size of the pointer image." )] [Range( 0.0f, 2.0f )]
	private float pointerSize = 1.0f;
	[SerializeField] [Tooltip( "The time in seconds for the pointer to target the new button." )]
	private float pointerTargetTime = 0.25f;
	[SerializeField] [Tooltip( "Determines how the pointer will interact with the input and the buttons." )]
	private UltimateRadialMenu.PointerSnapOption pointerSnapOption = UltimateRadialMenu.PointerSnapOption.Smooth;
	[SerializeField] [Tooltip( "The rotation offset to apply to the pointer image." )]
	private float pointerRotationOffset = 90;
	[SerializeField] [Tooltip( "Determines if the pointer should change color or not." )]
	private bool pointerColorChange = false;
	[SerializeField] [Tooltip( "The time in seconds that the pointer should fade in. A value of 0 will make it fade in instantly." )]
	private float pointerFadeInDuration = 0.25f;
	[SerializeField] [Tooltip( "The time in seconds that the pointer should fade out. A value of 0 will make it fade out instantly." )]
	private float pointerFadeOutDuration = 0.5f;
	[SerializeField] [Tooltip( "The default color of the pointer image." )]
	private Color pointerNormalColor = Color.white;
	[SerializeField] [Tooltip( "The active color of the pointer image." )]
	private Color pointerActiveColor = Color.white;
	[SerializeField] [Tooltip( "Determines if this component should set the sibling index of the pointer object or not, and what index to place it at in the hierarchy." )]
	private UltimateRadialMenu.SetSiblingIndex pointerSiblingIndex = UltimateRadialMenu.SetSiblingIndex.Disabled;
	// Button Icon //
	[Header( "Button Icon Settings" )] [SerializeField] [Tooltip( "Determines if the sub buttons should have an icon associated with them or not." )]
	private bool useButtonIcon = false;
	[SerializeField] [Tooltip( "Determines the icon size in relation to the button." )] [Range( 0.0f, 1.0f )]
	private float iconSize = 0.25f;
	[SerializeField] [Tooltip( "Sets the default icon rotation." )]
	private float iconRotation = 0.0f;
	[SerializeField] [Tooltip( "Sets the horizontal position of the icon relative to the button." )] [Range( 0.0f, 100.0f )]
	private float iconHorizontalPosition = 50.0f;
	[SerializeField] [Tooltip( "Sets the vertical position of the icon relative to the button." )] [Range( 0.0f, 100.0f )]
	private float iconVerticalPosition = 50.0f;
	[SerializeField] [Tooltip( "Determines if the icons rotation should be local to the button or the radial menu as a whole." )]
	private bool iconLocalRotation = false;
	[SerializeField] [Tooltip( "Should the submenu attempt to rotate the icons to be as upright as possible to the player?" )]
	private bool iconSmartRotation = false;
	[SerializeField] [Tooltip( "The default color of the icon image." )]
	private Color iconNormalColor = Color.white;
	// Button Text //
	[Header( "Button Text Settings" )] [SerializeField] [Tooltip( "Determines if the sub buttons should have text associated with them or not." )]
	private bool useButtonText = false;
	[SerializeField] [Tooltip( "Determines the X and Y ratio of the text transform." )]
	private Vector2 textAreaRatio = Vector2.one;
	[SerializeField] [Tooltip( "The overall size of the text transform." )] [Range( 0.0f, 0.5f )]
	private float textSize = 0.25f;
	[SerializeField] [Tooltip( "The horizontal position of the text transform relative to the button." )] [Range( 0.0f, 100.0f )]
	private float textHorizontalPosition = 50.0f;
	[SerializeField] [Tooltip( "The vertical position of the text transform relative to the button." )] [Range( 0.0f, 100.0f )]
	private float textVerticalPosition = 50.0f;
	[SerializeField] [Tooltip( "Determines if the name of each button should be displayed on the button text." )]
	private bool displayNameOnButton = true;
	[SerializeField] [Tooltip( "Determines if the text will position itself according to the local position and rotation of the button." )]
	private bool textLocalPosition = true;
	[SerializeField] [Tooltip( "Determines if the text should follow the local rotation of the button or not." )]
	private bool textLocalRotation = true;
	[SerializeField] [Tooltip( "Should the text transform be vertical?" )]
	private bool textRotationVertical = false;
	[SerializeField] [Tooltip( "The normal default color of the text." )]
	private Color textNormalColor = Color.white;
	[SerializeField] [Tooltip( "Should the text position itself relative to the icon instead of the button transform?" )]
	private bool relativeToIcon = false;
	[SerializeField] [Tooltip( "The font to be used for each sub button text component." )]
	private Font buttonTextFont;
	[SerializeField] [Tooltip( "Determines if the text will have an outline or not." )]
	private bool buttonTextOutline = false;
	[SerializeField] [Tooltip( "The color of the text outline." )]
	private Color buttonTextOutlineColor = Color.white;
	// End Images //
	[Header( "End Images" )] [SerializeField] [Tooltip( "Determines if the submenu should use images at the ends of the buttons to show the start and end of the menu." )]
	private bool useEndImages = false;
	[SerializeField] [Tooltip( "The left image to be used for the end images." )]
	private Image endImageLeft = null;
	[SerializeField] [Tooltip( "The right image to be used for the end images." )]
	private Image endImageRight = null;
	[SerializeField] [Tooltip( "The angle modifier to apply from the first and last buttons in the list." )] [Range( -45.0f, 45.0f )]
	private float endImageAngleModifier = 5;
	[SerializeField] [Tooltip( "The overall size of the end images." )] [Range( 0.01f, 0.5f )]
	private float endImageSize = 0.15f;
	[SerializeField] [Tooltip( "Determines if any amount of distance should be added to the end images." )] [Range( -0.5f, 0.5f )]
	private float endImageDistanceModifier = 0.0f;
	#endregion

	#region BUTTON INTERACTION
	// BUTTON INTERACTION //
	[SerializeField] [Tooltip( "Determines whether or not the sub buttons will swap sprites when being interacted with or not." )]
	private bool spriteSwap = false;
	[SerializeField] [Tooltip( "Determines whether or not the sub buttons will change color when being interacted with or not." )]
	private bool colorChange = true;
	[SerializeField] [Tooltip( "Determines whether or not the sub buttons will scale when being interacted with or not." )]
	private bool scaleTransform = false;
	[SerializeField] [Tooltip( "Determines whether or not the icon will change color when being interacted with or not." )]
	private bool iconColorChange = false;
	[SerializeField] [Tooltip( "Determines whether or not the icon will scale when being interacted with or not." )]
	private bool iconScaleTransform = false;
	[SerializeField] [Tooltip( "Determines whether or not the text will change color when being interacted with or not." )]
	private bool textColorChange = false;
	// Highlighted //
	[SerializeField] [Tooltip( "The sprite to be applied to the sub button when highlighted." )]
	private Sprite highlightedSprite = null;
	[SerializeField] [Tooltip( "The color to be applied to the sub button when highlighted." )]
	private Color highlightedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the sub button transform when highlighted." )] [Range( 0.5f, 1.5f )]
	private float highlightedScaleModifier = 1.1f;
	[SerializeField] [Tooltip( "The position modifier for how much the sub button will expand from it's default position." )] [Range( -0.2f, 0.2f )]
	private float positionModifier = 0.0f;
	[SerializeField] [Tooltip( "The color to be applied to the icon when highlighted." )]
	private Color iconHighlightedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the icon transform when highlighted." )] [Range( 0.5f, 1.5f )]
	private float iconHighlightedScaleModifier = 1.1f;
	[SerializeField] [Tooltip( "The color to be applied to the text when highlighted." )]
	private Color textHighlightedColor = Color.white;
	// Pressed //
	[SerializeField] [Tooltip( "The sprite to be applied to the sub button when pressed." )]
	private Sprite pressedSprite = null;
	[SerializeField] [Tooltip( "The color to be applied to the sub button when pressed." )]
	private Color pressedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the sub button transform when pressed." )] [Range( 0.5f, 1.5f )]
	private float pressedScaleModifier = 1.05f;
	[SerializeField] [Tooltip( "The position modifier for how much the sub button will expand from it's default position." )] [Range( -0.2f, 0.2f )]
	private float pressedPositionModifier = 0.0f;
	[SerializeField] [Tooltip( "The color to be applied to the icon when pressed." )]
	private Color iconPressedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the icon transform when pressed." )] [Range( 0.5f, 1.5f )]
	private float iconPressedScaleModifier = 1.0f;
	[SerializeField] [Tooltip( "The color to be applied to the text when pressed." )]
	private Color textPressedColor = Color.white;
	// Selected //
	[SerializeField] [Tooltip( "Should the submenu select the button after it is interacted with?" )]
	private bool selectButtonOnInteract = false;
	[SerializeField] [Tooltip( "Should the submenu toggle the selection of the button if it is interacted with again?" )]
	private bool toggleSelection = false;
	[SerializeField] [Tooltip( "Should multiple buttons be allowed to be selected simultaneously?" )]
	private bool allowMultipleSelected = false;
	[SerializeField] [Tooltip( "The sprite to be applied to the sub button when it is selected." )]
	private Sprite selectedSprite = null;
	[SerializeField] [Tooltip( "The color to be applied to the sub button when selected." )]
	private Color selectedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the sub button transform when selected." )] [Range( 0.5f, 1.5f )]
	private float selectedScaleModifier = 1.0f;
	[SerializeField] [Tooltip( "The position modifier for how much the sub button will expand from it's default position." )] [Range( -0.2f, 0.2f )]
	private float selectedPositionModifier = 0.0f;
	[SerializeField] [Tooltip( "The color to be applied to the icon when selected." )]
	private Color iconSelectedColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the icon transform when selected." )] [Range( 0.5f, 1.5f )]
	private float iconSelectedScaleModifier = 1.0f;
	[SerializeField] [Tooltip( "The color to be applied to the text when selected." )]
	private Color textSelectedColor = Color.white;
	// Disabled //
	[SerializeField] [Tooltip( "The sprite to be applied to the sub button when it is disabled." )]
	private Sprite disabledSprite = null;
	[SerializeField] [Tooltip( "The color to be applied to the sub button when disabled." )]
	private Color disabledColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the sub button transform when disabled." )]
	private float disabledScaleModifier = 1.0f;
	[SerializeField] [Tooltip( "The position modifier for how much the sub button will expand from it's default position when disabled." )]
	private float disabledPositionModifier = 0.0f;
	[SerializeField] [Tooltip( "The color to be applied to the icon when disabled." )]
	private Color iconDisabledColor = Color.white;
	[SerializeField] [Tooltip( "The scale modifier to be applied to the icon transform when disabled." )]
	private float iconDisabledScaleModifier = 1.0f;
	[SerializeField] [Tooltip( "The color to be applied to the text when disabled." )]
	private Color textDisabledColor = Color.white;
	#endregion

	#region SUB BUTTON LIST
	// SUB BUTTON LIST //
	[Serializable]
	public class UltimateRadialSubButton
	{
		/// <summary>
		/// Returns the state of having information registered to this button.
		/// </summary>
		public bool Registered
		{
			get
			{
				// If this button has been officially registered, then return true.
				if( registered )
					return true;

				// If the Unity Event has been assigned, then return true.
				if( unityEvent != null && unityEvent.GetPersistentEventCount() > 0 )
					return true;

				// Otherwise return false since this button isn't registered.
				return false;
			}
		}
		bool registered = false;
		/// <summary>
		/// Returns the current state of being selected on the submenu.
		/// </summary>
		public bool Selected
		{
			get;
			private set;
		}

		// BASIC VARIABLES //
		public UltimateRadialSubmenu submenu;
		public RectTransform buttonTransform;
		public Image buttonImage;
		[Tooltip( "Determines if this button should be disabled or not." )]
		public bool buttonDisabled = false;
		[Tooltip( "The name of this button to display." )]
		public string name;
		[Tooltip( "The description of this button. This information can be displayed on the Ultimate Radial Menu text if the option is enabled." )]
		public string description;
		public int buttonIndex = -1;
		
		// INPUT VARIABLES //
		public float angle, angleRange;

		// ICON SETTINGS //
		public RectTransform iconTransform;
		[Tooltip( "The image component of the icon for this button." )]
		public Image icon;
		[Tooltip( "Determines if the icon should use positioning different than the default settings or not." )]
		public bool useIconUnique = false;
		[Tooltip( "The unique size of this button's icon." )] [Range( 0.0f, 1.0f )]
		public float iconSize = 0.0f;
		[Tooltip( "The unique position value of this button's icon." )] [Range( 0.0f, 100.0f )]
		public float iconHorizontalPosition = 0.0f, iconVerticalPosition = 0.0f;
		[Tooltip( "The unique rotation value to apply to this button's icon." )]
		public float iconRotation = 0.0f;
		[Tooltip( "Determines if this button's icon scale should be inverted on the Y axis." )]
		public bool invertScaleY = false;
		public Vector3 iconNormalScale;
		/// <summary>
		/// Returns the color of the icon or sets the icon color to the value provided.
		/// </summary>
		public Color iconColor
		{
			get
			{
				// If the icon is null...
				if( icon == null )
				{
					// If the submenu and the radial menu are assigned, then return the normal icon color.
					if( submenu != null && submenu.radialMenu != null )
						return submenu.radialMenu.iconNormalColor;

					// Otherwise, if the sub or radial menus are null, return white.
					return Color.white;
				}

				// Return the current icon color.
				return icon.color;
			}
			set
			{
				// If the icon is assigned then set the icon color.
				if( icon != null )
					icon.color = value;
			}
		}

		// TEXT SETTINGS //
		[Tooltip( "The text component for this button's display text." )]
		public Text text;

		// TRANSFORM INTERACTION SETTINGS //
		public Vector3 normalPosition, highlightedPosition, pressedPosition, selectedPosition, disabledPosition;

		// BASIC CALLBACK INFORMATION //
		public string key;
		public int id;

		// CALLBACKS //
		event Action OnButtonInteract;
		event Action<int> OnButtonInteractWithId;
		event Action<string> OnButtonInteractWithKey;
		event Action OnClearButtonInformation;
		event Action<bool> OnSelectedStateChanged;
		[SerializeField]
		private UnityEvent unityEvent;
		

		/// <summary>
		/// Returns true if the angle is within the range of this button.
		/// </summary>
		/// <param name="angle">The current input angle.</param>
		public bool IsInAngle ( float inputAngle )
		{
			// If this button is disabled then return false.
			if( buttonDisabled )
				return false;

			// If the angle is within this buttons range, then return true.
			if( Mathf.Abs( inputAngle - angle ) <= angleRange || Mathf.Abs( ( inputAngle - 360f ) - angle ) <= angleRange || Mathf.Abs( inputAngle - ( angle - 360f ) ) <= angleRange )
				return true;

			// Else return false.
			return false;
		}

		/// <summary>
		/// Invokes the functionality for when the input enters the button.
		/// </summary>
		public void OnEnter ()
		{
			// If this button is disabled then return.
			if( buttonDisabled )
				return;
			
			// If this button is selected, then just return since it is already in the selected state.
			if( Selected )
				return;

			// If the user wants to swap sprites when the button is highlighted...
			if( submenu.spriteSwap )
			{
				// If the highlighted sprite is assigned, then apply that sprite to the image.
				if( submenu.highlightedSprite != null )
					buttonImage.sprite = submenu.highlightedSprite;
				// Else the highlighted sprite is null, so apply the normal sprite to the image.
				else
					buttonImage.sprite = submenu.normalSprite;
			}

			// If the user wants to change the color of the button, then apply the highlighted color.
			if( submenu.colorChange && buttonImage.sprite != null )
				buttonImage.color = submenu.highlightedColor;

			// If the user wants to scale the transform when the button is being hovered over...
			if( submenu.scaleTransform )
			{
				// Modify the scale and position of the button.
				buttonTransform.localScale = Vector3.one * submenu.highlightedScaleModifier;
				buttonTransform.localPosition = highlightedPosition;
			}

			// If the user wants to use the icon and it is assigned...
			if( submenu.useButtonIcon && icon != null && icon.sprite != null )
			{
				// If the user wants to change color, then apply the highlighted color.
				if( submenu.iconColorChange )
					icon.color = submenu.iconHighlightedColor;

				// If the user wants to scale the transform of the icon, then apply the highlighted scale.
				if( submenu.iconScaleTransform )
					iconTransform.localScale = iconNormalScale * submenu.iconHighlightedScaleModifier;
			}

			// If the user wants to use text and color change and it is assigned, then apply the highlighted color.
			if( submenu.useButtonText && text != null && submenu.textColorChange )
				text.color = submenu.textHighlightedColor;
		}

		/// <summary>
		/// Invokes the functionality for when the input exits the button.
		/// </summary>
		public void OnExit ()
		{
			// If this button is disabled then return...
			if( buttonDisabled )
				return;

			// If the application is playing this button is in the selected state, then return.
			if( Application.isPlaying && Selected )
				return;

			// If the user wants to swap sprites, then apply the normal sprite to the image.
			if( submenu.spriteSwap && submenu.normalSprite != null )
				buttonImage.sprite = submenu.normalSprite;

			// If the user wants to change the color of the image, then apply the normal color.
			if( submenu.colorChange && buttonImage.sprite != null )
				buttonImage.color = submenu.normalColor;

			// If the user wants to scale the transform, then apply the normal scale and position.
			if( submenu.scaleTransform )
			{
				buttonImage.GetComponent<RectTransform>().localScale = Vector3.one;
				buttonImage.GetComponent<RectTransform>().localPosition = normalPosition;
			}

			// If the user wants to use the icon and it is assigned...
			if( submenu.useButtonIcon && icon != null && icon.sprite != null )
			{
				// If the user wants to change color, then apply the normal color.
				if( submenu.iconColorChange )
					icon.color = submenu.iconNormalColor;

				// If the user wants to scale the transform of the icon, then apply the normal scale.
				if( submenu.iconScaleTransform )
					iconTransform.localScale = iconNormalScale;
			}

			// If the user wants to use text color change and it is assigned, then apply the color.
			if( submenu.useButtonText && text != null && submenu.textColorChange )
				text.color = submenu.textNormalColor;
		}

		/// <summary>
		/// Invokes the functionality for when the input is down on the button.
		/// </summary>
		public void OnInputDown ()
		{
			// If the user wants to swap sprites and the pressed sprite isn't null, then apply the sprite.
			if( submenu.spriteSwap && submenu.pressedSprite != null )
				buttonImage.sprite = submenu.pressedSprite;

			// If the user wants to change the color, then apply the pressed color to the image.
			if( submenu.colorChange && buttonImage.sprite != null )
				buttonImage.color = submenu.pressedColor;

			// If the user wants to scale the transform...
			if( submenu.scaleTransform )
			{
				// Then apply the scale modifier and position.
				buttonImage.GetComponent<RectTransform>().localScale = Vector3.one * submenu.pressedScaleModifier;
				buttonImage.GetComponent<RectTransform>().localPosition = pressedPosition;
			}

			// If the user wants to use the icon and it is assigned...
			if( submenu.useButtonIcon && icon != null && icon.sprite != null )
			{
				// If the user wants to change color, then apply the pressed color.
				if( submenu.iconColorChange )
					icon.color = submenu.iconPressedColor;

				// If the user wants to scale the transform of the icon, then apply the normal scale multiplied by the pressed mod.
				if( submenu.iconScaleTransform )
					iconTransform.localScale = iconNormalScale * submenu.iconPressedScaleModifier;
			}

			// If the user wants to use text color change and it is assigned then apply the pressed color
			if( submenu.useButtonText && text != null && submenu.textColorChange )
				text.color = submenu.textPressedColor;
		}

		/// <summary>
		/// Invokes the functionality for when the input is release on the button.
		/// </summary>
		public void OnInputUp ()
		{
			// If the button is disabled, then return.
			if( buttonDisabled )
				return;

			// If this button is still the currently selected button, then return to the OnEnter() function.
			if( buttonIndex == submenu.CurrentButtonIndex )
				OnEnter();
			// Else call the OnExit() function because the input has left this button.
			else
				OnExit();
		}

		/// <summary>
		/// Invokes the functionality for when the input interacts with the button.
		/// </summary>
		public void OnInteract ()
		{
			// If this button is disabled, then return.
			if( buttonDisabled )
				return;

			// If the user wants to select the button when interacted with...
			if( submenu.selectButtonOnInteract )
			{
				// If the user wants to toggle the selected state and this button is currently selected, then call the Deselect function.
				if( submenu.toggleSelection && Selected )
					OnDeselect();
				// Else just select this button.
				else
					OnSelect();
			}

			// If the user has assigned a unity event to call, then call it.
			if( unityEvent != null )
				unityEvent.Invoke();

			// If the user has subscribed to the default callback then invoke it.
			OnButtonInteract?.Invoke();

			// If the user has subscribed to the ID callback then invoke it with the assigned integer ID.
			OnButtonInteractWithId?.Invoke( id );

			// If the user has subscribed to the string key callback then invoke it with the assigned key.
			OnButtonInteractWithKey?.Invoke( key );
			
			// Inform any subscribers that this button has been interacted with.
			submenu.OnButtonInteract?.Invoke( buttonIndex );
		}

		/// <summary>
		/// Invokes the functionality for when this button is selected.
		/// </summary>
		public void OnSelect ()
		{
			// If the button is disabled, then return.
			if( buttonDisabled )
				return;
			
			// Set Selected to true for reference.
			Selected = true;

			// If the user only wants one button selected at a time...
			if( submenu.selectButtonOnInteract && !submenu.allowMultipleSelected )
			{
				// Loop through all the submenu buttons...
				for( int i = 0; i < submenu.UltimateRadialSubButtonList.Count; i++ )
				{
					// If the sub button in the list is this, then continue.
					if( submenu.UltimateRadialSubButtonList[ i ] == this )
						continue;

					// If the button is selected, deselect it.
					if( submenu.UltimateRadialSubButtonList[ i ].Selected )
						submenu.UltimateRadialSubButtonList[ i ].OnDeselect();
				}
			}

			// If the user wants to swap sprites and the sprite is assigned, then assign it to the button.
			if( submenu.spriteSwap && submenu.selectedSprite != null )
				buttonImage.sprite = submenu.selectedSprite;

			// If the user wants to change the color of the button, then apply the selected color.
			if( submenu.colorChange && buttonImage.sprite != null )
				buttonImage.color = submenu.selectedColor;

			// If the user wants to scale the transform, then apply the scale and position.
			if( submenu.scaleTransform )
			{
				buttonTransform.localScale = Vector3.one * submenu.selectedScaleModifier;
				buttonTransform.localPosition = selectedPosition;
			}

			// If the user wants to use the icon and it is assigned...
			if( submenu.useButtonIcon && icon != null && icon.sprite != null )
			{
				// If the user wants to change color, then apply the selected color.
				if( submenu.iconColorChange )
					icon.color = submenu.iconSelectedColor;

				// If the user wants to scale the transform of the icon...
				if( submenu.iconScaleTransform )
					iconTransform.localScale = iconNormalScale * submenu.iconSelectedScaleModifier;
			}

			// If the user wants to use text color change and it is assigned, then apply the color.
			if( submenu.useButtonText && text != null && submenu.textColorChange )
				text.color = submenu.textSelectedColor;

			// Inform any subscribers that this button has been selected.
			submenu.OnButtonSelected?.Invoke( buttonIndex );

			// Inform any subscribers with the selected state change. This is used to keep the ButtonInfo up to date with this button.
			OnSelectedStateChanged?.Invoke( Selected );
		}

		/// <summary>
		/// Invokes the functionality for when this button is not selected anymore.
		/// </summary>
		public void OnDeselect ()
		{
			// If the button is disabled, then return.
			if( buttonDisabled )
				return;

			// Set Selected to false for reference.
			Selected = false;

			// Call OnExit to reset the button.
			OnExit();

			// If the input is within this button, enter the button so it doesn't have to wait a frame to calculate.
			if( IsInAngle( submenu.CurrentInputAngle ) && submenu.CurrentInputDistance < submenu.CalculatedMaxRange && submenu.CurrentInputDistance > submenu.CalculatedMinRange )
				OnEnter();

			// Inform any button info registered to this button that this button has been deselected.
			OnSelectedStateChanged?.Invoke( Selected );
		}
		
		/// <summary>
		/// Invokes the functionality for when button is enabled.
		/// </summary>
		public void OnEnable ()
		{
			// If the button is already enabled then return.
			if( !buttonDisabled )
				return;

			// Set the disable button to false so that calculations can continue on this button.
			buttonDisabled = false;

			// Call OnExit to reset the button.
			OnExit();
		}

		/// <summary>
		/// Invokes the functionality for when button is disabled.
		/// </summary>
		public void OnDisable ()
		{
			// If the button is already disabled then return.
			if( buttonDisabled )
				return;

			// Set the disable button to true so that nothing will be calculated on this button.
			buttonDisabled = true;

			// Set selected to false since the button is not disabled.
			Selected = false;

			// If the user wants to use a disabled sprite then apply the sprite.
			if( submenu.spriteSwap && submenu.disabledSprite != null )
				buttonImage.sprite = submenu.disabledSprite;

			// If the use wants to change the color, then apply the color.
			if( submenu.colorChange && buttonImage.sprite != null )
				buttonImage.color = submenu.disabledColor;

			// If the user is scaling the transform, then reset the scale and position.
			if( submenu.scaleTransform )
			{
				buttonImage.GetComponent<RectTransform>().localScale = Vector3.one * submenu.disabledScaleModifier;
				buttonImage.GetComponent<RectTransform>().localPosition = disabledPosition;
			}

			// If the user wants to use the icon and it is assigned...
			if( submenu.useButtonIcon && icon != null && icon.sprite != null )
			{
				// If the user wants to change color, then apply the disabled color.
				if( submenu.iconColorChange )
					icon.color = submenu.iconDisabledColor;

				// If the user wants to scale the transform of the icon, then apply the normal scale.
				if( submenu.iconScaleTransform )
					iconTransform.localScale = iconNormalScale * submenu.iconDisabledScaleModifier;
			}

			// If the user wants to use text, it is assigned, and the user wants to change the color, then apply the color.
			if( submenu.useButtonText && text != null && submenu.textColorChange )
				text.color = submenu.textDisabledColor;
		}

		/// <summary>
		/// Updates the icon image with the new sprite.
		/// </summary>
		/// <param name="newIcon">The new icon sprite to use.</param>
		public void UpdateIcon ( Sprite newIcon )
		{
			// If the icon is unassigned, log error and return.
			if( icon == null )
			{
				Debug.LogError( FormatDebug( "The button's icon image component is not assigned", "Please make sure that the submenu has the <b>Button Icon</b> option enabled", submenu.gameObject.name ) );
				return;
			}

			// Assign the sprite to the provided sprite.
			icon.sprite = newIcon;

			// If the sprite is null, then change the color to clear to make it invisible.
			if( icon.sprite == null )
				icon.color = Color.clear;
			// Else the sprite is assigned, so apply the normal icon color.
			else
				icon.color = submenu.iconNormalColor;
		}

		/// <summary>
		/// Updates the text associated with this button to display the provided string.
		/// </summary>
		/// <param name="newText">The new string to apply to the text.</param>
		public void UpdateText ( string newText )
		{
			// If the text component is unassigned, then log error and return.
			if( text == null )
			{
				Debug.LogError( FormatDebug( "The sub button's text component is not assigned", "Please make sure that the submenu has the <b>Button Text</b> option enabled", submenu.gameObject.name ) );
				return;
			}

			// Assign the text string.
			text.text = newText;
		}

		/// <summary>
		/// Updates the name of this sub button. Additionally, updates the display of anything that uses the name for text.
		/// </summary>
		/// <param name="newName">The new name to apply to this button.</param>
		public void UpdateName ( string newName )
		{
			// Assign the new name.
			name = newName;
			
			// If the radial button is set to display the name and the text component is assigned, then apply the name.
			if( submenu.displayNameOnButton && text != null )
				text.text = name;

			// If the user has center text displaying the name of the button, then refresh the name text if this button is currently selected.
			if( submenu.overwriteRadialMenuText && submenu.radialMenu.displayButtonName && submenu.radialMenu.nameText != null && submenu.CurrentButtonIndex == buttonIndex )
				submenu.radialMenu.nameText.text = !buttonDisabled ? name : "";
		}

		/// <summary>
		/// Updates the description of this button and updates the needed text if it is the current button.
		/// </summary>
		/// <param name="newDescription">The new description to apply to this button.</param>
		public void UpdateDescription ( string newDescription )
		{
			// Assign the new description.
			description = newDescription;
			
			// If the user has center text displaying the name of the button, then refresh the name text if this button is currently selected.
			if( submenu.overwriteRadialMenuText && submenu.radialMenu.displayButtonDescription && submenu.radialMenu.descriptionText != null && submenu.CurrentButtonIndex == buttonIndex )
				submenu.radialMenu.descriptionText.text = !buttonDisabled ? description : "";
		}

		/// <summary>
		/// Subscribes the provided function to the button interaction event.
		/// </summary>
		/// <param name="ButtonCallback">The action callback to call when this new button is being interacted with.</param>
		public void AddCallback ( Action ButtonCallback )
		{
			OnButtonInteract += ButtonCallback;
		}

		/// <summary>
		/// Subscribes the provided function to the button interaction event.
		/// </summary>
		/// <param name="ButtonCallback">The action callback to call when this new button is being interacted with.</param>
		public void AddCallback ( Action<int> ButtonCallback )
		{
			OnButtonInteractWithId += ButtonCallback;
		}

		/// <summary>
		/// Subscribes the provided function to the button interaction event.
		/// </summary>
		/// <param name="ButtonCallback">The action callback to call when this new button is being interacted with.</param>
		public void AddCallback ( Action<string> ButtonCallback )
		{
			OnButtonInteractWithKey += ButtonCallback;
		}

		/// <summary>
		/// [Internal] Registers the information to the button.
		/// </summary>
		/// <param name="ButtonCallback">The Callback function to call when interacted with.</param>
		/// <param name="buttonInfo">The information to apply to the button.</param>
		public void RegisterButtonInfo ( UltimateRadialSubButtonInfo buttonInfo )
		{
			// Set registered to true since we are assigning the information.
			registered = true;

			// Assign this sub button to the buttonInfo so that it can reference this button.
			buttonInfo.subButton = this;

			// Register the button info values.
			id = buttonInfo.id;
			key = buttonInfo.key;
			name = buttonInfo.name;
			description = buttonInfo.description;

			// If the icon image is assigned and the user wants to show the icon...
			if( icon != null && submenu.useButtonIcon )
			{
				// If the provided icon sprite is assigned, then assign the sprite and set the color.
				if( buttonInfo.icon != null )
				{
					icon.sprite = buttonInfo.icon;
					icon.color = submenu.iconNormalColor;
				}
				// Else just set the color of the icon image to clear.
				else
					icon.color = Color.clear;
			}

			// If the text isn't null then assign the radial info text.
			if( text != null && submenu.displayNameOnButton )
				text.text = buttonInfo.name;
			
			// If the supplied button info is selected, then select this button.
			if( buttonInfo.Selected )
				OnSelect();

			// Subscribe the buttonInfo clear function so that it can be notified when this button is cleared.
			OnSelectedStateChanged += buttonInfo.OnSelectedStateChanged;
			OnClearButtonInformation += buttonInfo.OnClearButtonInformation;
		}

		/// <summary>
		/// Clears the button information.
		/// </summary>
		public void ClearButtonInformation ()
		{
			// Set registered and disabled to false since the button is being cleared.
			registered = false;
			buttonDisabled = false;

			// Reset the key and id values.
			key = "";
			id = -1;
			name = "";
			description = "";
			Selected = false;

			// Reset the sprite.
			if( submenu.spriteSwap && submenu.normalSprite != null )
				buttonImage.sprite = submenu.normalSprite;

			// Reset the button color.
			if( buttonImage.sprite != null )
				buttonImage.color = submenu.normalColor;

			// Reset the scale.
			if( submenu.scaleTransform )
				buttonTransform.localScale = Vector2.one;

			// Reset the button icon.
			if( icon != null )
			{
				icon.sprite = null;
				icon.color = Color.clear;
			}

			// If the button was applying unique positioning to the icon, then reset it so that it doesn't effect the next button information.
			if( useIconUnique )
				useIconUnique = false;

			// If the text component is assigned, then reset the text.
			if( text != null )
				text.text = "";

			// Notify any button information subscribers that this button has been cleared.
			OnClearButtonInformation?.Invoke();

			// Reset callbacks.
			OnButtonInteract = null;
			OnButtonInteractWithId = null;
			OnButtonInteractWithKey = null;
			unityEvent = null;
			OnClearButtonInformation = null;
			OnSelectedStateChanged = null;
		}
	}
	public List<UltimateRadialSubButton> UltimateRadialSubButtonList = new List<UltimateRadialSubButton>();
	List<UltimateRadialSubButton> UltimateRadialSubButtonPool = new List<UltimateRadialSubButton>();

	// STATIC BUTTON INFORMATION //
	[SerializeField] [Tooltip( "Should this submenu use static button information that is assigned in the editor?" )]
	private bool useStaticInformation = false;
	[Serializable]
	public class SubmenuInformation
	{
		public UltimateRadialSubmenu submenu;
		public UltimateRadialButtonInfo radialMenuButtonInfo;
		//[SerializeField]
		public bool registerSubmenu = true;

		[Serializable]
		public class SubButtonInformation
		{
			// BUTTON INFO //
			public UltimateRadialSubButtonInfo subButtonInfo;

			// BASIC VARIABLES //
			public bool buttonDisabled = false;

			// ICON SETTINGS //
			public bool useIconUnique = false;
			public float iconSize = 0.0f;
			public float iconHorizontalPosition = 0.0f, iconVerticalPosition = 0.0f;
			public float iconRotation = 0.0f;
			public bool invertScaleY = false;

			public UnityEvent unityEvent;

			/// <summary>
			/// [INTERNAL] Called when the player interacts with the static sub button.
			/// </summary>
			public void OnInteract ()
			{
				// If the user has assigned a unity event to call, then call it.
				if( unityEvent != null )
					unityEvent.Invoke();
			}
		}
		public List<SubButtonInformation> SubButtonInformations = new List<SubButtonInformation>();
		
		/// <summary>
		/// [INTERNAL] This function is called when the user wants to have static sub button information displayed.
		/// </summary>
		public void PopulateSubmenu ()
		{
			// If the current radial button index is the same as the one that was stored when this submenu was sent to the radial menu, then return.
			if( submenu.CurrentRadialButton != null && radialMenuButtonInfo.GetButtonIndex >= 0 && submenu.CurrentRadialButton.buttonIndex == radialMenuButtonInfo.GetButtonIndex )
				return;
			
			// Clear the submenu.
			submenu.ClearMenu();

			// Loop through all static button information...
			for( int i = 0; i < SubButtonInformations.Count; i++ )
			{
				// Register the sub button information to the submenu.
				submenu.RegisterButton( SubButtonInformations[ i ].OnInteract, SubButtonInformations[ i ].subButtonInfo );

				// If this button will be disabled, disable it.
				if( SubButtonInformations[ i ].buttonDisabled )
					SubButtonInformations[ i ].subButtonInfo.subButton.OnDisable();

				// Copy any custom icon values.
				SubButtonInformations[ i ].subButtonInfo.subButton.useIconUnique = SubButtonInformations[ i ].useIconUnique;
				SubButtonInformations[ i ].subButtonInfo.subButton.iconSize = SubButtonInformations[ i ].iconSize;
				SubButtonInformations[ i ].subButtonInfo.subButton.iconHorizontalPosition = SubButtonInformations[ i ].iconHorizontalPosition;
				SubButtonInformations[ i ].subButtonInfo.subButton.iconVerticalPosition = SubButtonInformations[ i ].iconVerticalPosition;
				SubButtonInformations[ i ].subButtonInfo.subButton.invertScaleY = SubButtonInformations[ i ].invertScaleY;
			}

			// Enable the submenu.
			submenu.Enable();
		}
	}
	public List<SubmenuInformation> SubmenuInformations = new List<SubmenuInformation>();
	#endregion

	#region SCRIPT REFERENCE AND CALLBACKS
	// SCRIPT REFERENCE //
	static Dictionary<string, UltimateRadialSubmenu> UltimateRadialSubmenus = new Dictionary<string, UltimateRadialSubmenu>();

	// ACTION SUBSCRIPTIONS //
	/// <summary>
	/// Called when a button has been entered.
	/// </summary>
	public event Action<int> OnButtonEnter;
	/// <summary>
	/// Called when a button has been exited.
	/// </summary>
	public event Action<int> OnButtonExit;
	/// <summary>
	/// Called when the input has been pressed on a button.
	/// </summary>
	public event Action<int> OnButtonInputDown;
	/// <summary>
	/// Called when the input has been released on a button.
	/// </summary>
	public event Action<int> OnButtonInputUp;
	/// <summary>
	/// Called when a button has been interacted with.
	/// </summary>
	public event Action<int> OnButtonInteract;
	/// <summary>
	/// Called when a button has been selected.
	/// </summary>
	public event Action<int> OnButtonSelected;
	/// <summary>
	/// Called when the submenu gains focus.
	/// </summary>
	public event Action OnMenuFocused;
	/// <summary>
	/// Called when the submenu has lost focus.
	/// </summary>
	public event Action OnMenuLostFocus;
	/// <summary>
	/// Called when the submenu has been enabled.
	/// </summary>
	public event Action OnMenuEnabled;
	/// <summary>
	/// Called when the submenu has been disabled.
	/// </summary>
	public event Action OnMenuDisabled;
	/// <summary>
	/// Called when the submenu's positioning has been updated.
	/// </summary>
	public event Action OnUpdatePositioning;
	/// <summary>
	/// This callback relays the information that is provided from the Ultimate Radial Menu Input Manager script according to the users settings.
	/// </summary>
	public event Action<Vector2, float, bool, bool> OnProcessInput;
	/// <summary>
	/// Called while the button is being hovered. The second parameter is the time in seconds that the player has hovered the button.
	/// </summary>
	public event Action<int, float> OnButtonHover;
	float buttonHoverTime = 0.0f;

#if UNITY_EDITOR
#pragma warning disable CS0414
	// EDITOR //
	[SerializeField] private int EditorRadialMenuIndex = 0;
	[SerializeField] private Sprite IconPlaceholderSprite = null;
	#pragma warning restore CS0414
	#endif
	#endregion
	#pragma warning restore IDE0044

	void Awake ()
	{
		// If the application is not playing, then return.
		if( !Application.isPlaying )
			return;

		// If there are basic errors present...
		if( HasBasicErrors() )
		{
			// Disable this component, and return.
			enabled = false;
			return;
		}

		// If the name for the radial menu that this is associated with is assigned...
		if( radialMenu.radialMenuName != string.Empty )
		{
			// Check to see if the dictionary already contains this name, and if so, remove the current one.
			if( UltimateRadialSubmenus.ContainsKey( radialMenu.radialMenuName ) )
				UltimateRadialSubmenus.Remove( radialMenu.radialMenuName );

			// Register this UltimateRadialSubmenu into the dictionary.
			UltimateRadialSubmenus.Add( radialMenu.radialMenuName, GetComponent<UltimateRadialSubmenu>() );
		}

		// Assign the canvas group component.
		canvasGroup = GetComponent<CanvasGroup>();

		// If this object doesn't have a CanvasGroup component attached, then add it.
		if( canvasGroup == null )
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		// Set the canvas group alpha to 0 by default so that it is invisible.
		canvasGroup.alpha = 0.0f;

		// Reset the stored values. 
		CurrentButtonIndex = -1;
		buttonIndexOnInputDown = -1;
		CurrentRadialButton = null;

		// Store the base transform component.
		BaseTransform = GetComponent<RectTransform>();

		// Loop through all the current sub buttons and make sure that their submenu property is assigned.
		for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
			UltimateRadialSubButtonList[ i ].submenu = this;

		// Clear the menu by default.
		ClearMenu();

		// If there is any stored static button information then register the information.
		if( useStaticInformation && SubmenuInformations.Count > 0 )
			RegisterStaticInformation();
	}

	void Start ()
	{
		// If the game is running, then return.
		if( !Application.isPlaying )
			return;

		// If there are basic errors present...
		if( HasBasicErrors() )
		{
			// Disable this component, and return.
			enabled = false;
			return;
		}

#if UNITY_EDITOR
		// If the user has the Enter Play Mode Options enabled, it will not run the Awake() for scripts that use the [ExecuteOnEditMode]. So run Awake() if the user isn't using the Scene Reload option.
		if( UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag( UnityEditor.EnterPlayModeOptions.DisableSceneReload ) )
			Awake();
#endif

		// If the user wants to toggle the menu over time, calculate the speeds.
		if( useMenuToggle )
		{
			toggleInSpeed = 1.0f / toggleInDuration;
			toggleOutSpeed = 1.0f / toggleOutDuration;
		}

		// If the user wants to use a pointer image...
		if( usePointer )
		{
			// Calculate the targeting speed.
			pointerTargetSpeed = 1.0f / pointerTargetTime;

			// If the user wants to toggle it's state over time, calculate the speeds.
			if( pointerColorChange )
			{
				pointerFadeInSpeed = 1.0f / pointerFadeInDuration;
				pointerFadeOutSpeed = 1.0f / pointerFadeOutDuration;
			}
		}

		// Subscribe to the callbacks of the radial menu.
		radialMenu.OnMenuDisabled += UltimateRadialMenu_OnMenuDisabled;
		radialMenu.OnButtonEnter += UltimateRadialMenu_OnButtonEnter;
		radialMenu.OnProcessInput += ProcessInput;
		radialMenu.OnUpdatePositioning += UpdatePositioning;
		radialMenu.OnButtonCountModified += UltimateRadialMenu_OnButtonCountModified;

		// Call the OnRadialMenuButtonCountModified callback right away so that this submenu will be at the right child index.
		UltimateRadialMenu_OnButtonCountModified( 0 );
	}
	
	// RADIAL MENU CALLBACKS //
	/// <summary>
	/// [INTERNAL] This function is subscribed to the OnRadialMenuDisabled callback of the Ultimate Radial Menu.
	/// </summary>
	void UltimateRadialMenu_OnMenuDisabled ()
	{
		// Disable the submenu since the radial menu has been disabled.
		Disable();
	}

	/// <summary>
	/// [INTERNAL] This function is subscribed to the OnRadialButtonEnter callback of the Ultimate Radial Menu.
	/// </summary>
	void UltimateRadialMenu_OnButtonEnter ( int buttonIndex )
	{
		// If the submenu is enabled, and the button index is different than what is stored, disable the submenu.
		if( UltimateRadialSubButtonList.Count > 0 && CurrentRadialButton != null && CurrentRadialButton.buttonIndex != radialMenu.CurrentButtonIndex )
			Disable();
	}

	/// <summary>
	/// [INTERNAL] Subscribed to the OnRadialMenuButtonCountModified function. 
	/// </summary>
	private void UltimateRadialMenu_OnButtonCountModified ( int buttonCount )
	{
		// If the sibling index option is disabled, then return.
		if( submenuSiblingIndex == UltimateRadialMenu.SetSiblingIndex.Disabled )
			return;

		// If the user wants the submenu to be the last sibling, then set this as last sibling.
		if( submenuSiblingIndex == UltimateRadialMenu.SetSiblingIndex.Last )
			transform.SetAsLastSibling();
		// Else if the user wants the submenu to be the first sibling and this transform is NOT currently the first index, then set it as first sibling.
		else if( submenuSiblingIndex == UltimateRadialMenu.SetSiblingIndex.First && transform.GetSiblingIndex() > 0 )
			transform.SetAsFirstSibling();
	}
	// END RADIAL MENU CALLBACKS //

#if UNITY_EDITOR
	void Update ()
	{
		// If the application is not playing (in edit mode of Unity) update the positioning.
		if( !Application.isPlaying )
			UpdatePositioning();
	}

	void OnTransformParentChanged ()
	{
		// If the radial menu of the transform parent is null, then return...
		if( radialMenu == null )
			return;

		// If the radial menu transform is not the same as this new parent transform...
		if( radialMenu.transform != transform.parent )
		{
			// Attempt to find a radial menu in the parent of this transform.
			radialMenu = transform.GetComponentInParent<UltimateRadialMenu>();

			// If the radial menu is null then return.
			if( radialMenu == null )
				return;
		}
	}

	void Reset ()
	{
		// If this game object does not have a RectTransform component, then replace the normal Transform component with a RectTranform.
		if( !GetComponent<RectTransform>() )
			gameObject.AddComponent<RectTransform>();
	}
#endif

	/// <summary>
	/// [INTERNAL] This function is subscribed to the OnProcessInput from the main Ultimate Radial Menu script.
	/// </summary>
	/// <param name="input">The raw input value.</param>
	/// <param name="distance">The distance of the input.</param>
	/// <param name="inputDown">The state of the input being pressed down this frame.</param>
	/// <param name="inputUp">The state of the input being released this frame.</param>
	public void ProcessInput ( Vector2 input, float distance, bool inputDown, bool inputUp )
	{
		// If the sub button count is 0, then return since the list is empty.
		if( UltimateRadialSubButtonList.Count == 0 )
			return;

		// If the submenu is inactive or it's not interactable...
		if( !IsEnabled || !Interactable )
		{
			// Process the pointer and the submenu toggle, then return.
			ProcessPointer();
			ProcessToggle();
			return;
		}

		// If the first frame of input calculations has not been skipped, then it could actually invoke the submenu button from the same input action as the radial menu, so set firstFrameSkipped to true and return to skip this frame.
		if( !firstFrameSkipped )
		{
			firstFrameSkipped = true;
			return;
		}

		// If the current radial button was not stored properly, then inform the user.
		if( CurrentRadialButton == null )
			Debug.LogError( FormatDebug( "The submenu was not able to store the current button of the Ultimate Radial Menu. Did you call the Enable() function of the submenu from somewhere other than the Ultimate Radial Menu button interaction?", "Please ensure that you're enabling the submenu from interacting with the main Ultimate Radial Menu", gameObject.name ) );

		// Set the InputInRange bool to false right away for recalculating input this frame.
		InputInRange = false;
		
		// Calculate the angle of the input and convert it to degrees.
		float angle = Mathf.Atan2( input.y, input.x ) * Mathf.Rad2Deg;

		// If the angle is negative, then add 360 to make it positive.
		if( angle < 0 )
			angle += 360;

		// Store the calculated angle so that other scripts can get it.
		CurrentInputAngle = angle;
		CurrentInputDistance = distance;

		// If the current input device is controller...
		if( radialMenu.inputManager.CurrentInputDevice == UltimateRadialMenuInputManager.InputDevice.Controller )
		{
			// If the input value is actually assigned, then set the distance value to being between the calculated distances.
			if( input != Vector2.zero )
				distance = Mathf.Lerp( CalculatedMinRange, CalculatedMaxRange, 0.5f );
		}
		
		// Loop through all the submenu buttons...
		for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
		{
			// If the distance of the input exceeds the boundaries of the submenu...
			if( distance < CalculatedMinRange || distance > CalculatedMaxRange )
			{
				// If the input was in range the last frame...
				if( inputInRangeLastFrame )
				{
					// Reset the submenu.
					ResetSubmenu();
					
					// Notify any subscriptions that the submenu has lost focus.
					OnMenuLostFocus?.Invoke();
				}

				// Break the loop.
				break;
			}

			// If there was a current button that had input on it, and the input is still within the angle of that button, then set InputInRange to true and break the loop since nothing needs to change.
			if( CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count && UltimateRadialSubButtonList[ CurrentButtonIndex ].IsInAngle( angle ) )
			{
				InputInRange = true;

				// Increase the current hover time to send with the callback.
				buttonHoverTime += Time.deltaTime;
				OnButtonHover?.Invoke( CurrentButtonIndex, buttonHoverTime );

				break;
			}

			// If the angle is within the range of the current submenu button...
			if( UltimateRadialSubButtonList[ i ].IsInAngle( angle ) )
			{
				// Set the InputInRange to true.
				InputInRange = true;
				
				// If the current index is assigned, then this means this is a different button than previous...
				if( CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count )
				{
					// Reset the stored input down index.
					buttonIndexOnInputDown = -1;

					// Exit the previous button.
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnExit();

					// Inform and subscribers that this button has been exited.
					OnButtonExit?.Invoke( CurrentButtonIndex );
				}

				// Assign the current button index to this index.
				CurrentButtonIndex = i;

				// Reset the button hover time since a new button has been hovered.
				buttonHoverTime = 0.0f;

				// Call the OnEnter function on the current button.
				UltimateRadialSubButtonList[ i ].OnEnter();

				// If the invoke action is set to being when the button is entered, then call the OnInteract() function on the current button.
				if( invokeAction == UltimateRadialMenuInputManager.InvokeAction.OnButtonEnter )
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnInteract();

				// If the user wants to display the pointer, and it is assigned...
				if( usePointer && pointerImage != null )
				{
					// Store the target rotation to being the angle of the sub button at the current index.
					pointerTargetRotation = Quaternion.Euler( 0, 0, UltimateRadialSubButtonList[ CurrentButtonIndex ].angle - pointerRotationOffset );

					// If the pointer is not currently active or the user wants to apply the rotation instantly, then simply apply the calculated rotation.
					if( !PointerActive || pointerSnapOption == UltimateRadialMenu.PointerSnapOption.Instant )
						pointerImage.rectTransform.localRotation = pointerTargetRotation;
				}
				
				// If the user wants to overwrite the radial menu text...
				if( overwriteRadialMenuText )
				{
					// If the user wants to display the name of the button on the radial menu and the radial menu text is assigned, then apply the name.
					if( radialMenu.displayButtonName && radialMenu.nameText != null )
						radialMenu.nameText.text = !UltimateRadialSubButtonList[ i ].buttonDisabled ? UltimateRadialSubButtonList[ i ].name : "";

					// If the user wants to display the description of the button on the radial menu and the text is assigned, then display the description.
					if( radialMenu.displayButtonDescription && radialMenu.descriptionText != null )
						radialMenu.descriptionText.text = !UltimateRadialSubButtonList[ i ].buttonDisabled ? UltimateRadialSubButtonList[ i ].description : "";
				}

				// Inform any subscribers that this button has been entered.
				OnButtonEnter?.Invoke( i );

				// Break the loop.
				break;
			}
		}

		// If the last frame caught input but this frame did not, OR the sub button list is zero...
		if( ( inputInRangeLastFrame && !InputInRange ) || UltimateRadialSubButtonList.Count == 0 )
		{
			// Reset the submenu.
			ResetSubmenu();

			// Inform any subscribers that the submenu has lost focus.
			OnMenuLostFocus?.Invoke();
		}
		
		// If the input is within range this frame...
		if( distance > radialMenu.BaseTransform.sizeDelta.x / 2 * minRange )
		{
			// If the radial menu is currently interactable...
			if( radialMenu.Interactable )
			{
				// Set Interactable to false so the radial menu will be frozen.
				radialMenu.Interactable = false;

				// Call the EnablePointer() function on the radial menu.
				radialMenu.EnablePointer();

				// If the current radial button is not null...
				if( CurrentRadialButton != null )
				{
					// If the user does NOT want to overwrite the menu text, then the text may have reset if the user has the min range setting too high...
					if( !overwriteRadialMenuText )
					{
						// If the user wants to display the name of the button and the text is assigned, then apply the name of the current radial button.
						if( radialMenu.displayButtonName && radialMenu.nameText != null )
							radialMenu.nameText.text = !CurrentRadialButton.buttonDisabled ? CurrentRadialButton.name : "";

						// If the user wants to display the description of the button and the text is assigned, then display the description of the current radial button.
						if( radialMenu.displayButtonDescription && radialMenu.descriptionText != null )
							radialMenu.descriptionText.text = !CurrentRadialButton.buttonDisabled ? CurrentRadialButton.description : "";
					}

					// If the user wants the radial button to be selected, then select the current Ultimate Radial Menu button.
					if( selectRadialButton )
						CurrentRadialButton.OnSelect();
					// Otherwise just enter the button to make sure the player can see which button has the submenu.
					else
						CurrentRadialButton.OnEnter();
				}
			}

			// If the input was not within range, the user does want a deactivation angle, and the current radial button is assigned...
			if( !InputInRange && submenuDeactivationAngle > 0.0f && CurrentRadialButton != null )
			{
				// If the current input angle is NOT within range of the deactivation angles from the radial buttons center angle, then disable the submenu.
				if( !( Mathf.Abs( angle - CurrentRadialButton.angle ) <= deactivationAngleRange || Mathf.Abs( ( angle - 360f ) - CurrentRadialButton.angle ) <= deactivationAngleRange || Mathf.Abs( angle - ( CurrentRadialButton.angle - 360f ) ) <= deactivationAngleRange ) )
					Disable();
			}
		}
		// Else the distance is less than the minimum range of the submenu, so if the radial menu is not currently interactable...
		else if( !radialMenu.Interactable )
		{
			// Set the radial menu to interactable.
			radialMenu.Interactable = true;

			// If the current radial button is assigned...
			if( CurrentRadialButton != null )
			{
				// If the user wanted the radial button in the selected state while navigating the submenu, then deselect the radial button.
				if( selectRadialButton )
					CurrentRadialButton.OnDeselect();
			}
		}
		// Else the distance is not within our active range for the submenu, and the Ultimate Radial Menu is currently active and interactable.
		else
		{
			// TODO: Potentially check input direction threshold here so that the input can still be considered "in range" when going to the farthest buttons on the menu.

			// So if the distance is less than the minimum range of the Ultimate Radial Menu, then clear all the submenu buttons.
			if( distance < radialMenu.CalculatedMinRange )
				Disable();
		}

		// If the input is within range this frame, and the stored button index is within range and this button is not disabled...
		if( InputInRange && CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count && !UltimateRadialSubButtonList[ CurrentButtonIndex ].buttonDisabled )
		{
			// If the input is down on this frame...
			if( inputDown )
			{
				// Call the OnInputDown() function on the current button if it is not selected.
				if( !UltimateRadialSubButtonList[ CurrentButtonIndex ].Selected )
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnInputDown();

				// If the invoke action is set to being when the button is down...
				if( invokeAction == UltimateRadialMenuInputManager.InvokeAction.OnButtonDown )
				{
					// Call the OnInteract() function on the current button.
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnInteract();

					// If the user wants to disable the submenu when interacted with, then disable the menus.
					if( disableOnInteract )
					{
						Disable();
						radialMenu.Disable();
					}
				}

				// Set the button index to the buttonIndexOnInputDown so that the button up can be calculated.
				buttonIndexOnInputDown = CurrentButtonIndex;

				// Inform and subscribers that this button has received down input.
				OnButtonInputDown?.Invoke( CurrentButtonIndex );
			}

			// If the input is up on this frame...
			if( inputUp )
			{
				// Call the OnInputUp() function on the current button if it is not selected.
				if( !UltimateRadialSubButtonList[ CurrentButtonIndex ].Selected )
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnInputUp();

				// If the invoke action is set to being when the button has been clicked, and the current button index is the same as when the buttonIndexOnInputDown was calculated...
				if( ( invokeAction == UltimateRadialMenuInputManager.InvokeAction.OnButtonClick && CurrentButtonIndex == buttonIndexOnInputDown ) || invokeAction == UltimateRadialMenuInputManager.InvokeAction.OnButtonUp )
				{
					// Call the OnInteract() function on the current button.
					UltimateRadialSubButtonList[ CurrentButtonIndex ].OnInteract();

					// If the user wants to disable the submenu when interacted with, then disable the submenu and the radial menu.
					if( disableOnInteract )
					{
						Disable();
						radialMenu.Disable();
					}
				}

				// Reset the buttonIndexOnInputDown.
				buttonIndexOnInputDown = -1;

				// Inform and subscribers that this button has received up input.
				OnButtonInputUp?.Invoke( CurrentButtonIndex );
			}
		}
		
		// Process the pointer before the inputInRangeLastFrame has been updated.
		ProcessPointer();

		// Process the submenu toggle.
		ProcessToggle();

		// If there are any subscribers to the OnRadialMenuProcessInput callback, notify them.
		OnProcessInput?.Invoke( input, distance, inputDown, inputUp );

		// If the input is in range, but wasn't last frame, then inform any subscribers that the menu has been focused.
		if( InputInRange && !inputInRangeLastFrame )
			OnMenuFocused?.Invoke();

		// Store the InputInRange value for the next calculation.
		inputInRangeLastFrame = InputInRange;
	}

	/// <summary>
	/// [INTERNAL] Resets the Ultimate Radial Sub Buttons and all the enabled options to their default state.
	/// </summary>
	void ResetSubmenu ()
	{
		// If the current index is assigned...
		if( CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count )
		{
			// Exit the current sub button.
			UltimateRadialSubButtonList[ CurrentButtonIndex ].OnExit();

			// Inform any subscribers that the submenu button has been exited.
			OnButtonExit?.Invoke( CurrentButtonIndex );
		}

		// Reset the stored indexes.
		CurrentButtonIndex = -1;
		buttonIndexOnInputDown = -1;

		// If the user wanted to overwrite the radial menu text...
		if( overwriteRadialMenuText )
		{
			// If the current radial button is not null, and the input is within the angle of the radial button and within range...
			if( CurrentRadialButton != null && CurrentRadialButton.IsInAngle( CurrentInputAngle ) && CurrentInputDistance <= radialMenu.CalculatedMaxRange )
			{
				// If the user wants to display the name of the button and the text is assigned, then apply the name of the current radial button.
				if( radialMenu.displayButtonName && radialMenu.nameText != null )
					radialMenu.nameText.text = !CurrentRadialButton.buttonDisabled ? CurrentRadialButton.name : "";

				// If the user wants to display the description of the button and the text is assigned, then display the description of the current radial button.
				if( radialMenu.displayButtonDescription && radialMenu.descriptionText != null )
					radialMenu.descriptionText.text = !CurrentRadialButton.buttonDisabled ? CurrentRadialButton.description : "";
			}
			// Else the button is unassigned or out of range, so set reset the text.
			else
			{
				if( radialMenu.displayButtonName == true && radialMenu.nameText != null )
					radialMenu.nameText.text = "";

				if( radialMenu.displayButtonDescription && radialMenu.descriptionText != null )
					radialMenu.descriptionText.text = "";
			}
		}
	}

	/// <summary>
	/// [INTERNAL] Resets the Ultimate Radial Menu so that it can process input again.
	/// </summary>
	void ResetRadialMenu ()
	{
		// If the radial menu is not assigned, then return to avoid errors.
		if( radialMenu == null )
			return;

		// Set the radial menu to interactable.
		radialMenu.Interactable = true;
		
		// If the current radial button is assigned...
		if( CurrentRadialButton != null )
		{
			// If the user wanted to select the radial button, then deselect it.
			if( selectRadialButton )
				CurrentRadialButton.OnDeselect();
			// Otherwise, just exit the button.
			else
				CurrentRadialButton.OnExit();
		}
	}
	
	/// <summary>
	/// [INTERNAL] Returns the ratio of the targeted sprite.
	/// </summary>
	/// <param name="sprite">The sprite to calculate the ratio of.</param>
	Vector2 GetImageAspectRatio ( Sprite sprite )
	{
		// If the provided sprite is null, then just return a one vector.
		if( sprite == null )
			return Vector2.one;

		// Return the sprites size divided by the highest value of it's height or width. This will be the aspect ratio of the sprite.
		return new Vector2( sprite.rect.width, sprite.rect.height ) / Mathf.Max( sprite.rect.width, sprite.rect.height );
	}

	/// <summary>
	/// [INTERNAL] Creates a button at the specified index.
	/// </summary>
	/// <param name="buttonIndex">The targeted button index to create the button at.</param>
	void CreateButtonAtIndex ( int buttonIndex )
	{
		// If there is a submenu button in the pool list, then insert the first submenu button from the pool list.
		if( UltimateRadialSubButtonPool.Count > 0 )
			UltimateRadialSubButtonList.Insert( buttonIndex, GetButtonFromPool() );
		// Else there is no pooled buttons, so create a new one.
		else
		{
			// Insert a new submenu button at the targeted index.
			UltimateRadialSubButtonList.Insert( buttonIndex, new UltimateRadialSubButton() );

			// Assign the submenu variable.
			UltimateRadialSubButtonList[ buttonIndex ].submenu = this;

			// If the base transform hasn't been stored yet, store it now.
			if( BaseTransform == null )
				BaseTransform = GetComponent<RectTransform>();

			// Create the button game object and set the parent.
			GameObject radialButton = new GameObject( "Radial Sub Button", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
			radialButton.transform.SetParent( BaseTransform );
			radialButton.transform.SetAsLastSibling();

			// Store the RectTransform component and modify it.
			UltimateRadialSubButtonList[ buttonIndex ].buttonTransform = radialButton.GetComponent<RectTransform>();
			UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.anchorMin = new Vector2( 0.5f, 0.5f );
			UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.anchorMax = new Vector2( 0.5f, 0.5f );
			UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.pivot = new Vector2( 0.5f, 0.5f );
			UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.localScale = Vector3.one;

			// Store the image component and update the sprite and color.
			UltimateRadialSubButtonList[ buttonIndex ].buttonImage = radialButton.GetComponent<Image>();
			UltimateRadialSubButtonList[ buttonIndex ].buttonImage.sprite = normalSprite;
			if( UltimateRadialSubButtonList[ buttonIndex ].buttonImage.sprite != null )
				UltimateRadialSubButtonList[ buttonIndex ].buttonImage.color = normalColor;
			else
				UltimateRadialSubButtonList[ buttonIndex ].buttonImage.color = Color.clear;

			// If the user wants to display a icon on the button...
			if( useButtonIcon )
			{
				// Create the button icon game object. Then set the parent as the buttonTransform, and change the name.
				GameObject buttonIcon = new GameObject( "Sub Button Icon", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
				buttonIcon.transform.SetParent( UltimateRadialSubButtonList[ buttonIndex ].buttonTransform );
				buttonIcon.gameObject.name = "Sub Button Icon";

				// Store the RectTransform component and set it to fill up the parent transform.
				UltimateRadialSubButtonList[ buttonIndex ].iconTransform = buttonIcon.GetComponent<RectTransform>();
				UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.anchorMin = new Vector2( 0.5f, 0.5f );
				UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.anchorMax = new Vector2( 0.5f, 0.5f );
				UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.pivot = new Vector2( 0.5f, 0.5f );
				UltimateRadialSubButtonList[ buttonIndex ].iconTransform.localScale = Vector3.one;

				// Store the buttonIcon image component and clear the sprite and update the color to clear.
				UltimateRadialSubButtonList[ buttonIndex ].icon = buttonIcon.GetComponent<Image>();
				UltimateRadialSubButtonList[ buttonIndex ].icon.sprite = null;
				UltimateRadialSubButtonList[ buttonIndex ].icon.color = Color.clear;
			}

			// If the user wants to use text on the button...
			if( useButtonText )
			{
				// Create the text object, set the parent and name.
				GameObject buttonText = new GameObject( "Sub Button Text", typeof( RectTransform ), typeof( CanvasRenderer ) );
				buttonText.transform.SetParent( UltimateRadialSubButtonList[ buttonIndex ].buttonTransform );

				// Store the text component and modify the settings.
				UltimateRadialSubButtonList[ buttonIndex ].text = buttonText.AddComponent<Text>();
				UltimateRadialSubButtonList[ buttonIndex ].text.text = "";
				UltimateRadialSubButtonList[ buttonIndex ].text.alignment = TextAnchor.MiddleCenter;
				UltimateRadialSubButtonList[ buttonIndex ].text.resizeTextForBestFit = true;
				UltimateRadialSubButtonList[ buttonIndex ].text.resizeTextMinSize = 0;
				UltimateRadialSubButtonList[ buttonIndex ].text.resizeTextMaxSize = 300;
				if( buttonTextFont != null )
					UltimateRadialSubButtonList[ buttonIndex ].text.font = buttonTextFont;
				UltimateRadialSubButtonList[ buttonIndex ].text.color = textNormalColor;
				UltimateRadialSubButtonList[ buttonIndex ].text.rectTransform.localScale = Vector3.one;

				// If the user wants a text outline on the button text...
				if( buttonTextOutline )
				{
					// Add a outline component and update the color.
					UnityEngine.UI.Outline textOutline = buttonText.AddComponent<UnityEngine.UI.Outline>();
					textOutline.effectColor = buttonTextOutlineColor;
				}
			}
		}
	}

	/// <summary>
	/// [INTERNAL] Finds the sub button index according to the provided integer index. This function will "fix" the index to make sure that it does what the user wants it to do.
	/// </summary>
	/// <param name="buttonIndex">The button index to modify.</param>
	void FindButtonIndex ( ref int buttonIndex )
	{
		// If there is a button being hovered...
		if( CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count )
		{
			// Exit the current button since the sub menu will be moved.
			UltimateRadialSubButtonList[ CurrentButtonIndex ].OnExit();

			// Reset the current button index.
			CurrentButtonIndex = -1;
		}
		
		// If the button index out of the range of the list, then the user doesn't care where the button goes...
		if( buttonIndex < 0 || buttonIndex >= UltimateRadialSubButtonList.Count )
		{
			// Create a radial sub button at the end of the list.
			CreateButtonAtIndex( UltimateRadialSubButtonList.Count );

			// Update the button index to the last button in the list.
			buttonIndex = UltimateRadialSubButtonList.Count - 1;
			return;
		}
		
		// Create a button at the users defined index.
		CreateButtonAtIndex( buttonIndex );
	}

	/// <summary>
	/// [INTERNAL] Returns the first sub button in the pool and removes it from the list.
	/// </summary>
	UltimateRadialSubButton GetButtonFromPool ()
	{
		// Grab the first pooled button, enable it, and remove it from the pool.
		UltimateRadialSubButton subButton = UltimateRadialSubButtonPool[ 0 ];
		subButton.buttonTransform.gameObject.SetActive( true );
		UltimateRadialSubButtonPool.Remove( subButton );
		
		// Return the sub button.
		return subButton;
	}

	/// <summary>
	/// [INTERNAL] Sends the sub button to the pool and removes it from the list of active buttons.
	/// </summary>
	/// <param name="buttonIndex">The targeted button index to send to the pool.</param>
	void SendButtonToPool ( int buttonIndex )
	{
		// Add the targeted button to the pool list.
		UltimateRadialSubButtonPool.Add( UltimateRadialSubButtonList[ buttonIndex ] );

		// Clear the button information so that it is ready for the next information.
		UltimateRadialSubButtonList[ buttonIndex ].ClearButtonInformation();

		// Set the game object to not active.
		UltimateRadialSubButtonList[ buttonIndex ].buttonTransform.gameObject.SetActive( false );

		// Remove this button from the list.
		UltimateRadialSubButtonList.RemoveAt( buttonIndex );
	}
	
	/// <summary>
	/// [INTERNAL] Processes the toggle of the submenu over time.
	/// </summary>
	void ProcessToggle ()
	{
		// If the user doesn't want to toggle the menu over time, then return.
		if( !useMenuToggle )
			return;

		// If the submenu needs to be toggled in...
		if( toggleIn )
		{
			// Lerp the toggle value over time according to the users settings.
			toggleLerpValue += Time.unscaledDeltaTime * toggleInSpeed;

			// If the user wants to use the canvas group to fade the menu, then transition the alpha from current to full by the lerp value.
			if( menuToggleAlpha )
				canvasGroup.alpha = toggleLerpValue;

			// If the user wants to have the scale of the buttons updated...
			if( menuToggleScale )
			{
				// Loop through all the buttons and apply the lerp value to the scale.
				for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
					UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one * toggleLerpValue;
			}

			// If the menu is still focused and the lerp value is complete...
			if( IsEnabled && toggleLerpValue >= 1.0f )
			{
				// Set toggleIn to false and the lerp value to 1.
				toggleIn = false;
				toggleLerpValue = 1.0f;

				// If the user wants use the canvas group alpha, then apply the full alpha.
				if( menuToggleAlpha )
					canvasGroup.alpha = 1.0f;

				// If the user wants to scale the buttons, apply the full scale to them all.
				if( menuToggleScale )
				{
					for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one;
				}
			}
		}
		// Else if the submenu needs to be toggled out...
		else if( toggleOut )
		{
			// Lerp the toggle value down.
			toggleLerpValue -= Time.unscaledDeltaTime * toggleOutSpeed;

			// If the user wants to fade the alpha, then transition the alpha from current to zero by the lerp value.
			if( menuToggleAlpha )
				canvasGroup.alpha = toggleLerpValue;

			// If the user wants to scale the buttons...
			if( menuToggleScale )
			{
				// Loop through all the buttons and apply the lerp value to the scale.
				for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
					UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one * toggleLerpValue;
			}

			// If the lerp value is over 1.0f...
			if( toggleLerpValue <= 0.0f )
			{
				// Set toggleOut and the lerp value to default.
				toggleOut = false;
				toggleLerpValue = 0.0f;

				// If the user wants use the canvas group alpha, then apply the zero alpha.
				if( menuToggleAlpha )
					canvasGroup.alpha = 0.0f;

				// If the user wants to scale the buttons, apply the zero scale to them all.
				if( menuToggleScale )
				{
					for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.zero;
				}
				
				// Clear the menu since the toggle is finished.
				ClearMenu();
			}
		}
	}

	/// <summary>
	/// [INTERNAL] Processes the pointer according to the users options.
	/// </summary>
	void ProcessPointer ()
	{
		// If the user wants to apply the rotation instantly ( which has already been done when the new button was found ) or the pointer transform is null, then return.
		if( !usePointer || pointerSnapOption == UltimateRadialMenu.PointerSnapOption.Instant || pointerImage == null )
			return;

		// If the user wants the color of the pointer to change...
		if( pointerColorChange )
		{
			// If the last known input is different than the current input in range...
			if( inputInRangeLastFrame != InputInRange )
			{
				// If the current input is within range of the radial menu...
				if( InputInRange )
				{
					// Set PointerActive to true for reference.
					PointerActive = true;
				
					// If the fade in duration is zero...
					if( pointerFadeInDuration <= 0.0f )
					{
						// Set the pointer color to the active color.
						pointerImage.color = pointerActiveColor;

						// If the fade out duration is set and the pointer is currently fading out...
						if( pointerFadeOutDuration > 0.0f && pointerFadeOut )
						{
							// Set pointerFadeOut to false so it won't fade, and reset the lerp value.
							pointerFadeOut = false;
							pointerLerpValue = 1.0f;
						}
					}
					// Else the user either does want to fade over time, and the fade in duration is set...
					else
					{
						// Set pointerFadeIn to true so it will run, and set pointerFadeOut to false so it won't run.
						pointerFadeIn = true;
						pointerFadeOut = false;
					}
				}
				// Else the current input is not in range of the menu...
				else
				{
					// If the fade out duration is not assigned...
					if( pointerFadeOutDuration <= 0.0f )
					{
						// Set PointerActive to false for reference.
						PointerActive = false;

						// Set the pointer image color to the normal color.
						pointerImage.color = pointerNormalColor;

						// If the fade in duration is set, and the pointer is currently fading in...
						if( pointerFadeInDuration > 0.0f && pointerFadeIn )
						{
							// Set the fade in controller to false and reset the lerp value.
							pointerFadeIn = false;
							pointerLerpValue = 0.0f;
						}
					}
					// Else the user doesn't want to fade over time, and the fade out duration is set...
					else
					{
						// Set pointerFadeOut to true so it will run, and set pointerFadeIn to false so it won't.
						pointerFadeOut = true;
						pointerFadeIn = false;
					}
				}
			}

			// If the pointer needs to be faded in...
			if( pointerFadeIn )
			{
				// Lerp the pointer value for the color over time.
				pointerLerpValue += Time.unscaledDeltaTime * pointerFadeInSpeed;

				// Transition the color from normal to active by pointerLerpValue.
				pointerImage.color = Color.Lerp( pointerNormalColor, pointerActiveColor, pointerLerpValue );

				// If the value is finished fading in...
				if( pointerLerpValue >= 1.0f )
				{
					// Set fade in controller to false so the pointer won't continue to fade in.
					pointerFadeIn = false;

					// Apply the final color.
					pointerImage.color = pointerActiveColor;

					// Finalize the lerp value so that it will be able to properly fade out.
					pointerLerpValue = 1.0f;
				}
			}
			// Else if the pointer needs to be faded out...
			else if( pointerFadeOut )
			{
				// Lerp the pointer value.
				pointerLerpValue -= Time.unscaledDeltaTime * pointerFadeOutSpeed;

				// Transition the color from the current color to normal by the pointerLerpValue.
				pointerImage.color = Color.Lerp( pointerNormalColor, pointerActiveColor, pointerLerpValue );

				// If the lerp value is finished fading out...
				if( pointerLerpValue <= 0.0f )
				{
					// Set pointerFadeOut to false so it won't continue to fade out.
					pointerFadeOut = false;

					// Apply the normal color.
					pointerImage.color = pointerNormalColor;

					// Finalize the lerp value and set PointerActive to false for reference.
					pointerLerpValue = 0.0f;
					PointerActive = false;
				}
			}
		}

		// If the pointer is not active and visible, then just return.
		if( !PointerActive )
			return;

		// If the snapping option is set to free, then transition the rotation to the current angle of the input.
		if( pointerSnapOption == UltimateRadialMenu.PointerSnapOption.Free )
			pointerImage.rectTransform.localRotation = Quaternion.Slerp( pointerImage.rectTransform.localRotation, Quaternion.Euler( 0, 0, CurrentInputAngle - pointerRotationOffset ), Time.unscaledDeltaTime * pointerTargetSpeed );
		// Else transition the rotation to the target rotation of the currently selected button.
		else
			pointerImage.rectTransform.localRotation = Quaternion.Slerp( pointerImage.rectTransform.localRotation, pointerTargetRotation, Time.unscaledDeltaTime * pointerTargetSpeed );
	}
	
	/// <summary>
	/// [INTERNAL] Returns the state of any basic errors being present.
	/// </summary>
	bool HasBasicErrors ()
	{
		// If the radial menu is unassigned...
		if( radialMenu == null )
		{
			// Try to store the radial menu component in the parent object.
			radialMenu = GetComponentInParent<UltimateRadialMenu>();

			// If the radial menu is still null...
			if( radialMenu == null )
			{
				// Inform the user and return to avoid any more errors.
				Debug.LogError( FormatDebug( "There is no Ultimate Radial Menu assigned to this submenu. It is a required property, so this component will disable itself until this property is assigned", "Please make sure that this object is placed inside of an Ultimate Radial Menu object so it can function properly", gameObject.name ) );
				return true;
			}
		}

		// If the submenu gameObject is not active in the scene...
		if( !gameObject.activeInHierarchy )
		{
			// Inform the user and return to avoid other errors.
			Debug.LogError( FormatDebug( "The submenu gameObject is disabled, which is preventing it from functioning properly", "Please make sure that the gameObject is enabled in your scene before running your application", gameObject.name ) );
			return true;
		}
		
		// Else return false for errors since there wasn't any.
		return false;
	}
	
	// -------------------------------------------------- < PUBLIC FUNCTIONS FOR THE USER > -------------------------------------------------- //
	/// <summary>
	/// Updates the positioning of the submenu buttons.
	/// </summary>
	public void UpdatePositioning ()
	{
		// Temporary float for the center angle to calculate off of.
		float centerAngle = 90.0f;

		// If the radial menu is assigned...
		if( radialMenu != null )
		{
			// If the menu is in world space, then apply the new size of the box collider according to the submenu.
			if( radialMenu.IsWorldSpaceRadialMenu )
				radialMenu.GetComponent<BoxCollider>().size = new Vector3( radialMenu.BaseTransform.sizeDelta.x * maxRange, radialMenu.BaseTransform.sizeDelta.y * maxRange, 0.001f );

			// If the application is running, and the current radial button is assigned, then use the current radial buttons angle.
			if( Application.isPlaying && CurrentRadialButton != null )
				centerAngle = CurrentRadialButton.angle;
			// Else if the application is NOT running, and the overall angle of the radial menu is less than a full 360, then center the submenu.
			else if( !Application.isPlaying && radialMenu.overallAngle < 360.0f )
				centerAngle -= radialMenu.centerAngle;

#if UNITY_EDITOR
			// If the application is NOT currently playing, and the editor submenu index is assigned and within range, then store the center angle used for the editor.
			if( !Application.isPlaying && EditorRadialMenuIndex >= 0 && EditorRadialMenuIndex < radialMenu.UltimateRadialButtonList.Count )
				centerAngle = radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex ].angle;
#endif

			// If the center angle is less that zero, then add 360 to make it a positive number for calculations.
			if( centerAngle < 0.0f )
				centerAngle += 360f;
		}

		// If the base transform is null, then attempt to find the RectTransform component.
		if( BaseTransform == null )
			BaseTransform = GetComponent<RectTransform>();
		
		// Apply the scale of 1 for calculations.
		BaseTransform.localScale = Vector3.one;

		// If the pivot is not 0.5, then set the pivot.
		if( BaseTransform.pivot != Vector2.one / 2 )
			BaseTransform.pivot = Vector2.one / 2;

		// Assign the position, rotation, and size delta of the radial menu transform.
		BaseTransform.localPosition = Vector3.zero;
		BaseTransform.localRotation = Quaternion.identity;
		if( radialMenu != null )
			BaseTransform.sizeDelta = radialMenu.GetComponent<RectTransform>().sizeDelta;

		// If the sub button list is empty, just return since there is no reason to try and position any buttons.
		if( UltimateRadialSubButtonList.Count == 0 )
			return;

		// Calculate the minimum range.
		CalculatedMinRange = BaseTransform.sizeDelta.x / 2 * minRange;

		// If the user wants to have an infinite max range then apply that, otherwise calculate the max range by the baseTransform's size delta.
		if( infiniteMaxRange )
			CalculatedMaxRange = Mathf.Infinity;
		else
			CalculatedMaxRange = BaseTransform.sizeDelta.x / 2 * maxRange;

		// Store the overall angle value for reference.
		OverallAngle = anglePerButton * UltimateRadialSubButtonList.Count;

		// Configure half of the total angle that will be needed for positioning.
		float halfTotalAngle = OverallAngle / 2;
		
		// Store the buttons size so it doesn't need to be calculated for each button in the loop.
		Vector2 buttonImageSize = ( BaseTransform.sizeDelta * buttonSize ) * GetImageAspectRatio( normalSprite );
		
		// Loop through all of the submenu buttons.
		for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
		{
			// Store the button index.
			UltimateRadialSubButtonList[ i ].buttonIndex = i;

			// Apply the size to the button transform.
			UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta = buttonImageSize;

			// Reset the normal position of this button.
			UltimateRadialSubButtonList[ i ].normalPosition = Vector3.zero;

			// Store the angle of the button. This starts at the center angle calculated earlier. After that just add/subtract the angle information.
			UltimateRadialSubButtonList[ i ].angle = centerAngle + halfTotalAngle - ( anglePerButton * i ) - ( anglePerButton / 2 );

			// If the center angle is greater than 180 degrees, reverse the stored angle so that it is correct.
			if( smartSequencing && centerAngle > 180 )
				UltimateRadialSubButtonList[ i ].angle = centerAngle - halfTotalAngle + ( anglePerButton * i ) + ( anglePerButton / 2 );
				
			// Configure the angle range for calculations.
			UltimateRadialSubButtonList[ i ].angleRange = anglePerButton / 2;

			UltimateRadialSubButtonList[ i ].normalPosition.x += Mathf.Cos( UltimateRadialSubButtonList[ i ].angle * Mathf.Deg2Rad ) * BaseTransform.sizeDelta.x * submenuDistance;
			UltimateRadialSubButtonList[ i ].normalPosition.y += Mathf.Sin( UltimateRadialSubButtonList[ i ].angle * Mathf.Deg2Rad ) * BaseTransform.sizeDelta.x * submenuDistance;
			
			// Apply the new position to the transform, as well as a default scale of one.
			UltimateRadialSubButtonList[ i ].buttonTransform.localPosition = UltimateRadialSubButtonList[ i ].normalPosition;
			UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one;

			// If the user wants to follow the orbital rotation of the menu, then calculate the rotation plus the rotation offset.
			if( followOrbitalRotation )
				UltimateRadialSubButtonList[ i ].buttonTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, UltimateRadialSubButtonList[ i ].angle - 90 ) );
			// Else just apply zero local rotation.
			else
				UltimateRadialSubButtonList[ i ].buttonTransform.localRotation = Quaternion.identity;

			// -------------------------- < SCALE TRANSFORM > -------------------------- //
			if( scaleTransform )
			{
				// If the highlighted position modifier is assigned, then calculate the position by using the direction of the normal position multiplied by the highlighted position mod.
				if( positionModifier != 0 )
					UltimateRadialSubButtonList[ i ].highlightedPosition = UltimateRadialSubButtonList[ i ].normalPosition + BaseTransform.TransformDirection( UltimateRadialSubButtonList[ i ].normalPosition ) * positionModifier;
				// Else assign the normal position.
				else
					UltimateRadialSubButtonList[ i ].highlightedPosition = UltimateRadialSubButtonList[ i ].normalPosition;

				// If the pressed position modifier is assigned, then calculate the position by using the direction of the normal position multiplied by the pressed position mod.
				if( pressedPositionModifier != 0 )
					UltimateRadialSubButtonList[ i ].pressedPosition = UltimateRadialSubButtonList[ i ].normalPosition + BaseTransform.TransformDirection( UltimateRadialSubButtonList[ i ].normalPosition ) * pressedPositionModifier;
				// Else assign the normal position.
				else
					UltimateRadialSubButtonList[ i ].pressedPosition = UltimateRadialSubButtonList[ i ].normalPosition;

				// If the selected position modifier is assigned, then calculate the position by using the direction of the normal position multiplied by the selected position mod.
				if( selectedScaleModifier != 1.0f || selectedPositionModifier != 0 )
				{
					UltimateRadialSubButtonList[ i ].selectedPosition = UltimateRadialSubButtonList[ i ].normalPosition + BaseTransform.TransformDirection( UltimateRadialSubButtonList[ i ].normalPosition ) * selectedPositionModifier;

					// If this button is currently selected then apply the position and scale.
					if( UltimateRadialSubButtonList[ i ].Selected )
					{
						UltimateRadialSubButtonList[ i ].buttonTransform.localPosition = UltimateRadialSubButtonList[ i ].selectedPosition;
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one * selectedScaleModifier;
					}
				}
				// Else assign the normal position.
				else
					UltimateRadialSubButtonList[ i ].selectedPosition = UltimateRadialSubButtonList[ i ].normalPosition;

				// If the disabled position modifier is assigned, then calculate the position...
				if( disabledScaleModifier != 1.0f || disabledPositionModifier != 0 )
				{
					// Calculate the position by using the direction of the normal position multiplied by the selected position mod.
					UltimateRadialSubButtonList[ i ].disabledPosition = UltimateRadialSubButtonList[ i ].normalPosition + BaseTransform.TransformDirection( UltimateRadialSubButtonList[ i ].normalPosition ) * disabledPositionModifier;

					// If this button is disabled, then apply the disabled position.
					if( UltimateRadialSubButtonList[ i ].buttonDisabled )
					{
						UltimateRadialSubButtonList[ i ].buttonTransform.localPosition = UltimateRadialSubButtonList[ i ].disabledPosition;
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one * disabledScaleModifier;
					}
				}
				// Else assign the normal position.
				else
					UltimateRadialSubButtonList[ i ].disabledPosition = UltimateRadialSubButtonList[ i ].normalPosition;
			}
			
			// -------------------------- < ICON POSITIONING > -------------------------- //
			if( useButtonIcon && UltimateRadialSubButtonList[ i ].icon != null )
			{
				// Store the positioning information so that it can modified if need be.
				float horizontalPos = iconHorizontalPosition;
				float verticalPos = iconVerticalPosition;
				float sizeMod = iconSize;
				float rotationMod = iconRotation;

				// If the user wants to use this icon with unique positioning...
				if( UltimateRadialSubButtonList[ i ].useIconUnique )
				{
					// Modify the positioning information with the unique information.
					horizontalPos = UltimateRadialSubButtonList[ i ].iconHorizontalPosition;
					verticalPos = UltimateRadialSubButtonList[ i ].iconVerticalPosition;
					sizeMod = UltimateRadialSubButtonList[ i ].iconSize;
					rotationMod = UltimateRadialSubButtonList[ i ].iconRotation;
				}

				// Configure the position for the icon.
				Vector2 iconPosition = Vector3.zero;
				iconPosition.x += ( UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.x * ( horizontalPos / 100 ) ) - ( UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.x / 2 );
				iconPosition.y += ( UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.y * ( verticalPos / 100 ) ) - ( UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.y / 2 );

				// Apply the size and position to the icon.
				UltimateRadialSubButtonList[ i ].iconTransform.sizeDelta = ( Vector2.one * BaseTransform.sizeDelta.x * sizeMod ) * GetImageAspectRatio( UltimateRadialSubButtonList[ i ].icon.sprite );
				UltimateRadialSubButtonList[ i ].iconTransform.localPosition = iconPosition;
				
				// Store the normal scale.
				UltimateRadialSubButtonList[ i ].iconNormalScale = UltimateRadialSubButtonList[ i ].invertScaleY ? new Vector3( 1, -1, 1 ) : new Vector3( 1, 1, 1 );
				UltimateRadialSubButtonList[ i ].iconTransform.localScale = UltimateRadialSubButtonList[ i ].iconNormalScale;

				// If the user wants to scale the icon transforms when the button changes states...
				if( iconScaleTransform )
				{
					// If this button is selected, then apply the selected scale mod.
					if( UltimateRadialSubButtonList[ i ].Selected )
						UltimateRadialSubButtonList[ i ].iconTransform.localScale = UltimateRadialSubButtonList[ i ].iconNormalScale * iconSelectedScaleModifier;
					// Else if the button is disabled, apply the disabled scale mod.
					else if( UltimateRadialSubButtonList[ i ].buttonDisabled )
						UltimateRadialSubButtonList[ i ].iconTransform.localScale = UltimateRadialSubButtonList[ i ].iconNormalScale * iconDisabledScaleModifier;
				}

				// If the user wants to use local rotation...
				if( iconLocalRotation )
				{
					// If the user wants to attempt to have the icons as upright as possible to the player...
					if( iconSmartRotation )
					{
						// Store the image rotation.
						float imageRotation = UltimateRadialSubButtonList[ i ].buttonTransform.localRotation.eulerAngles.z;

						// If the stored rotation is less than zero then add 360 to get a positive number.
						if( imageRotation < 0 )
							imageRotation += 360;

						// If the stored rotation is more than 90 degrees and less than 270, then increase the rotation by 180 to flip the icon.
						if( imageRotation > 90 && imageRotation < 270 )
							rotationMod += 180;
					}
				}
				// Else the user wants world space rotation so store the rotation modifier as the buttons negative rotation, subtracting the rotation of the users setting.
				else
					rotationMod = -UltimateRadialSubButtonList[ i ].buttonTransform.localRotation.eulerAngles.z - ( UltimateRadialSubButtonList[ i ].useIconUnique ? UltimateRadialSubButtonList[ i ].iconRotation : iconRotation );

				// Apply the rotation to the icon.
				UltimateRadialSubButtonList[ i ].icon.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, rotationMod ) );
			}

			// -------------------------- < TEXT POSITIONING > -------------------------- //
			if( useButtonText && UltimateRadialSubButtonList[ i ].text != null )
			{
				// Apply the size to the text transform.
				UltimateRadialSubButtonList[ i ].text.rectTransform.sizeDelta = new Vector2( BaseTransform.sizeDelta.x * textSize, BaseTransform.sizeDelta.x * textSize ) * textAreaRatio;

				// Since the user might want to increase the area at which they can position the text, this Vector2 will be a larger area to position.
				Vector2 modifiedRefSizeForText = new Vector2( UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.x, UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.y ) * 2f;

				// If the user wants to position the text relative to the icon and the icon is assigned, then use the iconTransform instead of the buttonTransform.
				if( relativeToIcon && UltimateRadialSubButtonList[ i ].iconTransform != null )
					modifiedRefSizeForText = new Vector2( UltimateRadialSubButtonList[ i ].iconTransform.sizeDelta.x, UltimateRadialSubButtonList[ i ].iconTransform.sizeDelta.y ) * 2f;

				// Store a vector2 for the text position.
				Vector3 textPosition = Vector2.zero;

				// Calculate the text position.
				textPosition.x += ( modifiedRefSizeForText.x * ( textHorizontalPosition / 100 ) ) - ( modifiedRefSizeForText.x / 2 );
				textPosition.y += ( modifiedRefSizeForText.y * ( textVerticalPosition / 100 ) ) - ( modifiedRefSizeForText.y / 2 );

				// If the user wants to position the text in local position to the button...
				if( textLocalPosition )
				{
					// Apply the local position to the text.
					UltimateRadialSubButtonList[ i ].text.rectTransform.localPosition = textPosition;

					// If the user wants the text to have a local rotation with the button...
					if( textLocalRotation )
					{
						// Store the image rotation.
						float imageRotation = UltimateRadialSubButtonList[ i ].buttonTransform.localRotation.eulerAngles.z;

						// If the rotation is less than zero then add 360 to get a positive number.
						if( imageRotation < 0 )
							imageRotation += 360;

						// If the rotation is more than 90 degrees and less than 270, then increase the rotation by 180 to flip the text so it is readable.
						if( imageRotation > 90 && imageRotation < 270 )
							UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.Euler( 0, 0, 180 );
						// Else just set the local rotation to zero.
						else
							UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.identity;

						// If the user wants the text rotation to be vertical...
						if( textRotationVertical )
						{
							// If the image rotation is in these specific angles, then add 90 degrees to the rotation.
							if( ( imageRotation > 90 && imageRotation < 180 ) || ( imageRotation >= 270 && imageRotation < 360 ) )
								UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.Euler( 0, 0, UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation.eulerAngles.z + 90 );
							// Else minus 90 from the rotation.
							else
								UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.Euler( 0, 0, UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation.eulerAngles.z - 90 );
						}
					}
					// Else the user does not want the rotation to be local, so apply the inverse rotation of the button.
					else
						UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, -UltimateRadialSubButtonList[ i ].buttonTransform.localRotation.eulerAngles.z ) );
				}
				// Else the text positioning is set to global...
				else
				{
					// If this canvas is in world space...
					if( radialMenu != null && radialMenu.IsWorldSpaceRadialMenu )
					{
						// Convert the position from local to world space.
						textPosition = BaseTransform.transform.TransformPoint( textPosition );

						// Modify the calculated position by subtracting the base transforms position.
						textPosition -= BaseTransform.position;
					}

					// If the user wants to position the text relative to the icon, and the icon is assigned, then use the iconTransform position.
					if( relativeToIcon && UltimateRadialSubButtonList[ i ].iconTransform != null )
						UltimateRadialSubButtonList[ i ].text.rectTransform.position = UltimateRadialSubButtonList[ i ].iconTransform.position + textPosition;
					// Else apply the position of the text to being the button position plus the calculated text position.
					else
						UltimateRadialSubButtonList[ i ].text.rectTransform.position = UltimateRadialSubButtonList[ i ].buttonTransform.position + textPosition;

					// Set the rotation of the text to being the inverse rotation of the button.
					UltimateRadialSubButtonList[ i ].text.rectTransform.localRotation = Quaternion.Euler( 0, 0, -UltimateRadialSubButtonList[ i ].buttonTransform.localRotation.eulerAngles.z );
				}
			}
		}

		// If the user wants a base image displayed and it's assigned...
		if( useBaseImage && baseImage != null )
		{
			// If the pivot is not 0.5, then set the pivot.
			if( baseImage.rectTransform.pivot != Vector2.one / 2 )
				baseImage.rectTransform.pivot = Vector2.one / 2;

			// Apply the size of the base image by the users settings.
			baseImage.rectTransform.sizeDelta = BaseTransform.sizeDelta * baseImageSize;
			baseImage.rectTransform.localPosition = Vector3.zero;

			// If the user wants to fill the image to match the amount of sub buttons that are present...
			if( baseImageUseFill )
			{
				// Configure the fill amount by the overall angle, modified by the angle modifier that the user may have set.
				baseImage.fillAmount = ( OverallAngle + ( baseImageAngleModifier * 2 ) ) / 360f;
				
				// Then configure the rotation of the base image to the center angle modified by the configured fill amount.
				baseImage.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, centerAngle - 90 + ( ( 360f * baseImage.fillAmount ) / 2 ) ) );
			}
			// Else the user just wants the base image. So center it on the current radial button.
			else
				baseImage.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, centerAngle - 90 ) );
        }

		// If the user wants the pointer and the image is assigned...
		if( usePointer && pointerImage != null )
		{
			// If the pivot is not 0.5, then set the pivot.
			if( pointerImage.rectTransform.pivot != Vector2.one / 2 )
				pointerImage.rectTransform.pivot = Vector2.one / 2;

			// Apply the size position to the pointer transform.
			pointerImage.rectTransform.sizeDelta = Vector2.one * ( BaseTransform.sizeDelta.x * pointerSize );
			pointerImage.rectTransform.localPosition = Vector3.zero;

			// If the game is not running, then apply the rotation to look at the first button plus the rotation offset that the user has set.
			if( !Application.isPlaying && UltimateRadialSubButtonList.Count > 0 )
				pointerImage.rectTransform.localRotation = Quaternion.Euler( 0, 0, centerAngle - pointerRotationOffset );
		}

		// If the user wants end images and the images are assigned...
		if( useEndImages && endImageLeft != null && endImageRight != null )
		{
			// Temporary Vector3 for configuring the default position of the image.
			Vector3 endImagePosition = Vector3.zero;

			// Temp float for the left end image angle.
			float endImageAngle = centerAngle + halfTotalAngle + endImageAngleModifier;

			// Calculate the angle.
			endImagePosition.x += Mathf.Cos( endImageAngle * Mathf.Deg2Rad );
			endImagePosition.y += Mathf.Sin( endImageAngle * Mathf.Deg2Rad );

			// Multiply the direction by the size delta of the radial menu modified by the distance set by the user.
			endImagePosition *= BaseTransform.sizeDelta.x * ( submenuDistance + endImageDistanceModifier );

			// Apply the new position and size to the transform, as well as a default scale of one.
			endImageLeft.rectTransform.sizeDelta = BaseTransform.sizeDelta * endImageSize * GetImageAspectRatio( endImageLeft.sprite );
			endImageLeft.rectTransform.localPosition = endImagePosition;
			endImageLeft.rectTransform.localScale = Vector3.one;

			// Apply the calculated rotation.
			endImageLeft.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, endImageAngle - 90 ) );

			// Reset the calculated image position.
			endImagePosition = Vector3.zero;

			// Configure the end image angle modifier for the right side.
			endImageAngle = centerAngle - halfTotalAngle - endImageAngleModifier;

			// Calculate the angle.
			endImagePosition.x += Mathf.Cos( endImageAngle * Mathf.Deg2Rad );
			endImagePosition.y += Mathf.Sin( endImageAngle * Mathf.Deg2Rad );

			// Multiply the direction by the size delta of the radial menu modified by the distance set by the user.
			endImagePosition *= BaseTransform.sizeDelta.x * ( submenuDistance + endImageDistanceModifier );

			// Apply the new position to the transform, default scale of one, and rotation.
			endImageRight.rectTransform.sizeDelta = BaseTransform.sizeDelta * endImageSize * GetImageAspectRatio( endImageRight.sprite );
			endImageRight.rectTransform.localPosition = endImagePosition;
			endImageRight.rectTransform.localScale = new Vector3( -1, 1, 1 );
			endImageRight.rectTransform.localRotation = Quaternion.Euler( new Vector3( 0, 0, endImageAngle - 90 ) );
		}

		// If the current child count is NOT the same as the last stored value...
		if( transform.childCount != submenuChildCount )
		{
			// Store the current child count so that this won't run until there is a change in child count.
			submenuChildCount = transform.childCount;

			// If the user has a base image, then set it as the first sibling.
			if( useBaseImage && baseImage != null )
				baseImage.transform.SetAsFirstSibling();

			// If the user has end images, then set them as the last siblings.
			if( useEndImages && endImageLeft != null && endImageRight != null )
			{
				endImageLeft.transform.SetAsLastSibling();
				endImageRight.transform.SetAsLastSibling();
			}

			// If the user has the pointer enabled and wants it as a specific sibling index...
			if( usePointer && pointerImage != null && pointerSiblingIndex != UltimateRadialMenu.SetSiblingIndex.Disabled )
			{
				// If the user wants first index...
				if( pointerSiblingIndex == UltimateRadialMenu.SetSiblingIndex.First )
				{
					// If the user also has a base image, then set the sibling index to 1.
					if( useBaseImage && baseImage != null )
						pointerImage.transform.SetSiblingIndex( 1 );
					// Else set it as the first index.
					else
						pointerImage.transform.SetAsFirstSibling();
				}
				// Else set the pointer as the last sibling.
				else
					pointerImage.transform.SetAsLastSibling();
			}
		}

		// If the stored index is within range, exit the button since the position has changed and reset the stored index.
		if( CurrentButtonIndex >= 0 && CurrentButtonIndex < UltimateRadialSubButtonList.Count )
		{
			UltimateRadialSubButtonList[ CurrentButtonIndex ].OnExit();
			CurrentButtonIndex = -1;
		}

		// If the user wants a deactivation angle calculated...
		if( submenuDeactivationAngle > 0.0f )
			deactivationAngleRange = halfTotalAngle + submenuDeactivationAngle;

		// Inform any subscribers that the positioning of the submenu has been updated.
		OnUpdatePositioning?.Invoke();
	}

	/// <summary>
	/// Register the provided information to the Ultimate Radial Submenu.
	/// </summary>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public void RegisterButton ( Action ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// Find the actual index of the radial button depending on what the user passed as the buttonIndex parameter.
		FindButtonIndex( ref buttonIndex );
		
		// Register the button information.
		UltimateRadialSubButtonList[ buttonIndex ].RegisterButtonInfo( buttonInfo );
		
		// Subscribe the ButtonCallback function to the OnRadialButtonInteract event.
		UltimateRadialSubButtonList[ buttonIndex ].AddCallback( ButtonCallback );

		// If the submenu is active currently, then update the positioning so it looks correct.
		if( IsEnabled )
			UpdatePositioning();
	}

	/// <summary>
	/// Register the provided information to the Ultimate Radial Submenu.
	/// </summary>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public void RegisterButton ( Action<int> ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// Find the actual index of the radial button depending on what the user passed as the buttonIndex parameter.
		FindButtonIndex( ref buttonIndex );
		
		// Register the button information.
		UltimateRadialSubButtonList[ buttonIndex ].RegisterButtonInfo( buttonInfo );

		// Subscribe the ButtonCallback function to the OnRadialButtonInteract event.
		UltimateRadialSubButtonList[ buttonIndex ].AddCallback( ButtonCallback );

		// If the submenu is active currently, then update the positioning so it looks correct.
		if( IsEnabled )
			UpdatePositioning();
	}

	/// <summary>
	/// Register the provided information to the Ultimate Radial Submenu.
	/// </summary>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public void RegisterButton ( Action<string> ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// Find the actual index of the radial button depending on what the user passed as the buttonIndex parameter.
		FindButtonIndex( ref buttonIndex );
		
		// Register the button information.
		UltimateRadialSubButtonList[ buttonIndex ].RegisterButtonInfo( buttonInfo );

		// Subscribe the ButtonCallback function to the OnRadialButtonInteract event.
		UltimateRadialSubButtonList[ buttonIndex ].AddCallback( ButtonCallback );

		// If the submenu is active currently, then update the positioning so it looks correct.
		if( IsEnabled )
			UpdatePositioning();
	}

	/// <summary>
	/// Call this function after you have added all the relevant submenu items so that the submenu will position itself and set everything up correctly.
	/// </summary>
	public void Enable ()
	{
		// If there are any basic errors that would prevent the Ultimate Radial Submenu from working properly, then return.
		if( HasBasicErrors() )
			return;

		// Set the internal calculations so that the submenu can function.
		IsEnabled = true;
		Interactable = true;
		firstFrameSkipped = false;

		// Store the current radial button for reference.
		CurrentRadialButton = radialMenu.UltimateRadialButtonList[ radialMenu.CurrentButtonIndex ];

		// If the radial button was stored successfully, then subscribe to the OnClearButtonInformation callback with this Disable function. That way, when the radial button gets cleared, so will the submenu.
		if( CurrentRadialButton != null )
			CurrentRadialButton.OnClearButtonInformation += Disable;

		// If the user wants to toggle the menu over time...
		if( useMenuToggle )
		{
			// If the fade in duration is zero...
			if( toggleInDuration <= 0.0f )
			{
				// Set the canvas group alpha to full.
				canvasGroup.alpha = 1.0f;

				// If the user wants to scale the buttons then loop through all the buttons and set the scale to one.
				if( menuToggleScale )
				{
					for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one;
				}

				// If the menu is currently fading out set toggleOut to false so it won't fade, and reset the lerp value.
				if( toggleOut )
				{
					toggleOut = false;
					toggleLerpValue = 1.0f;
				}
			}
			// Else the user either does want to fade over time, and the fade in duration is set...
			else
			{
				// If the user does not want to fade the canvas group, then just apply fully alpha.
				if( !menuToggleAlpha )
					canvasGroup.alpha = 1.0f;

				// If the user wants to scale the buttons...
				if( menuToggleScale )
				{
					// Loop through all the buttons and set the scale to the current lerp value. It's important to do this so that if the submenu is enabled when it was in the middle of fading out the buttons will be the current scale.
					for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.one * toggleLerpValue;
				}

				// Set toggleIn to true and toggleOut to false.
				toggleIn = true;
				toggleOut = false;
			}
		}
		// Else just set the canvas group so the player can see the submenu.
		else
			canvasGroup.alpha = 1.0f;

		// If the user has a base image assigned, then enable the component.
		if( useBaseImage && baseImage != null )
			baseImage.enabled = true;

		// If end images are supposed to be used...
		if( useEndImages )
		{
			// If the left image is assigned, enable it.
			if( endImageLeft != null && !endImageLeft.enabled )
				endImageLeft.enabled = true;

			// If the right image is assigned, enable it.
			if( endImageRight != null && !endImageRight.enabled )
				endImageRight.enabled = true;
		}

		// Update the positioning of all the buttons.
		UpdatePositioning();

		// Inform and subscribers that the submenu has been enabled.
		OnMenuEnabled?.Invoke();
	}

	/// <summary>
	/// Clears and disables the submenu.
	/// </summary>
	public void Disable ()
	{
		// Reset the local calculation variables.
		IsEnabled = false;
		Interactable = false;

		// Reset the radial menu.
		ResetRadialMenu();
		
		// Reset the current selected index.
		CurrentButtonIndex = -1;
		CurrentRadialButton = null;

		// If the user wants to toggle the visual state of the submenu...
		if( useMenuToggle )
		{
			// If the user wants the toggle out to be instant...
			if( toggleOutDuration <= 0.0f )
			{
				// If the user wants to scale the buttons, then loop through all the buttons and set the scale to zero.
				if( menuToggleScale )
				{
					for( int i = 0; i < UltimateRadialSubButtonList.Count; i++ )
						UltimateRadialSubButtonList[ i ].buttonTransform.localScale = Vector3.zero;
				}

				// If the menu is currently fading in, then set toggleIn to false and reset the lerp value.
				if( toggleIn )
				{
					toggleIn = false;
					toggleLerpValue = 0.0f;
				}

				// Clear the submenu since the disable is instant.
				ClearMenu();
			}
			// Else the user wants the disable to be over time, so set toggleOut to true and toggleIn to false.
			else
			{
				toggleOut = true;
				toggleIn = false;
			}
		}
		// Else the user doesn't have any menu toggle options set, so clear the menu.
		else
			ClearMenu();

		// Since the menu has been disabled, inform any subscribers.
		OnMenuDisabled?.Invoke();
	}

	/// <summary>
	/// Removes the specific submenu button at the provided index.
	/// </summary>
	/// <param name="buttonIndex">The index of the button to be removed from the list.</param>
	public void RemoveButton ( int buttonIndex )
	{
		// If the button index is out of range, inform user and return.
		if( buttonIndex < 0 || buttonIndex >= UltimateRadialSubButtonList.Count )
		{
			Debug.LogError( FormatDebug( $"The button index ({buttonIndex}) is out of range", "Please make sure the button you want to remove is still active before trying to remove it", gameObject.name + " (RemoveButton)" ) );
			return;
		}

		// Send the targeted button to the pool.
		SendButtonToPool( buttonIndex );

		// If the current button index is assigned and the same as this button index, reset the button index.
		if( CurrentButtonIndex >= 0 && CurrentButtonIndex == buttonIndex )
			CurrentButtonIndex = -1;

		// If all the submenu buttons have been removed, then disable the menu.
		if( UltimateRadialSubButtonList.Count == 0 )
			Disable();
		// Else there are still buttons, so update the positioning of all the buttons.
		else
			UpdatePositioning();
	}

	/// <summary>
	/// Clears the submenu of all buttons.
	/// </summary>
	public void ClearMenu ()
	{
		// Reset the local calculation variables.
		IsEnabled = false;
		Interactable = false;

		// Reset the radial menu.
		ResetRadialMenu();

		// Loop through the current sub button list and send them to the pool.
		for( int i = UltimateRadialSubButtonList.Count - 1; i >= 0; i-- )
			SendButtonToPool( i );

		// Reset the stored indexes.
		CurrentButtonIndex = -1;
		CurrentRadialButton = null;

		// If the user has a base image assigned, disable it.
		if( useBaseImage && baseImage != null )
			baseImage.enabled = false;

		// If the user has the pointer option enabled and the pointer is assigned...
		if( usePointer && pointerImage != null )
		{
			// Set PointerActive to false for reference since the submenu has been disabled.
			PointerActive = false;

			// If the user wanted to change the color of the pointer...
			if( pointerColorChange )
			{
				// Set the fade in and fade out controllers to false.
				pointerFadeIn = pointerFadeOut = false;
				pointerLerpValue = 0.0f;

				// Reset the pointer image color.
				pointerImage.color = pointerNormalColor;
			}
		}

		// If the end images are being used...
		if( useEndImages )
		{
			// If the end image left is assigned, then disable the image.
			if( endImageLeft != null )
				endImageLeft.enabled = false;

			// If the end image right is assigned, then disable the image.
			if( endImageRight != null )
				endImageRight.enabled = false;
		}

		// Since the menu has been cleared, inform any subscribers.
		OnMenuDisabled?.Invoke();
	}

	/// <summary>
	/// Registers the stored static submenu information.
	/// </summary>
	public void RegisterStaticInformation ()
	{
		// If the submenu has basic errors, then just return.
		if( HasBasicErrors() )
			return;

		// If the user doesn't want to use static information, inform the user and return.
		if( !useStaticInformation )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to register static button information, but no static information has been set in the editor", "Please exit play mode, go to the Sub Button Information section and register some static button information before attempting to call the RegisterStaticInformation function", gameObject.name ) );
			return;
		}

		// If there are more stored submenu informations than there are radial menu buttons, inform the user so that they know why the menus might be incorrect.
		if( SubmenuInformations.Count > radialMenu.UltimateRadialButtonList.Count )
		{
			Debug.LogWarning( FormatDebug( "There are more static submenu informations than there are radial menu buttons. The submenu may have registered information to the radial menu that is incorrect", "Please go to the Sub Button Information section and ensure that all the information is correct", gameObject.name ) );
		}

		// Loop through all the static button informations...
		for( int i = 0; i < SubmenuInformations.Count; i++ )
		{
			// If this index is out of the range of the radial menu buttons, break the loop.
			if( i >= radialMenu.UltimateRadialButtonList.Count )
				break;

			// If the information already is registered to the radial menu, then continue to the next index in the loop.
			if( SubmenuInformations[ i ].radialMenuButtonInfo.Registered || SubmenuInformations[ i ].SubButtonInformations.Count == 0 )
				continue;

			// If the user doesn't want to register a submenu to this button then skip this index.
			if( !SubmenuInformations[ i ].registerSubmenu )
				continue;

			// If the targeted radial menu button is disabled or already has an event registered, then just continue to the next index.
			if( radialMenu.UltimateRadialButtonList[ i ].buttonDisabled || radialMenu.UltimateRadialButtonList[ i ].Registered )
				continue;

			// Register the button information to the radial menu.
			radialMenu.RegisterButton( SubmenuInformations[ i ].PopulateSubmenu, SubmenuInformations[ i ].radialMenuButtonInfo );

			// Assign the submenu of the information for reference.
			SubmenuInformations[ i ].submenu = this;
		}
	}
	// ------------------------------------------------ < END PUBLIC FUNCTIONS FOR THE USER > ------------------------------------------------ //

	/// <summary>
	/// [INTERNAL] Formats and sends detailed information to the user.
	/// </summary>
	static string FormatDebug ( string error, string solution, string objectName )
	{
		return "<b>Ultimate Radial Submenu</b>\n" +
			"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
			"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
			"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
	}

	/// <summary>
	/// Confirms the existence of the radial submenu that has been registered with the targeted name.
	/// </summary>
	/// <param name="radialMenuName">The string name that the radial submenu has been registered with.</param>
	static bool ConfirmUltimateRadialSubmenu ( string radialMenuName )
	{
		// If the static submenu dictionary does not contain the targeted radial menu key, then inform the user and return false.
		if( !UltimateRadialSubmenus.ContainsKey( radialMenuName ) )
		{
			Debug.LogWarning( FormatDebug( $"There is no Ultimate Radial Submenu in your scene registered with the name: { radialMenuName }", "Please ensure that the main Ultimate Radial Menu component that this submenu is for is registered with the correct name", "Unknown (User Script)" ) );
			return false;
		}
		return true;
	}

	// ----------------------------------------------------- < PUBLIC STATIC FUNCTIONS > ----------------------------------------------------- //
	/// <summary>
	/// Returns the submenu component that has been registered with the targeted radial menu name.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	public static UltimateRadialSubmenu ReturnComponent ( string radialMenuName )
	{
		// If the submenu does not exist then return null.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return null;

		return UltimateRadialSubmenus[ radialMenuName ];
	}

	/// <summary>
	/// Registered the provided information to the targeted Ultimate Radial Submenu.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public static void RegisterButton ( string radialMenuName, Action ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].RegisterButton( ButtonCallback, buttonInfo, buttonIndex );
	}

	/// <summary>
	/// Registered the provided information to the targeted Ultimate Radial Submenu.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public static void RegisterButton ( string radialMenuName, Action<int> ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].RegisterButton( ButtonCallback, buttonInfo, buttonIndex );
	}

	/// <summary>
	/// Registered the provided information to the targeted Ultimate Radial Submenu.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	/// <param name="ButtonCallback">The function that will be called with the button is interacted with.</param>
	/// <param name="buttonInfo">The provided button information to apply to the sub button.</param>
	/// <param name="buttonIndex">[OPTIONAL] This parameter is optional and will determine where to register this information. If no parameter is provided, the information will be registered to the first available button.</param>
	public static void RegisterButton ( string radialMenuName, Action<string> ButtonCallback, UltimateRadialSubButtonInfo buttonInfo, int buttonIndex = -1 )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].RegisterButton( ButtonCallback, buttonInfo, buttonIndex );
	}

	/// <summary>
	/// Call this function after you have added all the relevant submenu items so that the submenu will position itself and set everything up correctly.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	public static void Enable ( string radialMenuName )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].Enable();
	}

	/// <summary>
	/// Clears and disables the submenu.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	public static void Disable ( string radialMenuName )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].Disable();
	}

	/// <summary>
	/// Removes the specific submenu button at the provided index.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	/// <param name="buttonIndex">The index of the button to be removed from the list.</param>
	public static void RemoveButton ( string radialMenuName, int buttonIndex )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].RemoveButton( buttonIndex );
	}

	/// <summary>
	/// Clears the submenu of all buttons.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	public static void ClearMenu ( string radialMenuName )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].ClearMenu();
	}

	/// <summary>
	/// Registers the stored static submenu information on the targeted Ultimate Radial Submenu.
	/// </summary>
	/// <param name="radialMenuName">The string name that the submenu's parent Ultimate Radial Menu has been registered with.</param>
	public static void RegisterStaticInformation ( string radialMenuName )
	{
		// If there is not a submenu that has been registered with the targeted radialMenuName, then return.
		if( !ConfirmUltimateRadialSubmenu( radialMenuName ) )
			return;

		UltimateRadialSubmenus[ radialMenuName ].RegisterStaticInformation();
	}
}