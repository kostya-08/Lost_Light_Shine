using Godot;
using System;

public partial class Portal : Area2D
{
	[Export] private Vector2 _targetPosition = new Vector2(0, 0);
	
	public override void _Ready()
	{
		// Подключаем все возможные сигналы для диагностики
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		AreaEntered += OnAreaEntered;
		
		// Включаем мониторинг
		Monitoring = true;
		Monitorable = true;
		
		GD.Print("=== ПОРТАЛ ИНИЦИАЛИЗИРОВАН ===");
		GD.Print($"Позиция портала: {Position}");
		GD.Print($"Мониторинг включен: {Monitoring}");
		
		// Проверяем наличие CollisionShape
		var collisionShape = GetChild<CollisionShape2D>(0);
		if (collisionShape != null)
		{
			GD.Print($"CollisionShape найден: {collisionShape.Name}, Enabled: {collisionShape.Disabled == false}");
		}
		else
		{
			GD.PrintErr("ОШИБКА: Нет CollisionShape2D у портала!");
		}
	}
	
	private void OnBodyEntered(Node2D body)
	{
		GD.Print($"!!! СРАБОТАЛ СИГНАЛ BodyEntered !!!");
		GD.Print($"Тело: {body.Name}");
		GD.Print($"Тип: {body.GetType()}");
		GD.Print($"Позиция тела: {body.Position}");
		
		if (body is Player player)
		{
			GD.Print("✅ ЭТО ИГРОК! Телепортирую...");
			player.Position = _targetPosition;
			GD.Print($"Игрок перемещен на {_targetPosition}");
		}
		else
		{
			GD.Print("❌ Это НЕ игрок");
		}
	}
	
	private void OnBodyExited(Node2D body)
	{
		GD.Print($"Тело покинуло портал: {body.Name}");
	}
	
	private void OnAreaEntered(Area2D area)
	{
		GD.Print($"Область вошла в портал: {area.Name}");
	}
}
