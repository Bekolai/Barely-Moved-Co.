using UnityEngine;
using Mirror;

namespace BarelyMoved.Items
{
	/// <summary>
	/// Server-authoritative physics carry controller.
	/// Creates a kinematic anchor and constrains the item to it using a ConfigurableJoint.
	/// Drives the anchor toward a target pose provided by player(s), preserving collisions.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
    public class CarryPhysicsController : NetworkBehaviour, ICarryController
	{
		#region Serialized Fields
		[Header("Anchor Settings")]
		[SerializeField] private string m_AnchorName = "CarryAnchor";
		[SerializeField] private float m_MaxMoveSpeed = 2.5f; // m/s clamp for anchor (raise to help catch-up)
		[SerializeField] private float m_MaxAngularSpeedDeg = 180f; // deg/s clamp
		[SerializeField, Range(0f, 1f)] private float m_TargetSmoothing = 0.25f;

		[Header("Joint Drives")]
		[SerializeField] private float m_LinearSpring = 500f;
		[SerializeField] private float m_LinearDamper = 55f;
		[SerializeField] private float m_AngularSpring = 250f;
		[SerializeField] private float m_AngularDamper = 30f;

		[Header("While Carried Physics")] 
		[SerializeField] private float m_CarriedDrag = 0.2f;
		[SerializeField] private float m_CarriedAngularDrag = 0.2f;
		[SerializeField] private int m_SolverPositionIterations = 12;
		[SerializeField] private int m_SolverVelocityIterations = 4;
		#endregion

		#region Private Fields
		private Rigidbody m_ItemBody;
		private Rigidbody m_AnchorBody;
		private ConfigurableJoint m_Joint;
		private Vector3 m_TargetPosition;
		private Quaternion m_TargetRotation = Quaternion.identity;
		private float m_DefaultDrag;
		private float m_DefaultAngularDrag;
		private int m_DefaultSolverPosIters;
		private int m_DefaultSolverVelIters;
		private bool m_IsActive;
		#endregion

		#region Unity Lifecycle
		private void Awake()
		{
			m_ItemBody = GetComponent<Rigidbody>();
		}

		private void Start()
		{
			if (isServer)
			{
				EnsureAnchor();
				DisableCarryInternal();
			}
		}

		private void FixedUpdate()
		{
			if (!isServer) return;
			if (!m_IsActive || m_AnchorBody == null) return;

			// Smoothly drive anchor toward target
			Vector3 currentPos = m_AnchorBody.position;
			Quaternion currentRot = m_AnchorBody.rotation;

			Vector3 desiredPos = Vector3.Lerp(currentPos, m_TargetPosition, m_TargetSmoothing);
			Quaternion desiredRot = Quaternion.Slerp(currentRot, m_TargetRotation, m_TargetSmoothing);

			// Clamp linear step
			float maxStep = m_MaxMoveSpeed * Time.fixedDeltaTime;
			Vector3 step = desiredPos - currentPos;
			if (step.magnitude > maxStep)
			{
				desiredPos = currentPos + step.normalized * maxStep;
			}

			// Clamp angular step
			float maxAngleStep = m_MaxAngularSpeedDeg * Time.fixedDeltaTime;
			Quaternion delta = desiredRot * Quaternion.Inverse(currentRot);
			delta.ToAngleAxis(out float angle, out Vector3 axis);
			if (angle > 180f) angle -= 360f;
			angle = Mathf.Abs(angle);
			if (angle > maxAngleStep && angle > 0.0001f)
			{
				desiredRot = Quaternion.AngleAxis(Mathf.Sign(Vector3.Dot(axis, axis)) * maxAngleStep, axis.normalized) * currentRot;
			}

			m_AnchorBody.MovePosition(desiredPos);
			m_AnchorBody.MoveRotation(desiredRot);
		}
		#endregion

		#region Public API (Server)
		[Server]
		public void StartCarry(Vector3 _position, Quaternion _rotation)
		{
			EnsureAnchor();
			m_TargetPosition = _position;
			m_TargetRotation = _rotation;
			EnableCarryInternal();
		}

		[Server]
		public void UpdateTarget(Vector3 _position, Quaternion _rotation)
		{
			m_TargetPosition = _position;
			m_TargetRotation = _rotation;
		}

		[Server]
		public void StopCarry(Vector3 _releaseVelocity)
		{
			DisableCarryInternal();
			var item = GetComponent<GrabbableItem>();
			if (item != null)
			{
				// ensure no residual joint forces by freeing motions prior to release
				ApplyJointActive(false);
				item.Release(_releaseVelocity);
			}
		}

		[Server]
		public void ForceStopCarry()
		{
			// Free motions immediately
			ApplyJointActive(false);
			DisableCarryInternal();
		}
		#endregion

