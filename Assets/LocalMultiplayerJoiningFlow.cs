using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Input=UnityEngine.InputSystem.Input;

public class LocalMultiplayerJoiningFlow : MonoBehaviour
{
	private InputPlayer m_InputPlayer;
	private List<InputPlayer> m_Players;

	public void Start()
	{
		m_Players = new List<InputPlayer>();

		m_InputPlayer = new InputPlayer();
		m_InputPlayer = Input.AssignPlayerToNextDevice(DeviceTypes.Gamepad);
	}

	public void Update()
	{
		if (m_InputPlayer.isAssigned && !m_Players.Contains(m_InputPlayer))
		{
			m_Players.Add(m_InputPlayer);
		}
	}
}

public class ReadActionValues : MonoBehaviour
{
	public void Update()
	{
		// var move = Input.move.value;
	}
}