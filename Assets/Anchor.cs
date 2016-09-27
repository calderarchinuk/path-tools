﻿using UnityEngine;
using System.Collections;


public class Anchor : MonoBehaviour
{
	public CubicBezier3D Curve;
	public enum AnchorPointType
	{
		Start,
		End
	}
	public AnchorPointType AnchorPoint;
	public bool LockCurveToAnchor = true;

	private static Mesh _anchorMesh;
	public static Mesh AnchorMesh
	{
		get
		{
			if (_anchorMesh == null)
			{
				_anchorMesh = Resources.Load<Mesh>("AnchorMesh");
			}
			return _anchorMesh;

		}
	}
	//public Transform Target;
	//public float Power = 12;

	void OnDrawGizmos()
	{
		//Gizmos.DrawWireMesh(
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position,transform.forward * 10 + transform.position);

		Gizmos.color = Color.red;
		Gizmos.DrawLine(transform.position,transform.right + transform.position);
		Gizmos.DrawLine(transform.position,-transform.right + transform.position);
	}
}
