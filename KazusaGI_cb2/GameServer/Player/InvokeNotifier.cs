using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer;

public class InvokeNotifier<T> where T : ProtoBuf.IExtensible
{
	private readonly Player Owner;
	private readonly Type PacketType;

	List<T> ToScene = new();
	List<T> ToWorld = new();
	List<T> ToWorldOthers = new();
	List<T> ToHost = new();
	Dictionary<uint, T> ToPeer = new(); // peerId

	public InvokeNotifier(Player owner, Type packetType)
	{
		Owner = owner;
		if (packetType == typeof(CombatInvocationsNotify) ||
			packetType == typeof(AbilityInvocationsNotify) ||
			packetType == typeof(ClientAbilityInitFinishNotify))
		{
			PacketType = packetType;
		}
		else
			throw new Exception($"Unsupported packet type: {packetType}");
	}
	public void AddEntry(T entry, ForwardType forward = ForwardType.ForwardToAll, uint peer = default)
	{
		ToWorld.Add(entry);

		/*
		switch (forward)
		{
			case ForwardType.ForwardLocal:
				ToScene.Add(entry);
				break;
			case ForwardType.ForwardToAll:
				ToWorld.Add(entry);
				break;
			case ForwardType.ForwardToAllExceptCur:
				ToWorldOthers.Add(entry);
				break;
			case ForwardType.ForwardToHost:
				ToHost.Add(entry);
				break;
			case ForwardType.ForwardToAllGuest: //???
			case ForwardType.ForwardToPeer: //???
			case ForwardType.ForwardToPeers:
				ToScene.Add(entry);
				break;
			case ForwardType.ForwardOnlyServer:
				break;
			case ForwardType.ForwardToAllExistExceptCur:
				ToWorldOthers.Add(entry);
				break;
		}
		*/
	}

	public void Notify()
	{
		// For KazusaGI_cb2, we'll send to the current session for all forward types
		// This is a simplified implementation that works with the current architecture
		
		var allEntries = new List<T>();
		allEntries.AddRange(ToScene);
		allEntries.AddRange(ToWorld);
		allEntries.AddRange(ToWorldOthers);
		allEntries.AddRange(ToHost);
		allEntries.AddRange(ToPeer.Values);

		if (allEntries.Any())
		{
			var packet = CreateNotifyPacket(allEntries);
			if (packet != null)
			{
				// Get session from owner - we need to determine how to access it
				// For now, let's assume Owner has access to session
				var session = GetSessionFromOwner();
				session?.SendPacket(packet);
			}
			
			// Clear all lists
			ToScene.Clear();
			ToWorld.Clear();
			ToWorldOthers.Clear();
			ToHost.Clear();
			ToPeer.Clear();
		}
	}

	private ProtoBuf.IExtensible? CreateNotifyPacket(List<T> entries)
	{
		if (PacketType == typeof(AbilityInvocationsNotify))
		{
			var notify = new AbilityInvocationsNotify();
			foreach (var entry in entries)
			{
				if (entry is AbilityInvokeEntry abilityEntry)
					notify.Invokes.Add(abilityEntry);
			}
			return notify;
		}
		else if (PacketType == typeof(CombatInvocationsNotify))
		{
			var notify = new CombatInvocationsNotify();
			foreach (var entry in entries)
			{
				if (entry is CombatInvokeEntry combatEntry)
					notify.InvokeLists.Add(combatEntry);
			}
			return notify;
		}
		else if (PacketType == typeof(ClientAbilityInitFinishNotify))
		{
			var notify = new ClientAbilityInitFinishNotify();
			foreach (var entry in entries)
			{
				if (entry is AbilityInvokeEntry abilityEntry)
					notify.Invokes.Add(abilityEntry);
			}
			return notify;
		}
		
		return null;
	}

	private Session? GetSessionFromOwner()
	{
		return Owner.Session;
	}
}