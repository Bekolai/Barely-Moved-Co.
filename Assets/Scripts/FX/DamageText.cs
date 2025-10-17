using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

namespace BarelyMoved
{
	/// <summary>
	/// Runtime component for a single floating damage text instance.
	/// Spawner controls lifecycle via Play and a completion callback for pooling.
	///
	/// Enhanced Features:
	/// - Animation curves for non-linear movement and fading
	/// - Scale animation (pulsing/growing effects)
	/// - Outline and glow effects for better visibility
	/// - Color gradients for visual variety
	/// - Curved movement patterns (arcs and curves)
	/// - Configurable styling options
	/// </summary>
	public sealed class DamageText : MonoBehaviour
	{
		[Header("Core Components")]
		[SerializeField] private TMP_Text m_Text;
		public bool UsesUGUI { get; private set; }

		[Header("Lifetime & Movement")]
		[SerializeField] private float m_CurrentLifetime;
		[SerializeField] private float m_TotalLifetime;
		[SerializeField] private float m_FloatSpeed;
		[SerializeField] private AnimationCurve m_MovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField] private AnimationCurve m_FadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

		[Header("Scale Animation")]
		[SerializeField] private bool m_UseScaleAnimation = true;
		[SerializeField] private float m_ScaleMultiplier = 1.5f;
		[SerializeField] private AnimationCurve m_ScaleCurve = new AnimationCurve(
			new Keyframe(0f, 0.5f, 0f, 2f),
			new Keyframe(0.5f, 1.5f, 2f, 0f),
			new Keyframe(1f, 1f, -1f, 0f)
		);

		[Header("Visual Effects")]
		[SerializeField] private bool m_UseOutline = true;
		[SerializeField] private float m_OutlineWidth = 0.2f;
		[SerializeField] private Color m_OutlineColor = Color.black;
		[SerializeField] private bool m_UseGlow = false;
		[SerializeField] private Color m_GlowColor = Color.yellow;
		[SerializeField] private float m_GlowPower = 1f;

		[Header("Movement Pattern")]
		[SerializeField] private bool m_UseCurvedMovement = false;
		[SerializeField] private float m_CurveHeight = 0.5f;
		[SerializeField] private Vector3 m_MovementDirection = Vector3.up;

		[Header("References")]
		[SerializeField] private UnityEngine.Camera m_Camera;
		[SerializeField] private Action<DamageText> m_OnComplete;

		private Transform m_CachedTransform;
		private Color m_StartColor;
		private Color m_EndColor;
		private Vector3 m_StartPosition;
		private Vector3 m_TargetPosition;
		private bool m_IsPlaying;
		private float m_Progress;
		private bool m_UseColorGradient;

		void Awake()
		{
			m_CachedTransform = transform;
			if (m_Text == null)
			{
				// Try self
				m_Text = GetComponent<TMP_Text>();
				// Try children (user-made prefabs often nest the text)
				if (m_Text == null) m_Text = GetComponentInChildren<TMP_Text>(true);
				// Fallback to a 3D TextMeshPro if none present
				if (m_Text == null)
				{
					var tmp3D = gameObject.AddComponent<TextMeshPro>();
					tmp3D.alignment = TextAlignmentOptions.Center;
					tmp3D.textWrappingMode = TextWrappingModes.NoWrap;
					tmp3D.raycastTarget = false;
					var renderer = tmp3D.GetComponent<MeshRenderer>();
					if (renderer != null)
					{
						renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						renderer.receiveShadows = false;
					}
					m_Text = tmp3D;
				}
			}

			UsesUGUI = (m_Text is TextMeshProUGUI);
		}

		public void ConfigureAppearance(TMP_FontAsset font, float fontSize)
		{
			if (font != null) m_Text.font = font;
			m_Text.fontSize = fontSize;
		}

		public void ConfigureStyling(bool useOutline, float outlineWidth, Color outlineColor, bool useGlow, Color glowColor, float glowPower)
		{
			m_UseOutline = useOutline;
			m_OutlineWidth = outlineWidth;
			m_OutlineColor = outlineColor;
			m_UseGlow = useGlow;
			m_GlowColor = glowColor;
			m_GlowPower = glowPower;

			ApplyTextStyling();
		}

		public void ConfigureColorGradient(Color startColor, Color endColor, bool useGradient = true)
		{
			m_StartColor = startColor;
			m_EndColor = endColor;
			m_UseColorGradient = useGradient;
		}

		private void ApplyTextStyling()
		{
			if (m_Text == null) return;

			// Apply outline effect
			if (m_UseOutline && m_Text.fontSharedMaterial != null)
			{
				m_Text.fontSharedMaterial.SetFloat("_OutlineWidth", m_OutlineWidth);
				m_Text.fontSharedMaterial.SetColor("_OutlineColor", m_OutlineColor);
			}

			// Apply glow effect (if supported by the font material)
			if (m_UseGlow && m_Text.fontSharedMaterial != null)
			{
				m_Text.fontSharedMaterial.SetColor("_GlowColor", m_GlowColor);
				m_Text.fontSharedMaterial.SetFloat("_GlowPower", m_GlowPower);
			}
		}

