using Godot;
using System;
using System.Collections.Generic;

public partial class Inventory : Node
{
	public static Inventory Instance { get; private set; }
	
	public enum ItemType
	{
		None,
		Battery,
		Flashlight,
		Key
	}
	
	private Dictionary<ItemType, int> _items = new Dictionary<ItemType, int>();
	
	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
	}
	
	public bool AddItem(ItemType type, int count = 1)
	{
		if (_items.ContainsKey(type))
			_items[type] += count;
		else
			_items[type] = count;
		
		GD.Print($"Добавлен {type} x{count}");
		return true;
	}
	
	public bool RemoveItem(ItemType type, int count = 1)
	{
		if (!_items.ContainsKey(type) || _items[type] < count)
			return false;
		
		_items[type] -= count;
		if (_items[type] <= 0)
			_items.Remove(type);
		
		return true;
	}
	
	public bool HasItem(ItemType type)
	{
		return _items.ContainsKey(type) && _items[type] > 0;
	}
	
	public int GetItemCount(ItemType type)
	{
		return _items.ContainsKey(type) ? _items[type] : 0;
	}
}