		#region Internal
		private void EnsureAnchor()
		{
			if (m_AnchorBody != null) return;

			var anchorObj = new GameObject(m_AnchorName);
			anchorObj.transform.SetParent(transform, false);
			m_AnchorBody = anchorObj.AddComponent<Rigidbody>();
			m_AnchorBody.isKinematic = true;
			m_AnchorBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

			m_Joint = gameObject.AddComponent<ConfigurableJoint>();
			m_Joint.connectedBody = m_AnchorBody;
			m_Joint.autoConfigureConnectedAnchor = false;
			m_Joint.anchor = Vector3.zero;
			m_Joint.connectedAnchor = Vector3.zero;
			m_Joint.xMotion = ConfigurableJointMotion.Limited;
			m_Joint.yMotion = ConfigurableJointMotion.Limited;
			m_Joint.zMotion = ConfigurableJointMotion.Limited;
			m_Joint.angularXMotion = ConfigurableJointMotion.Limited;
			m_Joint.angularYMotion = ConfigurableJointMotion.Limited;
			m_Joint.angularZMotion = ConfigurableJointMotion.Limited;
			m_Joint.rotationDriveMode = RotationDriveMode.Slerp;

			SoftJointLimit linearLimit = new SoftJointLimit { limit = 0.01f };
			m_Joint.linearLimit = linearLimit;

			JointDrive posDrive = new JointDrive
			{
				positionSpring = m_LinearSpring,
				positionDamper = m_LinearDamper,
				maximumForce = Mathf.Infinity
			};
			m_Joint.xDrive = posDrive;
			m_Joint.yDrive = posDrive;
			m_Joint.zDrive = posDrive;

			JointDrive angDrive = new JointDrive
			{
				positionSpring = m_AngularSpring,
				positionDamper = m_AngularDamper,
				maximumForce = Mathf.Infinity
			};
			m_Joint.slerpDrive = angDrive;

			m_Joint.projectionMode = JointProjectionMode.PositionAndRotation;
			m_Joint.projectionDistance = 0.1f;
			m_Joint.projectionAngle = 15f;

			// Start inactive (motions free)
			ApplyJointActive(false);
		}

		private void EnableCarryInternal()
		{
			if (m_IsActive) return;
			m_IsActive = true;

			m_DefaultDrag = m_ItemBody.linearDamping;
			m_DefaultAngularDrag = m_ItemBody.angularDamping;
			m_DefaultSolverPosIters = m_ItemBody.solverIterations;
			m_DefaultSolverVelIters = m_ItemBody.solverVelocityIterations;

			m_ItemBody.linearDamping = m_CarriedDrag;
			m_ItemBody.angularDamping = m_CarriedAngularDrag;
			m_ItemBody.solverIterations = m_SolverPositionIterations;
			m_ItemBody.solverVelocityIterations = m_SolverVelocityIterations;
			m_ItemBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			m_ItemBody.interpolation = RigidbodyInterpolation.Interpolate;

			ApplyJointActive(true);
		}

		private void DisableCarryInternal()
		{
			m_IsActive = false;
			ApplyJointActive(false);
			if (m_AnchorBody != null)
			{
				m_AnchorBody.position = transform.position;
				m_AnchorBody.rotation = transform.rotation;
			}

			// restore item body settings
			if (m_ItemBody != null)
			{
				m_ItemBody.linearDamping = m_DefaultDrag;
				m_ItemBody.angularDamping = m_DefaultAngularDrag;
				m_ItemBody.solverIterations = m_DefaultSolverPosIters;
				m_ItemBody.solverVelocityIterations = m_DefaultSolverVelIters;
			}
		}

		private void ApplyJointActive(bool _active)
		{
			if (m_Joint == null) return;

			if (_active)
			{
				m_Joint.xMotion = ConfigurableJointMotion.Limited;
				m_Joint.yMotion = ConfigurableJointMotion.Limited;
				m_Joint.zMotion = ConfigurableJointMotion.Limited;
				m_Joint.angularXMotion = ConfigurableJointMotion.Limited;
				m_Joint.angularYMotion = ConfigurableJointMotion.Limited;
				m_Joint.angularZMotion = ConfigurableJointMotion.Limited;

				JointDrive posDrive = new JointDrive
				{
					positionSpring = m_LinearSpring,
					positionDamper = m_LinearDamper,
					maximumForce = Mathf.Infinity
				};
				m_Joint.xDrive = posDrive;
				m_Joint.yDrive = posDrive;
				m_Joint.zDrive = posDrive;

				JointDrive angDrive = new JointDrive
				{
					positionSpring = m_AngularSpring,
					positionDamper = m_AngularDamper,
					maximumForce = Mathf.Infinity
				};
				m_Joint.slerpDrive = angDrive;
			}
			else
			{
				m_Joint.xMotion = ConfigurableJointMotion.Free;
				m_Joint.yMotion = ConfigurableJointMotion.Free;
				m_Joint.zMotion = ConfigurableJointMotion.Free;
				m_Joint.angularXMotion = ConfigurableJointMotion.Free;
				m_Joint.angularYMotion = ConfigurableJointMotion.Free;
				m_Joint.angularZMotion = ConfigurableJointMotion.Free;

				JointDrive zeroDrive = new JointDrive { positionSpring = 0f, positionDamper = 0f, maximumForce = 0f };
				m_Joint.xDrive = zeroDrive;
				m_Joint.yDrive = zeroDrive;
				m_Joint.zDrive = zeroDrive;
				m_Joint.slerpDrive = zeroDrive;
			}
		}
		#endregion
	}
}