		public void Play(string text, Color color, float lifetime, float floatSpeed, Vector3 worldPosition, Vector3 randomJitter, UnityEngine.Camera camera, Action<DamageText> onComplete)
		{
			m_Camera = camera != null ? camera : UnityEngine.Camera.main;
			m_OnComplete = onComplete;
			m_TotalLifetime = Mathf.Max(0.01f, lifetime);
			m_CurrentLifetime = m_TotalLifetime;
			m_FloatSpeed = floatSpeed;
			m_StartColor = color;
			m_Text.text = text;
			m_Text.color = m_StartColor;

			// Initialize positions for movement calculation
			m_StartPosition = worldPosition + randomJitter;
			m_CachedTransform.position = m_StartPosition;

			// Calculate target position based on movement pattern
			if (m_UseCurvedMovement)
			{
				// Create an arc movement
				Vector3 horizontalOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-0.5f, 0.5f));
				m_TargetPosition = m_StartPosition + (m_MovementDirection * m_FloatSpeed * m_TotalLifetime) + horizontalOffset + (Vector3.up * m_CurveHeight);
			}
			else
			{
				m_TargetPosition = m_StartPosition + (m_MovementDirection * m_FloatSpeed * m_TotalLifetime);
			}

			m_Progress = 0f;
			m_IsPlaying = true;

			// Apply styling and make visible
			ApplyTextStyling();
			gameObject.SetActive(true);
		}

		void LateUpdate()
		{
			if (!m_IsPlaying) return;

			// Update progress
			m_CurrentLifetime -= Time.deltaTime;
			m_Progress = Mathf.Clamp01(1f - (m_CurrentLifetime / m_TotalLifetime));


			if(m_Camera != null)
			{
				transform.LookAt(m_Camera.transform);
				transform.Rotate(0, 180, 0);
			}

			// Movement with animation curve
			float movementProgress = m_MovementCurve.Evaluate(m_Progress);

			if (m_UseCurvedMovement)
			{
				// Curved movement using bezier-like interpolation
				Vector3 currentPos = Vector3.Lerp(m_StartPosition, m_TargetPosition, movementProgress);

				// Add curve height in the middle of the animation
				float curveT = Mathf.Sin(movementProgress * Mathf.PI);
				currentPos.y += curveT * m_CurveHeight;

				m_CachedTransform.position = currentPos;
			}
			else
			{
				// Linear movement with curve
				m_CachedTransform.position = Vector3.Lerp(m_StartPosition, m_TargetPosition, movementProgress);
			}

			// Scale animation
			if (m_UseScaleAnimation)
			{
				float scaleProgress = m_ScaleCurve.Evaluate(m_Progress);
				float scale = 1f + (scaleProgress * (m_ScaleMultiplier - 1f));
				m_CachedTransform.localScale = new Vector3(scale, scale, scale);
			}

			// Face the camera (billboard) for 3D text. UGUI will inherit Canvas rotation.
			if (!UsesUGUI && m_Camera != null)
			{
				m_CachedTransform.rotation = Quaternion.LookRotation(m_CachedTransform.position - m_Camera.transform.position);
			}

			// Color and fade with animation curve
			float fadeProgress = m_FadeCurve.Evaluate(m_Progress);
			Color currentColor;

			if (m_UseColorGradient)
			{
				// Interpolate between start and end colors
				currentColor = Color.Lerp(m_StartColor, m_EndColor, m_Progress);
			}
			else
			{
				currentColor = m_StartColor;
			}

			currentColor.a = fadeProgress;
			m_Text.color = currentColor;

			// Check for completion
			if (m_CurrentLifetime <= 0f)
			{
				Complete();
			}
		}

		private void Complete()
		{
			m_IsPlaying = false;
			m_OnComplete?.Invoke(this);
		}

		public void ResetForPool()
		{
			m_IsPlaying = false;
			m_OnComplete = null;
			m_Camera = null;
			m_Progress = 0f;
			m_StartPosition = Vector3.zero;
			m_TargetPosition = Vector3.zero;
			m_CachedTransform.localScale = Vector3.one;
			m_UseColorGradient = false;
			m_StartColor = Color.white;
			m_EndColor = Color.white;
			gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Example usage of the enhanced DamageText system.
	/// Attach this to a game object and call SpawnDamageText() to see the effects.
	/// </summary>
	public static class DamageTextExamples
	{
		/// <summary>
		/// Example of spawning a critical hit with dramatic effects
		/// </summary>
		public static void SpawnCriticalHit(DamageText textComponent, Vector3 position, int damage, UnityEngine.Camera camera = null)
		{
			// Configure for critical hit styling
			textComponent.ConfigureStyling(
				useOutline: true,
				outlineWidth: 0.3f,
				outlineColor: Color.yellow,
				useGlow: true,
				glowColor: Color.red,
				glowPower: 2f
			);

			// Configure color gradient for dramatic effect
			textComponent.ConfigureColorGradient(Color.red, Color.yellow);

			// Play with enhanced settings
			textComponent.Play(
				text: $"{damage}!",
				color: Color.red,
				lifetime: 2f,
				floatSpeed: 3f,
				worldPosition: position,
				randomJitter: UnityEngine.Random.insideUnitSphere * 0.5f,
				camera: camera,
				onComplete: null
			);
		}

		/// <summary>
		/// Example of spawning normal damage with subtle effects
		/// </summary>
		public static void SpawnNormalDamage(DamageText textComponent, Vector3 position, int damage, UnityEngine.Camera camera = null)
		{
			// Configure for normal damage styling
			textComponent.ConfigureStyling(
				useOutline: true,
				outlineWidth: 0.2f,
				outlineColor: Color.black,
				useGlow: false,
				glowColor: Color.white,
				glowPower: 1f
			);

			// Use single color for normal damage
			textComponent.ConfigureColorGradient(Color.white, Color.white, useGradient: false);

			// Play with standard settings
			textComponent.Play(
				text: damage.ToString(),
				color: Color.white,
				lifetime: 1.5f,
				floatSpeed: 2f,
				worldPosition: position,
				randomJitter: UnityEngine.Random.insideUnitSphere * 0.3f,
				camera: camera,
				onComplete: null
			);
		}
	}
}


