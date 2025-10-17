using System;
using UnityEngine;
using TMPro;

namespace BarelyMoved
{
	/// <summary>
	/// Runtime component for a single floating damage text instance.
	/// Spawner controls lifecycle via Play and a completion callback for pooling.
	/// </summary>
	public sealed class DamageText : MonoBehaviour
	{
		[SerializeField] private TMP_Text m_Text;
		public bool UsesUGUI { get; private set; }
		[SerializeField] private float m_CurrentLifetime;
		[SerializeField] private float m_TotalLifetime;
		[SerializeField] private float m_FloatSpeed;
		[SerializeField] private UnityEngine.Camera m_Camera;
		[SerializeField] private Action<DamageText> m_OnComplete;

		private Transform m_CachedTransform;
		private Color m_StartColor;
		private bool m_IsPlaying;

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
			m_CachedTransform.position = worldPosition + randomJitter;
			m_IsPlaying = true;
			gameObject.SetActive(true);
		}

		void LateUpdate()
		{
			if (!m_IsPlaying) return;

			// Face the camera (billboard) for 3D text. UGUI will inherit Canvas rotation.
			if (!UsesUGUI && m_Camera != null)
			{
				m_CachedTransform.rotation = Quaternion.LookRotation(m_CachedTransform.position - m_Camera.transform.position);
			}

			// Float upwards over time
			m_CachedTransform.position += Vector3.up * (m_FloatSpeed * Time.deltaTime);

			// Lifetime & fade
			m_CurrentLifetime -= Time.deltaTime;
			float t = 1f - Mathf.Clamp01(1f - (m_CurrentLifetime / m_TotalLifetime));
			Color c = m_StartColor;
			c.a = t;
			m_Text.color = c;

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
			gameObject.SetActive(false);
		}
	}
}


